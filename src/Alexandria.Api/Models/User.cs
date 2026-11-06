namespace Alexandria.Api.Models;

public class User
{
    public Guid Id { get; set; }

    /// <summary>Google's stable account identifier (the "sub" claim).</summary>
    public required string GoogleSubjectId { get; set; }

    public required string Email { get; set; }

    public required string DisplayName { get; set; }

    public required string UserName { get; set; }

    /// <summary>Upper-cased <see cref="UserName"/>; unique, used for case-insensitive lookups.</summary>
    public required string NormalizedUserName { get; set; }

    public string? ProfilePictureUrl { get; set; }

    /// <summary>When the user most recently accepted the beta terms (required at every sign-in).</summary>
    public DateTimeOffset? TermsAcceptedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<Library> Libraries { get; set; } = [];
}
