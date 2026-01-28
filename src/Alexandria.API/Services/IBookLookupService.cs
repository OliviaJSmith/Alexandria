using Alexandria.API.DTOs;

namespace Alexandria.API.Services;

/// <summary>
/// Service for looking up book information from external APIs.
/// Uses Open Library as primary source, Google Books as fallback.
/// </summary>
public interface IBookLookupService
{
    /// <summary>
    /// Looks up a book by ISBN. Returns null if not found.
    /// Tries Open Library first, then Google Books.
    /// </summary>
    Task<BookPreviewDto?> LookupByIsbnAsync(string isbn, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for books by title and optional author.
    /// Returns multiple potential matches.
    /// </summary>
    Task<IEnumerable<BookPreviewDto>> SearchAsync(string title, string? author = null, int maxResults = 5, CancellationToken cancellationToken = default);

    /// <summary>
    /// Looks up multiple ISBNs with rate limiting.
    /// Useful for bookshelf scans with multiple detected ISBNs.
    /// </summary>
    Task<IEnumerable<BookPreviewDto>> LookupMultipleIsbnsAsync(IEnumerable<string> isbns, CancellationToken cancellationToken = default);
}
