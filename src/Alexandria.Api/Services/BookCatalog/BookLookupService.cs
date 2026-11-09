namespace Alexandria.Api.Services.BookCatalog;

public interface IBookLookupService
{
    /// <summary>The first source that knows the ISBN wins.</summary>
    Task<BookPreview?> LookupByIsbnAsync(string isbn13, CancellationToken cancellationToken = default);

    /// <summary>Results from each source in order until <paramref name="limit"/> is reached, without duplicate ISBNs.</summary>
    Task<IReadOnlyList<BookPreview>> SearchAsync(string? query, string? author, int limit, CancellationToken cancellationToken = default);
}

/// <summary>Queries external catalogs in registration order: Open Library first, then Google Books.</summary>
public class BookLookupService(IEnumerable<IBookCatalogSource> sources) : IBookLookupService
{
    public async Task<BookPreview?> LookupByIsbnAsync(string isbn13, CancellationToken cancellationToken = default)
    {
        foreach (var source in sources)
        {
            var preview = await source.LookupByIsbnAsync(isbn13, cancellationToken);
            if (preview is not null)
            {
                return preview;
            }
        }

        return null;
    }

    public async Task<IReadOnlyList<BookPreview>> SearchAsync(
        string? query,
        string? author,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var results = new List<BookPreview>();
        var seenIsbns = new HashSet<string>(StringComparer.Ordinal);

        foreach (var source in sources)
        {
            if (results.Count >= limit)
            {
                break;
            }

            foreach (var preview in await source.SearchAsync(query, author, limit - results.Count, cancellationToken))
            {
                if (preview.Isbn is not null && !seenIsbns.Add(preview.Isbn))
                {
                    continue;
                }

                results.Add(preview);
                if (results.Count >= limit)
                {
                    break;
                }
            }
        }

        return results;
    }
}
