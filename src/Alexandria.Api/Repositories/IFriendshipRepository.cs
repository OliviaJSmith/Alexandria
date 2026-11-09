namespace Alexandria.Api.Repositories;

public interface IFriendshipRepository
{
    /// <summary>True when the two users have an accepted friendship, in either direction.</summary>
    Task<bool> AreFriendsAsync(Guid userId, Guid otherUserId, CancellationToken cancellationToken = default);
}
