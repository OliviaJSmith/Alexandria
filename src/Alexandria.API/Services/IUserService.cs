using Alexandria.API.DTOs;

namespace Alexandria.API.Services;

public interface IUserService
{
    Task<UserDto?> GetUserByIdAsync(int userId);
    Task<UserDto?> GetUserByGoogleIdAsync(string googleId);
    Task<ServiceResult<UserDto>> CreateUserAsync(CreateUserDto createUserDto);
    Task<ServiceResult<UserDto>> UpdateUserAsync(int userId, UpdateUserDto updateUserDto);
    Task<bool> IsUserNameAvailableAsync(string userName, int? excludeUserId = null);
}
