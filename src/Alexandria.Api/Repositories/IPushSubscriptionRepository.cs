using Alexandria.Api.Models;

namespace Alexandria.Api.Repositories;

public interface IPushSubscriptionRepository
{
    Task<IReadOnlyList<PushSubscription>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<PushSubscription?> GetByEndpointAsync(string endpoint, CancellationToken cancellationToken = default);

    void Add(PushSubscription subscription);

    void Remove(PushSubscription subscription);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
