using Alexandria.API.Data;
using Alexandria.API.DTOs;
using Alexandria.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Alexandria.API.Services;

public class FriendService(AlexandriaDbContext context, ILogger<FriendService> logger)
    : IFriendService
{
    public async Task<IEnumerable<FriendDto>> GetFriendsAsync(int userId)
    {
        var friendships = await context
            .Friendships.Include(f => f.Requester)
            .Include(f => f.Addressee)
            .Where(f =>
                (f.RequesterId == userId || f.AddresseeId == userId)
                && f.Status == FriendshipStatus.Accepted
            )
            .ToListAsync();

        return friendships.Select(f =>
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
                    UserName = friend.UserName,
                    ProfilePictureUrl = friend.ProfilePictureUrl,
                },
                CreatedAt = f.CreatedAt,
            };
        });
    }

    public async Task<IEnumerable<FriendRequestDto>> GetPendingRequestsAsync(int userId)
    {
        var pendingRequests = await context
            .Friendships.Include(f => f.Requester)
            .Where(f => f.AddresseeId == userId && f.Status == FriendshipStatus.Pending)
            .ToListAsync();

        return pendingRequests.Select(f => new FriendRequestDto
        {
            Id = f.Id,
            FromUser = new UserDto
            {
                Id = f.Requester.Id,
                Email = f.Requester.Email,
                Name = f.Requester.Name,
                ProfilePictureUrl = f.Requester.ProfilePictureUrl,
            },
            CreatedAt = f.CreatedAt,
        });
    }

    public async Task<UserDto?> SearchUserByEmailAsync(int currentUserId, string email)
    {
        var user = await context.Users.FirstOrDefaultAsync(u =>
            u.Email.ToLower() == email.ToLower() && u.Id != currentUserId
        );

        if (user is null)
            return null;

        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            ProfilePictureUrl = user.ProfilePictureUrl,
        };
    }

    public async Task<ServiceResult> SendFriendRequestAsync(int userId, int friendId)
    {
        if (userId == friendId)
            return ServiceResult.Failure("Cannot send friend request to yourself");

        var friend = await context.Users.FindAsync(friendId);
        if (friend is null)
            return ServiceResult.Failure("User not found");

        var existingFriendship = await context.Friendships.FirstOrDefaultAsync(f =>
            (f.RequesterId == userId && f.AddresseeId == friendId)
            || (f.RequesterId == friendId && f.AddresseeId == userId)
        );

        if (existingFriendship is not null)
            return ServiceResult.Failure("Friendship request already exists");

        var friendship = new Friendship
        {
            RequesterId = userId,
            AddresseeId = friendId,
            Status = FriendshipStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        context.Friendships.Add(friendship);
        await context.SaveChangesAsync();

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> AcceptFriendRequestAsync(int userId, int friendshipId)
    {
        var friendship = await context.Friendships.FindAsync(friendshipId);

        if (friendship is null)
            return ServiceResult.Failure("Not found");

        if (friendship.AddresseeId != userId)
            return ServiceResult.Failure("Forbidden");

        friendship.Status = FriendshipStatus.Accepted;
        friendship.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> RemoveFriendAsync(int userId, int friendshipId)
    {
        var friendship = await context.Friendships.FindAsync(friendshipId);

        if (friendship is null)
            return ServiceResult.Failure("Not found");

        if (friendship.RequesterId != userId && friendship.AddresseeId != userId)
            return ServiceResult.Failure("Forbidden");

        context.Friendships.Remove(friendship);
        await context.SaveChangesAsync();

        return ServiceResult.Success();
    }
}
