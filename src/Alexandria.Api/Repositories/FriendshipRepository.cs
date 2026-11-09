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
}
