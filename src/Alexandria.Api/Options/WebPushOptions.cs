namespace Alexandria.Api.Options;

/// <summary>Web Push (VAPID) keys for browser notifications. Optional: notifications are off when unset.</summary>
public class WebPushOptions
{
    public const string SectionName = "WebPush";

    /// <summary>Who push services can contact about this sender: a mailto: or https: URL (we use the site URL).</summary>
    public string? Subject { get; set; }

    /// <summary>VAPID public key, base64url. Not secret: browsers need it to subscribe.</summary>
    public string? PublicKey { get; set; }

    /// <summary>VAPID private key, base64url. Comes from Key Vault in Azure and user-secrets locally; never committed.</summary>
    public string? PrivateKey { get; set; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Subject) && !string.IsNullOrWhiteSpace(PublicKey) && !string.IsNullOrWhiteSpace(PrivateKey);
}
