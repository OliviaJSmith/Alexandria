using System.ComponentModel.DataAnnotations;
using Alexandria.Api.Data.Configurations;

namespace Alexandria.Api.Dtos;

/// <summary>The VAPID public key browsers pass to <c>pushManager.subscribe</c>.</summary>
public sealed record WebPushKeyDto(string PublicKey);

/// <summary>A browser push subscription, as returned by <c>PushSubscription.toJSON()</c>.</summary>
public class SubscribePushRequest
{
    [Required]
    [StringLength(PushSubscriptionConfiguration.EndpointMaxLength)]
    public string Endpoint { get; set; } = string.Empty;

    [Required]
    [StringLength(PushSubscriptionConfiguration.KeyMaxLength)]
    public string P256dh { get; set; } = string.Empty;

    [Required]
    [StringLength(PushSubscriptionConfiguration.KeyMaxLength)]
    public string Auth { get; set; } = string.Empty;
}

public class UnsubscribePushRequest
{
    [Required]
    [StringLength(PushSubscriptionConfiguration.EndpointMaxLength)]
    public string Endpoint { get; set; } = string.Empty;
}

/// <summary>What the service worker shows. <see cref="Url"/> is the app path to open on tap.</summary>
public sealed record PushMessage(string Title, string Body, string Url);
