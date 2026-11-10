using Alexandria.Api.Dtos;
using Alexandria.Api.Models;

namespace Alexandria.Api.Services.Notifications;

/// <summary>Browser push subscriptions and the notifications sent to them.</summary>
public interface INotificationService
{
    /// <summary>The VAPID public key; Unavailable when push isn't configured.</summary>
    ServiceResult<WebPushKeyDto> GetPublicKey();

    /// <summary>Saves the caller's browser subscription (moving it over if another user had it).</summary>
    Task<ServiceResult<bool>> SubscribeAsync(Guid userId, SubscribePushRequest request, CancellationToken cancellationToken = default);

    /// <summary>Removes one of the caller's subscriptions.</summary>
    Task<ServiceResult<bool>> UnsubscribeAsync(Guid userId, string endpoint, CancellationToken cancellationToken = default);

    /// <summary>Tells the lender someone asked to borrow their book. Never throws: a failed push must not fail the request.</summary>
    Task NotifyBorrowRequestAsync(Loan loan, CancellationToken cancellationToken = default);
}
