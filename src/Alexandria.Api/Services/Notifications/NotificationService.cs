using Alexandria.Api.Dtos;
using Alexandria.Api.Models;
using Alexandria.Api.Repositories;
using Microsoft.AspNetCore.WebUtilities;

namespace Alexandria.Api.Services.Notifications;

public class NotificationService(
    IPushSubscriptionRepository subscriptions,
    IWebPushSender sender,
    TimeProvider timeProvider,
    ILogger<NotificationService> logger) : INotificationService
{
    public const string NotConfiguredMessage = "Notifications aren't available right now.";
    public const string InvalidEndpointMessage = "That isn't a supported push service address.";
    public const string InvalidKeysMessage = "The subscription keys aren't valid.";
    public const string SubscriptionNotFoundMessage = "Subscription not found.";
    public const string LoansPath = "/loans";
    public const string FriendsPath = "/friends";

    /// <summary>The browser push services we deliver to. Anything else is refused so the API can't be aimed at arbitrary URLs.</summary>
    private static readonly string[] AllowedHosts = ["fcm.googleapis.com", "updates.push.services.mozilla.com"];
    private static readonly string[] AllowedHostSuffixes = [".push.apple.com", ".notify.windows.com"];

    private const int P256dhBytes = 65; // uncompressed P-256 public key
    private const int AuthBytes = 16;

    public ServiceResult<WebPushKeyDto> GetPublicKey() =>
        sender.IsConfigured
            ? ServiceResult<WebPushKeyDto>.Success(new WebPushKeyDto(sender.PublicKey!))
            : ServiceResult<WebPushKeyDto>.Failure(ServiceErrorType.Unavailable, NotConfiguredMessage);

    public async Task<ServiceResult<bool>> SubscribeAsync(Guid userId, SubscribePushRequest request, CancellationToken cancellationToken = default)
    {
        if (!sender.IsConfigured)
        {
            return Failure(ServiceErrorType.Unavailable, NotConfiguredMessage);
        }

        if (!IsAllowedEndpoint(request.Endpoint))
        {
            return Failure(ServiceErrorType.Validation, InvalidEndpointMessage);
        }

        if (DecodedLength(request.P256dh) != P256dhBytes || DecodedLength(request.Auth) != AuthBytes)
        {
            return Failure(ServiceErrorType.Validation, InvalidKeysMessage);
        }

        var existing = await subscriptions.GetByEndpointAsync(request.Endpoint, cancellationToken);
        if (existing is null)
        {
            subscriptions.Add(new PushSubscription
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Endpoint = request.Endpoint,
                P256dh = request.P256dh,
                Auth = request.Auth,
                CreatedAt = timeProvider.GetUtcNow(),
            });
        }
        else
        {
            // Same browser, possibly a different account now (shared computer): it belongs to whoever subscribed last.
            existing.UserId = userId;
            existing.P256dh = request.P256dh;
            existing.Auth = request.Auth;
        }

        await subscriptions.SaveChangesAsync(cancellationToken);
        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<bool>> UnsubscribeAsync(Guid userId, string endpoint, CancellationToken cancellationToken = default)
    {
        var existing = await subscriptions.GetByEndpointAsync(endpoint, cancellationToken);
        if (existing is null || existing.UserId != userId)
        {
            return Failure(ServiceErrorType.NotFound, SubscriptionNotFoundMessage);
        }

        subscriptions.Remove(existing);
        await subscriptions.SaveChangesAsync(cancellationToken);
        return ServiceResult<bool>.Success(true);
    }

    public Task NotifyBorrowRequestAsync(Loan loan, CancellationToken cancellationToken = default)
    {
        var borrower = loan.Borrower?.DisplayName ?? "A friend";
        var title = loan.LibraryBook?.Book?.Title ?? "one of your books";
        return SendToUserAsync(loan.LenderId, new PushMessage("Borrow request", $"{borrower} wants to borrow {title}.", LoansPath), cancellationToken);
    }

    public Task NotifyFriendRequestAsync(Friendship friendship, CancellationToken cancellationToken = default)
    {
        var requester = friendship.Requester;
        var who = requester is null ? "Someone" : $"{requester.DisplayName} (@{requester.UserName})";
        return SendToUserAsync(
            friendship.AddresseeId,
            new PushMessage("Friend request", $"{who} wants to be friends.", FriendsPath),
            cancellationToken);
    }

    public Task NotifyLoanDueAsync(Loan loan, DueReminder reminder, CancellationToken cancellationToken = default)
    {
        var title = loan.LibraryBook?.Book?.Title ?? "A book you borrowed";
        var lender = loan.Lender?.DisplayName ?? "its owner";
        var when = reminder == DueReminder.Today ? "today" : "tomorrow";
        return SendToUserAsync(
            loan.BorrowerId,
            new PushMessage($"Due {when}", $"{title} is due back to {lender} {when}.", LoansPath),
            cancellationToken);
    }

    /// <summary>Sends to every browser the user subscribed, deleting ones the push service says are gone. Never throws.</summary>
    private async Task SendToUserAsync(Guid userId, PushMessage message, CancellationToken cancellationToken)
    {
        if (!sender.IsConfigured)
        {
            return;
        }

        try
        {
            var targets = await subscriptions.GetForUserAsync(userId, cancellationToken);
            var removed = false;
            foreach (var target in targets)
            {
                if (await sender.SendAsync(target, message, cancellationToken) == PushSendResult.Gone)
                {
                    subscriptions.Remove(target);
                    removed = true;
                }
            }

            if (removed)
            {
                await subscriptions.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception e)
        {
            // The action that triggered this already succeeded; a notification is best effort.
            // Exception type only: no payload, endpoints or keys in logs.
            logger.LogWarning("{Notification} notification failed: {ErrorType}.", message.Title, e.GetType().Name);
        }
    }

    internal static bool IsAllowedEndpoint(string endpoint) =>
        Uri.TryCreate(endpoint, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps
        && uri.IsDefaultPort
        && string.IsNullOrEmpty(uri.UserInfo)
        && (AllowedHosts.Contains(uri.Host, StringComparer.OrdinalIgnoreCase)
            || AllowedHostSuffixes.Any(suffix => uri.Host.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)));

    private static int DecodedLength(string base64Url)
    {
        try
        {
            return WebEncoders.Base64UrlDecode(base64Url).Length;
        }
        catch (FormatException)
        {
            return -1;
        }
    }

    private static ServiceResult<bool> Failure(ServiceErrorType type, string message) => ServiceResult<bool>.Failure(type, message);
}
