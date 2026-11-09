using Alexandria.Api.Data;
using Alexandria.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Alexandria.Api.Repositories;

public class BookRepository(AlexandriaDbContext context) : IBookRepository
{
    private const string LikeEscape = "\\";

    public async Task<Book?> GetByIdAsync(Guid bookId, CancellationToken cancellationToken = default)
    {
        return await context.Books.FindAsync([bookId], cancellationToken);
    }

    public async Task<Book?> GetByIsbnAsync(string isbn13, CancellationToken cancellationToken = default)
    {
        return await context.Books
            .Where(b => b.Isbn == isbn13)
            .OrderBy(b => b.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Book>> SearchAsync(
        BookSearchCriteria criteria,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var books = context.Books.AsQueryable();

        if (!string.IsNullOrWhiteSpace(criteria.Query))
        {
            var pattern = ContainsPattern(criteria.Query);
            books = books.Where(b =>
                EF.Functions.ILike(b.Title, pattern, LikeEscape)
                || (b.Author != null && EF.Functions.ILike(b.Author, pattern, LikeEscape)));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Author))
        {
            var pattern = ContainsPattern(criteria.Author);
            books = books.Where(b => b.Author != null && EF.Functions.ILike(b.Author, pattern, LikeEscape));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Genre))
        {
            var pattern = ContainsPattern(criteria.Genre);
            books = books.Where(b => b.Genre != null && EF.Functions.ILike(b.Genre, pattern, LikeEscape));
        }

        if (criteria.Isbn is not null)
        {
            books = books.Where(b => b.Isbn == criteria.Isbn);
        }

        if (criteria.PublishedYear is not null)
        {
            books = books.Where(b => b.PublishedYear == criteria.PublishedYear);
        }

        return await books
            .OrderBy(b => b.Title)
            .ThenBy(b => b.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public void Add(Book book)
    {
        context.Books.Add(book);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await context.SaveChangesAsync(cancellationToken);
    }

    // User text is matched literally: LIKE wildcards in it are escaped.
    private static string ContainsPattern(string value)
    {
        var escaped = value.Trim()
            .Replace(LikeEscape, LikeEscape + LikeEscape)
            .Replace("%", LikeEscape + "%")
            .Replace("_", LikeEscape + "_");
        return $"%{escaped}%";
    }
}
