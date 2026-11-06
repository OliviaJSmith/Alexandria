using System.ComponentModel.DataAnnotations;

namespace Alexandria.Api.Dtos;

public class GoogleSignInRequest
{
    /// <summary>Google OAuth access token obtained by the app.</summary>
    [Required]
    [StringLength(4096, MinimumLength = 10)]
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>The user must accept the beta terms to sign in.</summary>
    public bool AcceptedTerms { get; set; }
}

public sealed record AuthResponse(string Token, DateTimeOffset ExpiresAt, UserDto User);
