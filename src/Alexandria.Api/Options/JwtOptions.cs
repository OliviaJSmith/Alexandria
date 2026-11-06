using System.ComponentModel.DataAnnotations;

namespace Alexandria.Api.Options;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>Minimum signing-key length for HMAC-SHA256.</summary>
    public const int MinimumSigningKeyBytes = 32;

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    /// <summary>Base64-encoded symmetric key. Comes from Key Vault or user-secrets; never committed.</summary>
    [Required]
    public string SigningKey { get; set; } = string.Empty;

    [Range(typeof(TimeSpan), "00:05:00", "30.00:00:00")]
    public TimeSpan TokenLifetime { get; set; } = TimeSpan.FromDays(7);

    public byte[] GetSigningKeyBytes() => Convert.FromBase64String(SigningKey);

    public bool HasValidSigningKey()
    {
        var buffer = new byte[SigningKey.Length];
        return Convert.TryFromBase64String(SigningKey, buffer, out var bytesWritten)
            && bytesWritten >= MinimumSigningKeyBytes;
    }
}
