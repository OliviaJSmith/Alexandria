using Alexandria.Api.Data;
using Alexandria.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Alexandria.Api.Repositories;

public class PushSubscriptionRepository(AlexandriaDbContext context) : IPushSubscriptionRepository
{
    public async Task<IReadOnlyList<PushSubscription>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await context.PushSubscriptions
            .Where(s => s.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task<PushSubscription?> GetByEndpointAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        return await context.PushSubscriptions.SingleOrDefaultAsync(s => s.Endpoint == endpoint, cancellationToken);
    }

    public void Add(PushSubscription subscription)
    {
        context.PushSubscriptions.Add(subscription);
    }

    public void Remove(PushSubscription subscription)
    {
        context.PushSubscriptions.Remove(subscription);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await context.SaveChangesAsync(cancellationToken);
    }
}
