using Alexandria.Api.Dtos;

namespace Alexandria.Api.Services;

public interface IUserService
{
    Task<ServiceResult<UserDto>> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
