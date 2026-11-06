using Alexandria.Api.Dtos;

namespace Alexandria.Api.Services;

public interface IAuthService
{
    /// <summary>
    /// Exchanges a Google access token for an Alexandria JWT, creating the account on first sign-in
    /// and recording acceptance of the beta terms.
    /// </summary>
    Task<ServiceResult<AuthResponse>> SignInWithGoogleAsync(GoogleSignInRequest request, CancellationToken cancellationToken = default);
}
