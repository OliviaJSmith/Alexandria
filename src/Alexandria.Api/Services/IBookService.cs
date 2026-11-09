using Alexandria.Api.Dtos;

namespace Alexandria.Api.Services;

/// <summary>The shared book catalog, backed by Open Library and Google Books for books Alexandria doesn't have yet.</summary>
public interface IBookService
{
    /// <summary>
    /// Searches Alexandria's catalog; if nothing matches on the first page, searches the external catalogs
    /// (results have a null id until created).
    /// </summary>
    Task<ServiceResult<IReadOnlyList<BookDto>>> SearchAsync(BookSearchQuery query, CancellationToken cancellationToken = default);

    Task<ServiceResult<BookDto>> GetByIdAsync(Guid bookId, CancellationToken cancellationToken = default);

    /// <summary>Adds a book to the catalog, or returns the existing entry with the same ISBN.</summary>
    Task<ServiceResult<CreateBookResult>> CreateAsync(CreateBookRequest request, CancellationToken cancellationToken = default);

    /// <summary>Finds a book by ISBN in Alexandria's catalog, then in the external catalogs.</summary>
    Task<ServiceResult<BookDto>> LookupByIsbnAsync(string isbn, CancellationToken cancellationToken = default);
}
