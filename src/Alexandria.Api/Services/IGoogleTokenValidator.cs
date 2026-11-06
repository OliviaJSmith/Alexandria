namespace Alexandria.Api.Services;

public sealed record GoogleUser(string SubjectId, string Email, string Name, string? PictureUrl);

public interface IGoogleTokenValidator
{
    /// <summary>
    /// Verifies a Google OAuth access token was issued to one of Alexandria's client IDs
    /// and returns the account it belongs to, or null if the token is not acceptable.
    /// </summary>
    Task<GoogleUser?> ValidateAccessTokenAsync(string accessToken, CancellationToken cancellationToken = default);
}
