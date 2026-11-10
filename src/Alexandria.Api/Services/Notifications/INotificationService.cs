using Alexandria.Api.Dtos;
using Alexandria.Api.Models;

namespace Alexandria.Api.Services.Notifications;

/// <summary>Which due-date reminder to send: the day before, or on the day.</summary>
public enum DueReminder
{
    Tomorrow,
    Today,
}

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

    /// <summary>Tells the addressee someone sent them a friend request. Never throws.</summary>
    Task NotifyFriendRequestAsync(Friendship friendship, CancellationToken cancellationToken = default);

    /// <summary>Reminds the borrower a book is due back soon. Never throws.</summary>
    Task NotifyLoanDueAsync(Loan loan, DueReminder reminder, CancellationToken cancellationToken = default);
}
