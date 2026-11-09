using System.Text.Json;
using Alexandria.Api.Options;
using Microsoft.Extensions.Options;
using static Alexandria.Api.Services.BookCatalog.CatalogJson;

namespace Alexandria.Api.Services.BookCatalog;

/// <summary>Google Books: works without a key at low volume; an optional key raises the quota. Fallback source.</summary>
public class GoogleBooksCatalogSource(
    HttpClient httpClient,
    IOptions<GoogleBooksOptions> options,
    ILogger<GoogleBooksCatalogSource> logger) : IBookCatalogSource
{
    public const string VolumesUrl = "https://www.googleapis.com/books/v1/volumes";
    public const string ApiKeyHeader = "X-goog-api-key";

    public string Name => "Google Books";

    public async Task<BookPreview?> LookupByIsbnAsync(string isbn13, CancellationToken cancellationToken = default)
    {
        var results = await QueryAsync($"isbn:{isbn13}", 1, cancellationToken);
        return results.Count == 0 ? null : results[0] with { Isbn = results[0].Isbn ?? isbn13 };
    }

    public async Task<IReadOnlyList<BookPreview>> SearchAsync(
        string? query,
        string? author,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var terms = new List<string>();
        if (!string.IsNullOrWhiteSpace(query))
        {
            terms.Add(query.Trim());
        }

        if (!string.IsNullOrWhiteSpace(author))
        {
            terms.Add($"inauthor:{author.Trim()}");
        }

        return terms.Count == 0 ? [] : await QueryAsync(string.Join(' ', terms), limit, cancellationToken);
    }

    private async Task<IReadOnlyList<BookPreview>> QueryAsync(string q, int limit, CancellationToken cancellationToken)
    {
        var url = $"{VolumesUrl}?q={Uri.EscapeDataString(q)}&maxResults={Math.Clamp(limit, 1, 40)}&printType=books";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (!string.IsNullOrEmpty(options.Value.ApiKey))
        {
            // Header rather than ?key= so the key never appears in logged URLs.
            request.Headers.Add(ApiKeyHeader, options.Value.ApiKey);
        }

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Google Books returned status {StatusCode}", (int)response.StatusCode);
                return [];
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            return ParseVolumes(document.RootElement);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException
            || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            logger.LogWarning(ex, "Google Books request failed");
            return [];
        }
    }

    private static List<BookPreview> ParseVolumes(JsonElement root)
    {
        var results = new List<BookPreview>();
        if (root.ValueKind != JsonValueKind.Object
            || !root.TryGetProperty("items", out var items)
            || items.ValueKind != JsonValueKind.Array)
        {
            return results;
        }

        foreach (var item in items.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object || !item.TryGetProperty("volumeInfo", out var info))
            {
                continue;
            }

            var title = GetString(info, "title");
            if (title is null)
            {
                continue;
            }

            var isbns = info.TryGetProperty("industryIdentifiers", out var identifiers) && identifiers.ValueKind == JsonValueKind.Array
                ? identifiers.EnumerateArray()
                    .Where(i => GetString(i, "type") is "ISBN_13" or "ISBN_10")
                    .OrderBy(i => GetString(i, "type") == "ISBN_13" ? 0 : 1)
                    .Select(i => GetString(i, "identifier"))
                    .OfType<string>()
                : [];
            var thumbnail = info.TryGetProperty("imageLinks", out var images) ? GetString(images, "thumbnail") : null;

            results.Add(new BookPreview(
                Title: title,
                Author: JoinOrNull(GetStrings(info, "authors")),
                Isbn: FirstValidIsbn(isbns),
                Publisher: GetString(info, "publisher"),
                PublishedYear: ParseYear(GetString(info, "publishedDate")),
                Description: GetString(info, "description"),
                CoverImageUrl: HttpsUrl(thumbnail),
                Genre: GetStrings(info, "categories").FirstOrDefault(),
                PageCount: GetInt(info, "pageCount")));
        }

        return results;
    }
}
