using Alexandria.Api.Models;

namespace Alexandria.Api.Repositories;

/// <summary>Filters for searching the shared book catalog. Text filters are case-insensitive substring matches.</summary>
public sealed record BookSearchCriteria(
    string? Query = null,
    string? Author = null,
    string? Genre = null,
    string? Isbn = null,
    int? PublishedYear = null);

public interface IBookRepository
{
    Task<Book?> GetByIdAsync(Guid bookId, CancellationToken cancellationToken = default);

    Task<Book?> GetByIsbnAsync(string isbn13, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Book>> SearchAsync(BookSearchCriteria criteria, int skip, int take, CancellationToken cancellationToken = default);

    void Add(Book book);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
