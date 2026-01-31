using Alexandria.API.Data;
using Alexandria.API.DTOs;
using Alexandria.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Alexandria.API.Services;

public class UserService(AlexandriaDbContext context, ILogger<UserService> logger) : IUserService
{
    public async Task<UserDto?> GetUserByIdAsync(int userId)
    {
        var user = await context.Users.FindAsync(userId);
        return user is null ? null : MapToDto(user);
    }

    public async Task<UserDto?> GetUserByGoogleIdAsync(string googleId)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.GoogleId == googleId);
        return user is null ? null : MapToDto(user);
    }

    public async Task<ServiceResult<UserDto>> CreateUserAsync(CreateUserDto createUserDto)
    {
        // Check if user with GoogleId already exists
        var existingUser = await context.Users.FirstOrDefaultAsync(u => u.GoogleId == createUserDto.GoogleId);
        if (existingUser is not null)
        {
            return ServiceResult<UserDto>.Failure("User with this Google ID already exists");
        }

        // Check if email is already in use
        var existingEmail = await context.Users.FirstOrDefaultAsync(u => u.Email == createUserDto.Email);
        if (existingEmail is not null)
        {
            return ServiceResult<UserDto>.Failure("Email is already in use");
        }

        // Validate UserName if provided
        if (!string.IsNullOrWhiteSpace(createUserDto.UserName))
        {
            if (!await IsUserNameAvailableAsync(createUserDto.UserName))
            {
                return ServiceResult<UserDto>.Failure("Username is already taken");
            }
        }

        var user = new User
        {
            GoogleId = createUserDto.GoogleId,
            Email = createUserDto.Email,
            Name = createUserDto.Name,
            UserName = createUserDto.UserName?.Trim(),
            ProfilePictureUrl = createUserDto.ProfilePictureUrl,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        logger.LogInformation("Created new user with ID {UserId} and email {Email}", user.Id, user.Email);

        return ServiceResult<UserDto>.Success(MapToDto(user));
    }

    public async Task<ServiceResult<UserDto>> UpdateUserAsync(int userId, UpdateUserDto updateUserDto)
    {
        var user = await context.Users.FindAsync(userId);
        if (user is null)
        {
            return ServiceResult<UserDto>.Failure("User not found");
        }

        // Validate UserName if being updated
        if (updateUserDto.UserName is not null)
        {
            var trimmedUserName = updateUserDto.UserName.Trim();
            if (!string.IsNullOrEmpty(trimmedUserName))
            {
                if (!await IsUserNameAvailableAsync(trimmedUserName, userId))
                {
                    return ServiceResult<UserDto>.Failure("Username is already taken");
                }
                user.UserName = trimmedUserName;
            }
            else
            {
                // Allow clearing the username
                user.UserName = null;
            }
        }

        if (updateUserDto.Name is not null)
        {
            user.Name = updateUserDto.Name;
        }

        if (updateUserDto.ProfilePictureUrl is not null)
        {
            user.ProfilePictureUrl = updateUserDto.ProfilePictureUrl;
        }

        user.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        logger.LogInformation("Updated user with ID {UserId}", userId);

        return ServiceResult<UserDto>.Success(MapToDto(user));
    }

    public async Task<bool> IsUserNameAvailableAsync(string userName, int? excludeUserId = null)
    {
        var normalizedUserName = userName.Trim().ToLowerInvariant();
        
        var query = context.Users.Where(u => u.UserName != null && u.UserName.ToLower() == normalizedUserName);
        
        if (excludeUserId.HasValue)
        {
            query = query.Where(u => u.Id != excludeUserId.Value);
        }

        return !await query.AnyAsync();
    }

    private static UserDto MapToDto(User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        Name = user.Name,
        UserName = user.UserName,
        ProfilePictureUrl = user.ProfilePictureUrl
    };
}
