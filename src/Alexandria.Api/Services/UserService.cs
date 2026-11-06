using Alexandria.Api.Dtos;
using Alexandria.Api.Repositories;

namespace Alexandria.Api.Services;

public class UserService(IUserRepository users) : IUserService
{
    public const string UserNotFoundMessage = "User not found.";

    public async Task<ServiceResult<UserDto>> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await users.GetByIdAsync(userId, cancellationToken);
        return user is null
            ? ServiceResult<UserDto>.Failure(ServiceErrorType.NotFound, UserNotFoundMessage)
            : ServiceResult<UserDto>.Success(UserDto.FromUser(user));
    }
}
