using Alexandria.Api.Dtos;
using Alexandria.Api.Models;

namespace Alexandria.Api.Services.Notifications;

public enum PushSendResult
{
    Sent,

    /// <summary>The push service says the subscription no longer exists (404/410); delete it.</summary>
    Gone,

    Failed,
}

/// <summary>Sends one encrypted Web Push message to one browser subscription.</summary>
public interface IWebPushSender
{
    bool IsConfigured { get; }

    string? PublicKey { get; }

    Task<PushSendResult> SendAsync(PushSubscription subscription, PushMessage message, CancellationToken cancellationToken = default);
}
