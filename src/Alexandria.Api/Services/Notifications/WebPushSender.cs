using System.Net;
using System.Text.Json;
using Alexandria.Api.Options;
using Lib.Net.Http.WebPush;
using Lib.Net.Http.WebPush.Authentication;
using Microsoft.Extensions.Options;
using AppPushMessage = Alexandria.Api.Dtos.PushMessage;
using AppPushSubscription = Alexandria.Api.Models.PushSubscription;

namespace Alexandria.Api.Services.Notifications;

/// <summary>
/// Thin wrapper around Lib.Net.Http.WebPush, which does the standard aes128gcm payload encryption (RFC 8291)
/// and VAPID signing (RFC 8292) that every browser's push service, including Apple's, accepts.
/// </summary>
public class WebPushSender(HttpClient httpClient, IOptions<WebPushOptions> options, ILogger<WebPushSender> logger) : IWebPushSender
{
    /// <summary>How long a push service keeps an undelivered message (e.g. phone offline): one day.</summary>
    public const int TimeToLiveSeconds = 24 * 60 * 60;

    private static readonly JsonSerializerOptions PayloadJson = new(JsonSerializerDefaults.Web);

    public bool IsConfigured => options.Value.IsConfigured;

    public string? PublicKey => options.Value.PublicKey;

    public async Task<PushSendResult> SendAsync(AppPushSubscription subscription, AppPushMessage message, CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (!settings.IsConfigured)
        {
            return PushSendResult.Failed;
        }

        // The client only borrows the HttpClient (owned by IHttpClientFactory).
        var client = new PushServiceClient(httpClient);
        var target = new PushSubscription { Endpoint = subscription.Endpoint };
        target.SetKey(PushEncryptionKeyName.P256DH, subscription.P256dh);
        target.SetKey(PushEncryptionKeyName.Auth, subscription.Auth);
        var content = new PushMessage(JsonSerializer.Serialize(message, PayloadJson))
        {
            TimeToLive = TimeToLiveSeconds,
            Urgency = PushMessageUrgency.High,
        };

        using var vapid = new VapidAuthentication(settings.PublicKey, settings.PrivateKey) { Subject = settings.Subject };
        try
        {
            await client.RequestPushMessageDeliveryAsync(target, content, vapid, VapidAuthenticationScheme.Vapid, cancellationToken);
            return PushSendResult.Sent;
        }
        catch (PushServiceClientException e) when (e.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone)
        {
            return PushSendResult.Gone;
        }
        catch (PushServiceClientException e)
        {
            // Status only: the endpoint is a per-browser capability URL and is not logged.
            logger.LogWarning("Push service rejected a notification with status {StatusCode}.", (int)e.StatusCode);
            return PushSendResult.Failed;
        }
    }
}
