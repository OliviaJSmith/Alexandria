using System.Security.Claims;
using Alexandria.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Alexandria.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>The signed-in user's id from the JWT "sub" claim, or null if absent or malformed.</summary>
    protected Guid? CurrentUserId =>
        Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id) ? id : null;

    /// <summary>
    /// Runs a service call for the signed-in user and maps its result; 401 if the token has no valid user id.
    /// </summary>
    protected async Task<ActionResult<T>> ForCurrentUserAsync<T>(
        Func<Guid, Task<ServiceResult<T>>> action,
        Func<T, ActionResult>? onSuccess = null)
    {
        if (CurrentUserId is not { } userId)
        {
            return Unauthorized();
        }

        return ToActionResult(await action(userId), onSuccess);
    }

    /// <summary>Maps a service result to 200 OK (or <paramref name="onSuccess"/>), or a problem response for its error.</summary>
    protected ActionResult<T> ToActionResult<T>(ServiceResult<T> result, Func<T, ActionResult>? onSuccess = null)
    {
        if (result.IsSuccess)
        {
            return onSuccess is null ? Ok(result.Value) : onSuccess(result.Value!);
        }

        var error = result.Error!;
        var statusCode = error.Type switch
        {
            ServiceErrorType.Validation => StatusCodes.Status400BadRequest,
            ServiceErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ServiceErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ServiceErrorType.NotFound => StatusCodes.Status404NotFound,
            ServiceErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError,
        };

        return Problem(detail: error.Message, statusCode: statusCode);
    }
}
