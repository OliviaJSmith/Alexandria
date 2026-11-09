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

    public async Task<User?> GetByNormalizedUserNameAsync(string normalizedUserName, CancellationToken cancellationToken = default)
    {
        return await context.Users.SingleOrDefaultAsync(u => u.NormalizedUserName == normalizedUserName, cancellationToken);
    }

    public async Task<IReadOnlyList<User>> SearchByUserNamePrefixAsync(
        string normalizedPrefix,
        Guid excludeUserId,
        int take,
        CancellationToken cancellationToken = default)
    {
        // Usernames only contain letters, digits and underscores; escape the underscore LIKE wildcard.
        var pattern = normalizedPrefix.Replace("\\", "\\\\").Replace("_", "\\_").Replace("%", "\\%") + "%";
        return await context.Users
            .Where(u => u.Id != excludeUserId && EF.Functions.Like(u.NormalizedUserName, pattern, "\\"))
            .OrderBy(u => u.NormalizedUserName)
            .Take(take)
            .ToListAsync(cancellationToken);
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
