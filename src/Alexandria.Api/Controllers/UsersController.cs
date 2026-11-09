using Alexandria.Api.Dtos;
using Alexandria.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alexandria.Api.Controllers;

[Route("api/users")]
[Authorize]
public class UsersController(IUserService userService) : ApiControllerBase
{
    /// <summary>Returns the signed-in user's profile.</summary>
    [HttpGet("me")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<UserDto>> GetCurrentUser(CancellationToken cancellationToken) =>
        ForCurrentUserAsync(userId => userService.GetByIdAsync(userId, cancellationToken));
}
