using Alexandria.Api.Dtos;
using Alexandria.Api.Models;
using Alexandria.Api.Repositories;

namespace Alexandria.Api.Services;

public class FriendService(
    IFriendshipRepository friendships,
    IUserRepository users,
    ILibraryRepository libraries,
    TimeProvider timeProvider) : IFriendService
{
    public const int MinSearchLength = 2;
    public const int MaxSearchResults = 20;
    public const string SearchTooShortMessage = "Type at least 2 characters of a username.";
    public const int MaxFriendSearchLength = 100;
    public const string FriendSearchTooLongMessage = "Search must be 100 characters or fewer.";
    public const string UserNotFoundMessage = "No user has that username.";
    public const string CannotFriendSelfMessage = "You can't send a friend request to yourself.";
    public const string AlreadyFriendsMessage = "You're already friends.";
    public const string AlreadyRequestedMessage = "You already sent this person a friend request.";
    public const string RequestUnavailableMessage = "A friend request can't be sent to this person right now.";
    public const string FriendshipNotFoundMessage = "Friend request not found.";
    public const string NotFriendsMessage = "You aren't friends with this person.";

    public async Task<ServiceResult<IReadOnlyList<FriendshipDto>>> GetFriendsAsync(
        Guid userId,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var term = search?.Trim().TrimStart('@') ?? string.Empty;
        if (term.Length > MaxFriendSearchLength)
        {
            return ServiceResult<IReadOnlyList<FriendshipDto>>.Failure(ServiceErrorType.Validation, FriendSearchTooLongMessage);
        }

        // A user's friend list is small, so it is filtered in memory after the one query that loads it.
        var accepted = await friendships.GetForUserAsync(userId, FriendshipStatus.Accepted, cancellationToken);
        var friends = accepted
            .Select(f => FriendshipDto.For(f, userId))
            .Where(f => term.Length == 0 || MatchesFriend(f.User, term))
            .OrderBy(f => f.User.UserName, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return ServiceResult<IReadOnlyList<FriendshipDto>>.Success(friends);
    }

    public async Task<ServiceResult<FriendRequestsDto>> GetRequestsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var pending = (await friendships.GetForUserAsync(userId, FriendshipStatus.Pending, cancellationToken))
            .Select(f => FriendshipDto.For(f, userId))
            .ToList();
        return ServiceResult<FriendRequestsDto>.Success(new FriendRequestsDto(
            pending.Where(f => f.IsIncoming).ToList(),
            pending.Where(f => !f.IsIncoming).ToList()));
    }

    public async Task<ServiceResult<IReadOnlyList<UserSearchResultDto>>> SearchUsersAsync(
        Guid userId,
        string query,
        CancellationToken cancellationToken = default)
    {
        var trimmed = query.Trim().TrimStart('@');
        if (trimmed.Length < MinSearchLength)
        {
            return ServiceResult<IReadOnlyList<UserSearchResultDto>>.Failure(ServiceErrorType.Validation, SearchTooShortMessage);
        }

        var matches = await users.SearchByUserNamePrefixAsync(UsernameGenerator.Normalize(trimmed), userId, MaxSearchResults, cancellationToken);
        var related = await friendships.GetBetweenManyAsync(userId, matches.Select(u => u.Id).ToList(), cancellationToken);

        var results = matches.Select(user =>
        {
            var friendship = related.FirstOrDefault(f => f.RequesterId == user.Id || f.AddresseeId == user.Id);
            return new UserSearchResultDto(UserSummaryDto.FromUser(user), RelationshipOf(friendship, userId), friendship?.Id);
        }).ToList();

        return ServiceResult<IReadOnlyList<UserSearchResultDto>>.Success(results);
    }

    public async Task<ServiceResult<FriendshipDto>> SendRequestAsync(Guid userId, string userName, CancellationToken cancellationToken = default)
    {
        var target = await users.GetByNormalizedUserNameAsync(UsernameGenerator.Normalize(userName.Trim().TrimStart('@')), cancellationToken);
        if (target is null)
        {
            return Failure(ServiceErrorType.NotFound, UserNotFoundMessage);
        }

        if (target.Id == userId)
        {
            return Failure(ServiceErrorType.Validation, CannotFriendSelfMessage);
        }

        var now = timeProvider.GetUtcNow();
        var existing = await friendships.GetBetweenAsync(userId, target.Id, cancellationToken);
        if (existing is not null)
        {
            var sentByMe = existing.RequesterId == userId;
            switch (existing.Status)
            {
                case FriendshipStatus.Accepted:
                    return Failure(ServiceErrorType.Conflict, AlreadyFriendsMessage);
                case FriendshipStatus.Pending when sentByMe:
                    return Failure(ServiceErrorType.Conflict, AlreadyRequestedMessage);
                case FriendshipStatus.Pending:
                    // They already asked us: sending back means yes.
                    existing.Status = FriendshipStatus.Accepted;
                    break;
                case FriendshipStatus.Rejected when !sentByMe:
                    // We declined them earlier and changed our mind: re-open as our request.
                    existing.RequesterId = userId;
                    existing.AddresseeId = target.Id;
                    (existing.Requester, existing.Addressee) = (existing.Addressee, existing.Requester);
                    existing.Status = FriendshipStatus.Pending;
                    break;
                default:
                    // Declined (or blocked) requests can't be re-sent by the same requester.
                    return Failure(ServiceErrorType.Conflict, RequestUnavailableMessage);
            }

            existing.UpdatedAt = now;
            await friendships.SaveChangesAsync(cancellationToken);
            return ServiceResult<FriendshipDto>.Success(FriendshipDto.For(existing, userId));
        }

        var requester = await users.GetByIdAsync(userId, cancellationToken);
        var friendship = new Friendship
        {
            Id = Guid.NewGuid(),
            RequesterId = userId,
            Requester = requester,
            AddresseeId = target.Id,
            Addressee = target,
            Status = FriendshipStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now,
        };
        friendships.Add(friendship);
        await friendships.SaveChangesAsync(cancellationToken);
        return ServiceResult<FriendshipDto>.Success(FriendshipDto.For(friendship, userId));
    }

    public Task<ServiceResult<FriendshipDto>> AcceptAsync(Guid userId, Guid friendshipId, CancellationToken cancellationToken = default) =>
        RespondAsync(userId, friendshipId, FriendshipStatus.Accepted, cancellationToken);

    public Task<ServiceResult<FriendshipDto>> DeclineAsync(Guid userId, Guid friendshipId, CancellationToken cancellationToken = default) =>
        RespondAsync(userId, friendshipId, FriendshipStatus.Rejected, cancellationToken);

    public async Task<ServiceResult<bool>> RemoveAsync(Guid userId, Guid friendshipId, CancellationToken cancellationToken = default)
    {
        var friendship = await friendships.GetByIdAsync(friendshipId, cancellationToken);
        var canRemove = friendship is not null && friendship.Status switch
        {
            FriendshipStatus.Accepted => friendship.RequesterId == userId || friendship.AddresseeId == userId,
            FriendshipStatus.Pending => friendship.RequesterId == userId,
            _ => false,
        };
        if (!canRemove)
        {
            return ServiceResult<bool>.Failure(ServiceErrorType.NotFound, FriendshipNotFoundMessage);
        }

        friendships.Remove(friendship!);
        await friendships.SaveChangesAsync(cancellationToken);
        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<IReadOnlyList<LibraryDto>>> GetFriendLibrariesAsync(
        Guid userId,
        Guid friendId,
        CancellationToken cancellationToken = default)
    {
        if (!await friendships.AreFriendsAsync(userId, friendId, cancellationToken))
        {
            return ServiceResult<IReadOnlyList<LibraryDto>>.Failure(ServiceErrorType.NotFound, NotFriendsMessage);
        }

        var owned = await libraries.GetByOwnerAsync(friendId, cancellationToken);
        return ServiceResult<IReadOnlyList<LibraryDto>>.Success(
            owned.Where(l => l.IsPublic).Select(LibraryDto.FromLibrary).ToList());
    }

    private async Task<ServiceResult<FriendshipDto>> RespondAsync(
        Guid userId,
        Guid friendshipId,
        FriendshipStatus response,
        CancellationToken cancellationToken)
    {
        var friendship = await friendships.GetByIdAsync(friendshipId, cancellationToken);
        if (friendship is null || friendship.AddresseeId != userId || friendship.Status != FriendshipStatus.Pending)
        {
            return Failure(ServiceErrorType.NotFound, FriendshipNotFoundMessage);
        }

        friendship.Status = response;
        friendship.UpdatedAt = timeProvider.GetUtcNow();
        await friendships.SaveChangesAsync(cancellationToken);
        return ServiceResult<FriendshipDto>.Success(FriendshipDto.For(friendship, userId));
    }

    /// <summary>Case-insensitive "contains" on the username or the real (display) name.</summary>
    private static bool MatchesFriend(UserSummaryDto user, string term) =>
        user.UserName.Contains(term, StringComparison.OrdinalIgnoreCase)
        || user.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase);

    private static UserRelationship RelationshipOf(Friendship? friendship, Guid viewerId) => friendship?.Status switch
    {
        FriendshipStatus.Accepted => UserRelationship.Friends,
        FriendshipStatus.Pending => friendship.RequesterId == viewerId ? UserRelationship.RequestSent : UserRelationship.RequestReceived,
        _ => UserRelationship.None,
    };

    private static ServiceResult<FriendshipDto> Failure(ServiceErrorType type, string message) =>
        ServiceResult<FriendshipDto>.Failure(type, message);
}
