using Alexandria.Api.Data;
using Alexandria.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Alexandria.Api.Repositories;

public class FriendshipRepository(AlexandriaDbContext context) : IFriendshipRepository
{
    public async Task<bool> AreFriendsAsync(Guid userId, Guid otherUserId, CancellationToken cancellationToken = default)
    {
        return await context.Friendships.AnyAsync(
            f => f.Status == FriendshipStatus.Accepted
                && ((f.RequesterId == userId && f.AddresseeId == otherUserId)
                    || (f.RequesterId == otherUserId && f.AddresseeId == userId)),
            cancellationToken);
    }

    public async Task<Friendship?> GetBetweenAsync(Guid userId, Guid otherUserId, CancellationToken cancellationToken = default)
    {
        return await WithUsers()
            .FirstOrDefaultAsync(
                f => (f.RequesterId == userId && f.AddresseeId == otherUserId)
                    || (f.RequesterId == otherUserId && f.AddresseeId == userId),
                cancellationToken);
    }

    public async Task<Friendship?> GetByIdAsync(Guid friendshipId, CancellationToken cancellationToken = default)
    {
        return await WithUsers().SingleOrDefaultAsync(f => f.Id == friendshipId, cancellationToken);
    }

    public async Task<IReadOnlyList<Friendship>> GetForUserAsync(
        Guid userId,
        FriendshipStatus status,
        CancellationToken cancellationToken = default)
    {
        return await WithUsers()
            .Where(f => f.Status == status && (f.RequesterId == userId || f.AddresseeId == userId))
            .OrderByDescending(f => f.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Friendship>> GetBetweenManyAsync(
        Guid userId,
        IReadOnlyCollection<Guid> otherUserIds,
        CancellationToken cancellationToken = default)
    {
        return await context.Friendships
            .Where(f => (f.RequesterId == userId && otherUserIds.Contains(f.AddresseeId))
                || (f.AddresseeId == userId && otherUserIds.Contains(f.RequesterId)))
            .ToListAsync(cancellationToken);
    }

    public void Add(Friendship friendship)
    {
        context.Friendships.Add(friendship);
    }

    public void Remove(Friendship friendship)
    {
        context.Friendships.Remove(friendship);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await context.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<Friendship> WithUsers() =>
        context.Friendships.Include(f => f.Requester).Include(f => f.Addressee);
}
