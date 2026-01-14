using Alexandria.API.Data;
using Alexandria.API.DTOs;
using Alexandria.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Alexandria.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FriendsController : ControllerBase
{
    private readonly AlexandriaDbContext _context;

    public FriendsController(AlexandriaDbContext context)
    {
        _context = context;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<FriendDto>>> GetFriends()
    {
        var userId = GetCurrentUserId();
        
        var friendships = await _context.Friendships
            .Include(f => f.Requester)
            .Include(f => f.Addressee)
            .Where(f => (f.RequesterId == userId || f.AddresseeId == userId) 
                && f.Status == FriendshipStatus.Accepted)
            .ToListAsync();

        var friends = friendships.Select(f =>
        {
            var friend = f.RequesterId == userId ? f.Addressee : f.Requester;
            return new FriendDto
            {
                Id = f.Id,
                Friend = new UserDto
                {
                    Id = friend.Id,
                    Email = friend.Email,
                    Name = friend.Name,
                    ProfilePictureUrl = friend.ProfilePictureUrl
                },
                CreatedAt = f.CreatedAt
            };
        });

        return Ok(friends);
    }

    [HttpPost("{friendId}")]
    public async Task<ActionResult> SendFriendRequest(int friendId)
    {
        var userId = GetCurrentUserId();

        if (userId == friendId)
        {
            return BadRequest("Cannot send friend request to yourself");
        }

        var friend = await _context.Users.FindAsync(friendId);
        if (friend == null)
        {
            return NotFound("User not found");
        }

        // Check if friendship already exists
        var existingFriendship = await _context.Friendships
            .FirstOrDefaultAsync(f => 
                (f.RequesterId == userId && f.AddresseeId == friendId) ||
                (f.RequesterId == friendId && f.AddresseeId == userId));

        if (existingFriendship != null)
        {
            return BadRequest("Friendship request already exists");
        }

        var friendship = new Friendship
        {
            RequesterId = userId,
            AddresseeId = friendId,
            Status = FriendshipStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Friendships.Add(friendship);
        await _context.SaveChangesAsync();

        return Ok();
    }

    [HttpPut("{friendshipId}/accept")]
    public async Task<ActionResult> AcceptFriendRequest(int friendshipId)
    {
        var userId = GetCurrentUserId();
        var friendship = await _context.Friendships.FindAsync(friendshipId);

        if (friendship == null)
        {
            return NotFound();
        }

        if (friendship.AddresseeId != userId)
        {
            return Forbid();
        }

        friendship.Status = FriendshipStatus.Accepted;
        friendship.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok();
    }

    [HttpDelete("{friendshipId}")]
    public async Task<ActionResult> RemoveFriend(int friendshipId)
    {
        var userId = GetCurrentUserId();
        var friendship = await _context.Friendships.FindAsync(friendshipId);

        if (friendship == null)
        {
            return NotFound();
        }

        if (friendship.RequesterId != userId && friendship.AddresseeId != userId)
        {
            return Forbid();
        }

        _context.Friendships.Remove(friendship);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
