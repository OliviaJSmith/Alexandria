using Alexandria.Api.Dtos;

namespace Alexandria.Api.Services;

/// <summary>
/// Friend requests and friendships. One row per pair of users: a request is Pending until the addressee
/// accepts (Accepted) or declines (Rejected). A declined request can't be re-sent by the same requester,
/// but the other user can still send one.
/// </summary>
public interface IFriendService
{
    /// <summary>
    /// The caller's friends, sorted by username. With <paramref name="search"/>, only friends whose username or
    /// real name contains it (case-insensitive; a leading @ is ignored).
    /// </summary>
    Task<ServiceResult<IReadOnlyList<FriendshipDto>>> GetFriendsAsync(Guid userId, string? search = null, CancellationToken cancellationToken = default);

    Task<ServiceResult<FriendRequestsDto>> GetRequestsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Users whose username starts with the query, with the caller's relationship to each.</summary>
    Task<ServiceResult<IReadOnlyList<UserSearchResultDto>>> SearchUsersAsync(Guid userId, string query, CancellationToken cancellationToken = default);

    /// <summary>Sends a request; if the other user already sent one to the caller, accepts it instead.</summary>
    Task<ServiceResult<FriendshipDto>> SendRequestAsync(Guid userId, string userName, CancellationToken cancellationToken = default);

    Task<ServiceResult<FriendshipDto>> AcceptAsync(Guid userId, Guid friendshipId, CancellationToken cancellationToken = default);

    Task<ServiceResult<FriendshipDto>> DeclineAsync(Guid userId, Guid friendshipId, CancellationToken cancellationToken = default);

    /// <summary>Unfriends, or cancels the caller's own pending request.</summary>
    Task<ServiceResult<bool>> RemoveAsync(Guid userId, Guid friendshipId, CancellationToken cancellationToken = default);

    /// <summary>A friend's public libraries. Not found unless the two are friends.</summary>
    Task<ServiceResult<IReadOnlyList<LibraryDto>>> GetFriendLibrariesAsync(Guid userId, Guid friendId, CancellationToken cancellationToken = default);
}
