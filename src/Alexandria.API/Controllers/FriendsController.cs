using Alexandria.API.DTOs;
using Alexandria.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alexandria.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FriendsController(IFriendService friendService, ILibraryService libraryService)
    : BaseController
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<FriendDto>>> GetFriends()
    {
        var userId = GetCurrentUserId();
        var friends = await friendService.GetFriendsAsync(userId);
        return Ok(friends);
    }

    [HttpGet("requests")]
    public async Task<ActionResult<IEnumerable<FriendRequestDto>>> GetPendingRequests()
    {
        var userId = GetCurrentUserId();
        var requests = await friendService.GetPendingRequestsAsync(userId);
        return Ok(requests);
    }

    [HttpGet("search")]
    public async Task<ActionResult<UserDto>> SearchUserByEmail([FromQuery] string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest("Email is required");

        var userId = GetCurrentUserId();
        var user = await friendService.SearchUserByEmailAsync(userId, email);

        if (user is null)
            return NotFound("No user found with that email");

        return Ok(user);
    }

    [HttpGet("search-by-username")]
    public async Task<ActionResult<IEnumerable<UserDto>>> SearchUsersByUserName(
        [FromQuery] string userName
    )
    {
        if (string.IsNullOrWhiteSpace(userName))
            return BadRequest("Username is required");

        var userId = GetCurrentUserId();
        var users = await friendService.SearchUsersByUserNameAsync(userId, userName);
        return Ok(users);
    }

    [HttpGet("{friendId}/libraries")]
    public async Task<ActionResult<IEnumerable<LibraryDto>>> GetFriendLibraries(int friendId)
    {
        var userId = GetCurrentUserId();

        if (!await friendService.AreFriendsAsync(userId, friendId))
            return NotFound("You are not friends with this user");

        var libraries = await libraryService.GetLibrariesByOwnerAsync(friendId);
        return Ok(libraries);
    }

    [HttpPost("{friendId}")]
    public async Task<ActionResult> SendFriendRequest(int friendId)
    {
        var userId = GetCurrentUserId();
        var result = await friendService.SendFriendRequestAsync(userId, friendId);

        if (!result.IsSuccess)
        {
            return result.Error switch
            {
                "User not found" => NotFound(result.Error),
                _ => BadRequest(result.Error),
            };
        }

        return Ok();
    }

    [HttpPut("{friendshipId}/accept")]
    public async Task<ActionResult> AcceptFriendRequest(int friendshipId)
    {
        var userId = GetCurrentUserId();
        var result = await friendService.AcceptFriendRequestAsync(userId, friendshipId);

        if (!result.IsSuccess)
        {
            return result.Error switch
            {
                "Not found" => NotFound(),
                "Forbidden" => Forbid(),
                _ => BadRequest(result.Error),
            };
        }

        return Ok();
    }

    [HttpDelete("{friendshipId}")]
    public async Task<ActionResult> RemoveFriend(int friendshipId)
    {
        var userId = GetCurrentUserId();
        var result = await friendService.RemoveFriendAsync(userId, friendshipId);

        if (!result.IsSuccess)
        {
            return result.Error switch
            {
                "Not found" => NotFound(),
                "Forbidden" => Forbid(),
                _ => BadRequest(result.Error),
            };
        }

        return NoContent();
    }
}
