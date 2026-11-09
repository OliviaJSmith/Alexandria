namespace Alexandria.Api.Services.BookCatalog;

/// <summary>Book details from an external catalog. <see cref="Isbn"/> is ISBN-13 when present.</summary>
public sealed record BookPreview(
    string Title,
    string? Author,
    string? Isbn,
    string? Publisher,
    int? PublishedYear,
    string? Description,
    string? CoverImageUrl,
    string? Genre,
    int? PageCount);

/// <summary>
/// An external book catalog (Open Library, Google Books). Implementations handle their own HTTP and
/// parsing failures by logging and returning no results, so one failing source never breaks a lookup.
/// </summary>
public interface IBookCatalogSource
{
    string Name { get; }

    Task<BookPreview?> LookupByIsbnAsync(string isbn13, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BookPreview>> SearchAsync(string? query, string? author, int limit, CancellationToken cancellationToken = default);
}
