using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Alexandria.API.Controllers;

public abstract class BaseController : ControllerBase
{
    protected int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    /// <summary>
    /// Gets the current user ID, or returns 1 as a default for anonymous users.
    /// Used for development/testing when auth is bypassed.
    /// </summary>
    protected int GetCurrentUserIdOrDefault()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) && userId > 0 ? userId : 1;
    }

    protected string GetCurrentUserEmail()
    {
        return User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
    }
}
