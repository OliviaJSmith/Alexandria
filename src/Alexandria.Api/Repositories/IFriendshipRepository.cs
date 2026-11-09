using Alexandria.Api.Models;

namespace Alexandria.Api.Repositories;

public interface IFriendshipRepository
{
    /// <summary>True when the two users have an accepted friendship, in either direction.</summary>
    Task<bool> AreFriendsAsync(Guid userId, Guid otherUserId, CancellationToken cancellationToken = default);

    /// <summary>The friendship row between two users in either direction, with both users loaded.</summary>
    Task<Friendship?> GetBetweenAsync(Guid userId, Guid otherUserId, CancellationToken cancellationToken = default);

    /// <summary>A friendship with both users loaded.</summary>
    Task<Friendship?> GetByIdAsync(Guid friendshipId, CancellationToken cancellationToken = default);

    /// <summary>Friendships the user is part of with the given status, both users loaded, newest first.</summary>
    Task<IReadOnlyList<Friendship>> GetForUserAsync(Guid userId, FriendshipStatus status, CancellationToken cancellationToken = default);

    /// <summary>Friendships between the user and any of the given users, in either direction.</summary>
    Task<IReadOnlyList<Friendship>> GetBetweenManyAsync(Guid userId, IReadOnlyCollection<Guid> otherUserIds, CancellationToken cancellationToken = default);

    void Add(Friendship friendship);

    void Remove(Friendship friendship);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
