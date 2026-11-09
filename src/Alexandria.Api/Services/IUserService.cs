using Alexandria.Api.Dtos;

namespace Alexandria.Api.Services;

public interface IUserService
{
    Task<ServiceResult<UserDto>> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Changes the user's username. Conflict if another account already uses it (case-insensitive).</summary>
    Task<ServiceResult<UserDto>> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken = default);

    /// <summary>Whether a username is well-formed and free (the caller's own username counts as available).</summary>
    Task<ServiceResult<UserNameAvailabilityDto>> CheckUserNameAsync(Guid userId, string userName, CancellationToken cancellationToken = default);
}
