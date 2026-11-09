using Alexandria.Api.Dtos;
using Alexandria.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alexandria.Api.Controllers;

[Route("api/friends")]
[Authorize]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public class FriendsController(IFriendService friendService) : ApiControllerBase
{
    /// <summary>The signed-in user's friends, optionally filtered by username or real name.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<FriendshipDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<ActionResult<IReadOnlyList<FriendshipDto>>> GetFriends([FromQuery] string? search, CancellationToken cancellationToken) =>
        ForCurrentUserAsync(userId => friendService.GetFriendsAsync(userId, search, cancellationToken));

    /// <summary>Pending requests: incoming (to accept or decline) and outgoing (to cancel).</summary>
    [HttpGet("requests")]
    [ProducesResponseType<FriendRequestsDto>(StatusCodes.Status200OK)]
    public Task<ActionResult<FriendRequestsDto>> GetRequests(CancellationToken cancellationToken) =>
        ForCurrentUserAsync(userId => friendService.GetRequestsAsync(userId, cancellationToken));

    /// <summary>Finds users by username prefix (at least 2 characters).</summary>
    [HttpGet("search")]
    [ProducesResponseType<IReadOnlyList<UserSearchResultDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<ActionResult<IReadOnlyList<UserSearchResultDto>>> Search([FromQuery] string userName, CancellationToken cancellationToken) =>
        ForCurrentUserAsync(userId => friendService.SearchUsersAsync(userId, userName ?? string.Empty, cancellationToken));

    /// <summary>Sends a friend request by username (accepts theirs if they already asked).</summary>
    [HttpPost("requests")]
    [ProducesResponseType<FriendshipDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<ActionResult<FriendshipDto>> SendRequest([FromBody] SendFriendRequest request, CancellationToken cancellationToken) =>
        ForCurrentUserAsync(userId => friendService.SendRequestAsync(userId, request.UserName, cancellationToken));

    [HttpPost("requests/{friendshipId:guid}/accept")]
    [ProducesResponseType<FriendshipDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<FriendshipDto>> Accept(Guid friendshipId, CancellationToken cancellationToken) =>
        ForCurrentUserAsync(userId => friendService.AcceptAsync(userId, friendshipId, cancellationToken));

    [HttpPost("requests/{friendshipId:guid}/decline")]
    [ProducesResponseType<FriendshipDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<FriendshipDto>> Decline(Guid friendshipId, CancellationToken cancellationToken) =>
        ForCurrentUserAsync(userId => friendService.DeclineAsync(userId, friendshipId, cancellationToken));

    /// <summary>Unfriends, or cancels a request the signed-in user sent.</summary>
    [HttpDelete("{friendshipId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Remove(Guid friendshipId, CancellationToken cancellationToken)
    {
        var result = await ForCurrentUserAsync(userId => friendService.RemoveAsync(userId, friendshipId, cancellationToken), _ => NoContent());
        return result.Result!;
    }

    /// <summary>A friend's public libraries.</summary>
    [HttpGet("{friendId:guid}/libraries")]
    [ProducesResponseType<IReadOnlyList<LibraryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<IReadOnlyList<LibraryDto>>> GetFriendLibraries(Guid friendId, CancellationToken cancellationToken) =>
        ForCurrentUserAsync(userId => friendService.GetFriendLibrariesAsync(userId, friendId, cancellationToken));
}
