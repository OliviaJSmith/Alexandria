using Alexandria.API.DTOs;
using Alexandria.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alexandria.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FriendsController(IFriendService friendService) : BaseController
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<FriendDto>>> GetFriends()
    {
        var userId = GetCurrentUserId();
        var friends = await friendService.GetFriendsAsync(userId);
        return Ok(friends);
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
                _ => BadRequest(result.Error)
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
                _ => BadRequest(result.Error)
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
                _ => BadRequest(result.Error)
            };
        }

        return NoContent();
    }
}
