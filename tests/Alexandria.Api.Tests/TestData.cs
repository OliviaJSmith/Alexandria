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

    public static Library Library(Guid ownerId, bool isPublic = false, string name = "Living Room") => new()
    {
        Id = Guid.NewGuid(),
        OwnerId = ownerId,
        Name = name,
        IsPublic = isPublic,
        CreatedAt = Now.AddDays(-10),
        UpdatedAt = Now.AddDays(-10),
    };

    public static Book Book(string title = "The Hobbit", string? isbn = "9780547928227") => new()
    {
        Id = Guid.NewGuid(),
        Title = title,
        Author = "J.R.R. Tolkien",
        Isbn = isbn,
        Genre = "Fantasy",
        CreatedAt = Now.AddDays(-20),
        UpdatedAt = Now.AddDays(-20),
    };

    public static LibraryBook Copy(Library library, Book book, LibraryBookStatus status = LibraryBookStatus.Available) => new()
    {
        Id = Guid.NewGuid(),
        LibraryId = library.Id,
        Library = library,
        BookId = book.Id,
        Book = book,
        Status = status,
        AddedAt = Now.AddDays(-5),
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
