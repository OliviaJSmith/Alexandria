using Alexandria.API.DTOs;
using Alexandria.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alexandria.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController(IUserService userService) : BaseController
{
    /// <summary>
    /// Gets the current user's profile.
    /// </summary>
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        var userId = GetCurrentUserId();
        var user = await userService.GetUserByIdAsync(userId);
        
        if (user is null)
            return NotFound();

        return Ok(user);
    }

    /// <summary>
    /// Gets a user by ID.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserDto>> GetUser(int id)
    {
        var user = await userService.GetUserByIdAsync(id);
        
        if (user is null)
            return NotFound();

        return Ok(user);
    }

    /// <summary>
    /// Creates a new user account.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserDto createUserDto)
    {
        var result = await userService.CreateUserAsync(createUserDto);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return CreatedAtAction(nameof(GetUser), new { id = result.Data!.Id }, result.Data);
    }

    /// <summary>
    /// Updates the current user's profile.
    /// </summary>
    [HttpPut("me")]
    public async Task<ActionResult<UserDto>> UpdateCurrentUser([FromBody] UpdateUserDto updateUserDto)
    {
        var userId = GetCurrentUserId();
        var result = await userService.UpdateUserAsync(userId, updateUserDto);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Data);
    }

    /// <summary>
    /// Checks if a username is available.
    /// </summary>
    [HttpGet("check-username")]
    [AllowAnonymous]
    public async Task<ActionResult<object>> CheckUserNameAvailability([FromQuery] string userName)
    {
        if (string.IsNullOrWhiteSpace(userName))
            return BadRequest(new { error = "Username is required" });

        var isAvailable = await userService.IsUserNameAvailableAsync(userName);
        return Ok(new { available = isAvailable });
    }
}
