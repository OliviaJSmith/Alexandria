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

    protected ActionResult<T> ToActionResult<T>(ServiceResult<T> result)
    {
        if (result.IsSuccess)
        {
            return Ok(result.Value);
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
