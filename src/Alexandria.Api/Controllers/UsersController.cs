using Alexandria.Api.Dtos;
using Alexandria.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alexandria.Api.Controllers;

[Route("api/users")]
[Authorize]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public class UsersController(IUserService userService) : ApiControllerBase
{
    /// <summary>Returns the signed-in user's profile.</summary>
    [HttpGet("me")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<UserDto>> GetCurrentUser(CancellationToken cancellationToken) =>
        ForCurrentUserAsync(userId => userService.GetByIdAsync(userId, cancellationToken));

    /// <summary>Changes the signed-in user's username. 409 if it's taken.</summary>
    [HttpPut("me")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<ActionResult<UserDto>> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken cancellationToken) =>
        ForCurrentUserAsync(userId => userService.UpdateProfileAsync(userId, request, cancellationToken));

    /// <summary>Whether a username is valid and available.</summary>
    [HttpGet("check-username")]
    [ProducesResponseType<UserNameAvailabilityDto>(StatusCodes.Status200OK)]
    public Task<ActionResult<UserNameAvailabilityDto>> CheckUserName([FromQuery] string userName, CancellationToken cancellationToken) =>
        ForCurrentUserAsync(userId => userService.CheckUserNameAsync(userId, userName ?? string.Empty, cancellationToken));
}
