namespace Alexandria.Api.Models;

/// <summary>A browser's Web Push subscription. One user can have several (phone, laptop, ...).</summary>
public class PushSubscription
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public User? User { get; set; }

    /// <summary>The push service URL the browser gave us. Unique: one browser subscription belongs to one user.</summary>
    public required string Endpoint { get; set; }

    /// <summary>The browser's public key (base64url) used to encrypt payloads.</summary>
    public required string P256dh { get; set; }

    /// <summary>The browser's auth secret (base64url).</summary>
    public required string Auth { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
