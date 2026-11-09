using Alexandria.Api.Data;
using Alexandria.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Alexandria.Api.Repositories;

public class LibraryRepository(AlexandriaDbContext context) : ILibraryRepository
{
    public async Task<Library?> GetByIdAsync(Guid libraryId, CancellationToken cancellationToken = default)
    {
        return await context.Libraries.FindAsync([libraryId], cancellationToken);
    }

    public async Task<IReadOnlyList<Library>> GetByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        return await context.Libraries
            .Where(l => l.OwnerId == ownerId)
            .OrderBy(l => l.Name)
            .ToListAsync(cancellationToken);
    }

    public void Add(Library library)
    {
        context.Libraries.Add(library);
    }

    public async Task<IReadOnlyList<LibraryBook>> GetLibraryBooksAsync(Guid libraryId, CancellationToken cancellationToken = default)
    {
        return await context.LibraryBooks
            .Include(lb => lb.Book)
            .Where(lb => lb.LibraryId == libraryId)
            .OrderByDescending(lb => lb.AddedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<LibraryBook?> GetLibraryBookAsync(Guid libraryId, Guid libraryBookId, CancellationToken cancellationToken = default)
    {
        return await context.LibraryBooks
            .Include(lb => lb.Book)
            .SingleOrDefaultAsync(lb => lb.Id == libraryBookId && lb.LibraryId == libraryId, cancellationToken);
    }

    public async Task<bool> ContainsBookAsync(Guid libraryId, Guid bookId, CancellationToken cancellationToken = default)
    {
        return await context.LibraryBooks.AnyAsync(lb => lb.LibraryId == libraryId && lb.BookId == bookId, cancellationToken);
    }

    public async Task<IReadOnlyList<LibraryBook>> GetOwnerBooksByStatusAsync(
        Guid ownerId,
        LibraryBookStatus status,
        CancellationToken cancellationToken = default)
    {
        return await context.LibraryBooks
            .Include(lb => lb.Book)
            .Where(lb => lb.Library!.OwnerId == ownerId && lb.Status == status)
            .OrderBy(lb => lb.Book!.Title)
            .ToListAsync(cancellationToken);
    }

    public void AddLibraryBook(LibraryBook libraryBook)
    {
        context.LibraryBooks.Add(libraryBook);
    }

    public void RemoveLibraryBook(LibraryBook libraryBook)
    {
        context.LibraryBooks.Remove(libraryBook);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await context.SaveChangesAsync(cancellationToken);
    }
}
