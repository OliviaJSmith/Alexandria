using Alexandria.Api.Dtos;
using Alexandria.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alexandria.Api.Controllers;

[Route("api/auth")]
public class AuthController(IAuthService authService) : ApiControllerBase
{
    /// <summary>Exchanges a Google access token for an Alexandria JWT. Requires accepting the beta terms.</summary>
    [HttpPost("google")]
    [AllowAnonymous]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> SignInWithGoogle(
        [FromBody] GoogleSignInRequest request,
        CancellationToken cancellationToken)
    {
        return ToActionResult(await authService.SignInWithGoogleAsync(request, cancellationToken));
    }
}
