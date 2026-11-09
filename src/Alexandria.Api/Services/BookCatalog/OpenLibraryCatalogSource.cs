using System.Text.Json;
using static Alexandria.Api.Services.BookCatalog.CatalogJson;

namespace Alexandria.Api.Services.BookCatalog;

/// <summary>Open Library (openlibrary.org): free, no key. Primary source.</summary>
public class OpenLibraryCatalogSource(HttpClient httpClient, ILogger<OpenLibraryCatalogSource> logger) : IBookCatalogSource
{
    public const string BaseUrl = "https://openlibrary.org";
    private const string SearchFields = "title,author_name,isbn,publisher,first_publish_year,cover_i,number_of_pages_median,subject";

    public string Name => "Open Library";

    public async Task<BookPreview?> LookupByIsbnAsync(string isbn13, CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrl}/api/books?bibkeys=ISBN:{Uri.EscapeDataString(isbn13)}&format=json&jscmd=data";
        using var document = await GetJsonAsync(url, cancellationToken);
        if (document is null
            || document.RootElement.ValueKind != JsonValueKind.Object
            || !document.RootElement.TryGetProperty($"ISBN:{isbn13}", out var edition))
        {
            return null;
        }

        var title = GetString(edition, "title");
        if (title is null)
        {
            return null;
        }

        var cover = edition.TryGetProperty("cover", out var covers) ? GetString(covers, "large") ?? GetString(covers, "medium") : null;
        return new BookPreview(
            Title: title,
            Author: JoinOrNull(GetStrings(edition, "authors", "name")),
            Isbn: isbn13,
            Publisher: GetStrings(edition, "publishers", "name").FirstOrDefault(),
            PublishedYear: ParseYear(GetString(edition, "publish_date")),
            Description: null,
            CoverImageUrl: HttpsUrl(cover),
            Genre: GetStrings(edition, "subjects", "name").FirstOrDefault(),
            PageCount: GetInt(edition, "number_of_pages"));
    }

    public async Task<IReadOnlyList<BookPreview>> SearchAsync(
        string? query,
        string? author,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var parameters = new List<string> { $"limit={limit}", $"fields={SearchFields}" };
        if (!string.IsNullOrWhiteSpace(query))
        {
            parameters.Add($"q={Uri.EscapeDataString(query.Trim())}");
        }

        if (!string.IsNullOrWhiteSpace(author))
        {
            parameters.Add($"author={Uri.EscapeDataString(author.Trim())}");
        }

        using var document = await GetJsonAsync($"{BaseUrl}/search.json?{string.Join('&', parameters)}", cancellationToken);
        if (document is null
            || document.RootElement.ValueKind != JsonValueKind.Object
            || !document.RootElement.TryGetProperty("docs", out var docs)
            || docs.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var results = new List<BookPreview>();
        foreach (var doc in docs.EnumerateArray())
        {
            var title = GetString(doc, "title");
            if (title is null)
            {
                continue;
            }

            var coverId = GetInt(doc, "cover_i");
            results.Add(new BookPreview(
                Title: title,
                Author: JoinOrNull(GetStrings(doc, "author_name")),
                Isbn: FirstValidIsbn(GetStrings(doc, "isbn")),
                Publisher: GetStrings(doc, "publisher").FirstOrDefault(),
                PublishedYear: GetInt(doc, "first_publish_year"),
                Description: null,
                CoverImageUrl: coverId is null ? null : $"https://covers.openlibrary.org/b/id/{coverId}-L.jpg",
                Genre: GetStrings(doc, "subject").FirstOrDefault(),
                PageCount: GetInt(doc, "number_of_pages_median")));
        }

        return results;
    }

    private async Task<JsonDocument?> GetJsonAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Open Library returned status {StatusCode}", (int)response.StatusCode);
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException
            || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            logger.LogWarning(ex, "Open Library request failed");
            return null;
        }
    }
}
