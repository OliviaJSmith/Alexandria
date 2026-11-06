using Alexandria.Api.Data;
using Alexandria.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Alexandria.Api.Repositories;

public class UserRepository(AlexandriaDbContext context) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Users.FindAsync([id], cancellationToken);
    }

    public async Task<User?> GetByGoogleSubjectIdAsync(string googleSubjectId, CancellationToken cancellationToken = default)
    {
        return await context.Users.SingleOrDefaultAsync(u => u.GoogleSubjectId == googleSubjectId, cancellationToken);
    }

    public async Task<bool> NormalizedUserNameExistsAsync(string normalizedUserName, CancellationToken cancellationToken = default)
    {
        return await context.Users.AnyAsync(u => u.NormalizedUserName == normalizedUserName, cancellationToken);
    }

    public void Add(User user)
    {
        context.Users.Add(user);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await context.SaveChangesAsync(cancellationToken);
    }
}
