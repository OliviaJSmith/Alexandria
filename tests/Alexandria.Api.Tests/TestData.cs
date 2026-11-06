using Alexandria.Api.Models;
using Alexandria.Api.Options;
using Moq;

namespace Alexandria.Api.Tests;

internal static class TestData
{
    public static readonly DateTimeOffset Now = new(2026, 11, 6, 12, 0, 0, TimeSpan.Zero);

    // 32 zero bytes, base64-encoded: valid length for HMAC-SHA256, obviously not a real key.
    public const string SigningKey = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";

    public const string GoogleClientId = "test-client.apps.googleusercontent.com";

    public static Mock<TimeProvider> TimeProviderAt(DateTimeOffset now)
    {
        var timeProvider = new Mock<TimeProvider>();
        timeProvider.Setup(t => t.GetUtcNow()).Returns(now);
        return timeProvider;
    }

    public static JwtOptions JwtOptions() => new()
    {
        Issuer = "alexandria-api",
        Audience = "alexandria-app",
        SigningKey = SigningKey,
        TokenLifetime = TimeSpan.FromDays(7),
    };

    public static User User(string userName = "QuietScribe7") => new()
    {
        Id = Guid.NewGuid(),
        GoogleSubjectId = "google-sub-1",
        Email = "reader@example.com",
        DisplayName = "Avid Reader",
        UserName = userName,
        NormalizedUserName = userName.ToUpperInvariant(),
        ProfilePictureUrl = "https://example.com/old.png",
        CreatedAt = Now.AddDays(-30),
        UpdatedAt = Now.AddDays(-30),
    };
}
