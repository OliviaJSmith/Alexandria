using System.Text.RegularExpressions;
using Alexandria.Api.Dtos;
using Alexandria.Api.Repositories;

namespace Alexandria.Api.Services;

public partial class UserService(IUserRepository users, TimeProvider timeProvider) : IUserService
{
    public const string UserNotFoundMessage = "User not found.";
    public const string InvalidUserNameMessage = "Usernames are 3–30 letters, numbers or underscores.";
    public const string UserNameTakenMessage = "That username is already taken.";

    public async Task<ServiceResult<UserDto>> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await users.GetByIdAsync(userId, cancellationToken);
        return user is null
            ? ServiceResult<UserDto>.Failure(ServiceErrorType.NotFound, UserNotFoundMessage)
            : ServiceResult<UserDto>.Success(UserDto.FromUser(user));
    }

    public async Task<ServiceResult<UserDto>> UpdateProfileAsync(
        Guid userId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var userName = request.UserName.Trim();
        if (!IsValidUserName(userName))
        {
            return ServiceResult<UserDto>.Failure(ServiceErrorType.Validation, InvalidUserNameMessage);
        }

        var user = await users.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return ServiceResult<UserDto>.Failure(ServiceErrorType.NotFound, UserNotFoundMessage);
        }

        var normalized = UsernameGenerator.Normalize(userName);
        if (normalized != user.NormalizedUserName && await users.NormalizedUserNameExistsAsync(normalized, cancellationToken))
        {
            return ServiceResult<UserDto>.Failure(ServiceErrorType.Conflict, UserNameTakenMessage);
        }

        user.UserName = userName;
        user.NormalizedUserName = normalized;
        user.UpdatedAt = timeProvider.GetUtcNow();
        await users.SaveChangesAsync(cancellationToken);
        return ServiceResult<UserDto>.Success(UserDto.FromUser(user));
    }

    public async Task<ServiceResult<UserNameAvailabilityDto>> CheckUserNameAsync(
        Guid userId,
        string userName,
        CancellationToken cancellationToken = default)
    {
        var trimmed = userName.Trim();
        if (!IsValidUserName(trimmed))
        {
            return ServiceResult<UserNameAvailabilityDto>.Success(new UserNameAvailabilityDto(trimmed, IsValid: false, IsAvailable: false));
        }

        var owner = await users.GetByNormalizedUserNameAsync(UsernameGenerator.Normalize(trimmed), cancellationToken);
        return ServiceResult<UserNameAvailabilityDto>.Success(
            new UserNameAvailabilityDto(trimmed, IsValid: true, IsAvailable: owner is null || owner.Id == userId));
    }

    public static bool IsValidUserName(string userName) => UserNamePattern().IsMatch(userName);

    [GeneratedRegex("^[A-Za-z0-9_]{3,30}$")]
    private static partial Regex UserNamePattern();
}
