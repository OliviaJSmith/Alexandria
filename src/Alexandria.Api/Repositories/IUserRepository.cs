using Alexandria.Api.Models;

namespace Alexandria.Api.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<User?> GetByGoogleSubjectIdAsync(string googleSubjectId, CancellationToken cancellationToken = default);

    Task<bool> NormalizedUserNameExistsAsync(string normalizedUserName, CancellationToken cancellationToken = default);

    void Add(User user);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
