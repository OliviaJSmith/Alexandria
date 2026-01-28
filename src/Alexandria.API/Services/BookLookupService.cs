using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Alexandria.API.DTOs;
using Alexandria.API.Utilities;

namespace Alexandria.API.Services;

/// <summary>
/// Looks up book information from Open Library (primary) and Google Books (fallback).
/// Implements rate limiting for Open Library courtesy (~1 request/second).
/// </summary>
public class BookLookupService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<BookLookupService> logger
) : IBookLookupService
{
    private readonly SemaphoreSlim _rateLimiter = new(1, 1);
    private DateTime _lastOpenLibraryRequest = DateTime.MinValue;

    private readonly int _requestDelayMs = configuration.GetValue(
        "OpenLibrary:RequestDelayMs",
        1000
    );
    private readonly string _googleBooksApiKey = configuration["GoogleBooks:ApiKey"] ?? "";

    public async Task<BookPreviewDto?> LookupByIsbnAsync(
        string isbn,
        CancellationToken cancellationToken = default
    )
    {
        var normalizedIsbn = IsbnHelper.NormalizeToIsbn13(isbn);
        if (normalizedIsbn is null)
        {
            logger.LogWarning("Invalid ISBN provided: {Isbn}", isbn);
            return null;
        }

        // Try Open Library first
        var result = await LookupFromOpenLibraryAsync(normalizedIsbn, cancellationToken);
        if (result is not null)
            return result;

        // Fallback to Google Books
        result = await LookupFromGoogleBooksAsync(normalizedIsbn, cancellationToken);
        return result;
    }

    public async Task<IEnumerable<BookPreviewDto>> SearchAsync(
        string title,
        string? author = null,
        int maxResults = 5,
        CancellationToken cancellationToken = default
    )
    {
        var results = new List<BookPreviewDto>();

        // Try Open Library search first
        var openLibraryResults = await SearchOpenLibraryAsync(
            title,
            author,
            maxResults,
            cancellationToken
        );
        results.AddRange(openLibraryResults);

        // If we don't have enough results, try Google Books
        if (results.Count < maxResults)
        {
            var googleResults = await SearchGoogleBooksAsync(
                title,
                author,
                maxResults - results.Count,
                cancellationToken
            );
            results.AddRange(googleResults);
        }

        return results.Take(maxResults);
    }

    public async Task<IEnumerable<BookPreviewDto>> LookupMultipleIsbnsAsync(
        IEnumerable<string> isbns,
        CancellationToken cancellationToken = default
    )
    {
        var results = new List<BookPreviewDto>();

        foreach (var isbn in isbns.Distinct())
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var result = await LookupByIsbnAsync(isbn, cancellationToken);
            if (result is not null)
                results.Add(result);
        }

        return results;
    }

    #region Open Library API

    private async Task<BookPreviewDto?> LookupFromOpenLibraryAsync(
        string isbn,
        CancellationToken cancellationToken
    )
    {
        await ThrottleOpenLibraryRequestAsync(cancellationToken);

        try
        {
            var client = httpClientFactory.CreateClient("OpenLibrary");
            var response = await client.GetAsync($"/isbn/{isbn}.json", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogDebug(
                    "Open Library ISBN lookup failed for {Isbn}: {StatusCode}",
                    isbn,
                    response.StatusCode
                );
                return null;
            }

            var edition = await response.Content.ReadFromJsonAsync<OpenLibraryEdition>(
                cancellationToken: cancellationToken
            );
            if (edition is null)
                return null;

            // Get additional work details if available
            string? description = null;
            string? coverUrl = null;

            if (edition.Covers?.Count > 0)
            {
                coverUrl = $"https://covers.openlibrary.org/b/id/{edition.Covers[0]}-L.jpg";
            }

            if (edition.Works?.Count > 0)
            {
                await ThrottleOpenLibraryRequestAsync(cancellationToken);
                var workKey = edition.Works[0].Key;
                var workResponse = await client.GetAsync($"{workKey}.json", cancellationToken);
                if (workResponse.IsSuccessStatusCode)
                {
                    var work = await workResponse.Content.ReadFromJsonAsync<OpenLibraryWork>(
                        cancellationToken: cancellationToken
                    );
                    description = ExtractDescription(work?.Description);
                }
            }

            // Get author names
            var authorNames = new List<string>();
            if (edition.Authors?.Count > 0)
            {
                foreach (var authorRef in edition.Authors.Take(3))
                {
                    await ThrottleOpenLibraryRequestAsync(cancellationToken);
                    var authorResponse = await client.GetAsync(
                        $"{authorRef.Key}.json",
                        cancellationToken
                    );
                    if (authorResponse.IsSuccessStatusCode)
                    {
                        var author =
                            await authorResponse.Content.ReadFromJsonAsync<OpenLibraryAuthor>(
                                cancellationToken: cancellationToken
                            );
                        if (author?.Name is not null)
                            authorNames.Add(author.Name);
                    }
                }
            }

            return new BookPreviewDto
            {
                Title = edition.Title ?? "Unknown Title",
                Author = authorNames.Count > 0 ? string.Join(", ", authorNames) : null,
                Isbn = isbn,
                Publisher = edition.Publishers?.FirstOrDefault(),
                PublishedYear = ParseYear(edition.PublishDate),
                Description = description,
                CoverImageUrl = coverUrl,
                PageCount = edition.NumberOfPages,
                Source = BookSource.OpenLibrary,
                ExternalId = edition.Key,
                Confidence = 1.0,
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error looking up ISBN {Isbn} from Open Library", isbn);
            return null;
        }
    }

    private async Task<IEnumerable<BookPreviewDto>> SearchOpenLibraryAsync(
        string title,
        string? author,
        int maxResults,
        CancellationToken cancellationToken
    )
    {
        await ThrottleOpenLibraryRequestAsync(cancellationToken);

        try
        {
            var client = httpClientFactory.CreateClient("OpenLibrary");
            var query = Uri.EscapeDataString(title);
            if (!string.IsNullOrEmpty(author))
                query += $"+author:{Uri.EscapeDataString(author)}";

            var response = await client.GetAsync(
                $"/search.json?q={query}&limit={maxResults}",
                cancellationToken
            );

            if (!response.IsSuccessStatusCode)
                return [];

            var searchResult = await response.Content.ReadFromJsonAsync<OpenLibrarySearchResult>(
                cancellationToken: cancellationToken
            );
            if (searchResult?.Docs is null)
                return [];

            return searchResult
                .Docs.Take(maxResults)
                .Select(doc => new BookPreviewDto
                {
                    Title = doc.Title ?? "Unknown Title",
                    Author = doc.AuthorName?.FirstOrDefault(),
                    Isbn = doc.Isbn?.FirstOrDefault(),
                    PublishedYear = doc.FirstPublishYear,
                    CoverImageUrl = doc.CoverId.HasValue
                        ? $"https://covers.openlibrary.org/b/id/{doc.CoverId}-L.jpg"
                        : null,
                    Source = BookSource.OpenLibrary,
                    ExternalId = doc.Key,
                    Confidence = 0.9,
                });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error searching Open Library for '{Title}'", title);
            return [];
        }
    }

    private async Task ThrottleOpenLibraryRequestAsync(CancellationToken cancellationToken)
    {
        await _rateLimiter.WaitAsync(cancellationToken);
        try
        {
            var elapsed = DateTime.UtcNow - _lastOpenLibraryRequest;
            if (elapsed.TotalMilliseconds < _requestDelayMs)
            {
                var delay = _requestDelayMs - (int)elapsed.TotalMilliseconds;
                await Task.Delay(delay, cancellationToken);
            }
            _lastOpenLibraryRequest = DateTime.UtcNow;
        }
        finally
        {
            _rateLimiter.Release();
        }
    }

    #endregion

    #region Google Books API

    private async Task<BookPreviewDto?> LookupFromGoogleBooksAsync(
        string isbn,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var client = httpClientFactory.CreateClient("GoogleBooks");
            var url = $"/volumes?q=isbn:{isbn}";
            if (!string.IsNullOrEmpty(_googleBooksApiKey))
                url += $"&key={_googleBooksApiKey}";

            var response = await client.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogDebug(
                    "Google Books ISBN lookup failed for {Isbn}: {StatusCode}",
                    isbn,
                    response.StatusCode
                );
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<GoogleBooksSearchResult>(
                cancellationToken: cancellationToken
            );
            var item = result?.Items?.FirstOrDefault();
            if (item?.VolumeInfo is null)
                return null;

            var volumeInfo = item.VolumeInfo;
            return new BookPreviewDto
            {
                Title = volumeInfo.Title ?? "Unknown Title",
                Author = volumeInfo.Authors is not null
                    ? string.Join(", ", volumeInfo.Authors)
                    : null,
                Isbn = isbn,
                Publisher = volumeInfo.Publisher,
                PublishedYear = ParseYear(volumeInfo.PublishedDate),
                Description = volumeInfo.Description,
                CoverImageUrl = volumeInfo.ImageLinks?.Thumbnail?.Replace("http://", "https://"),
                Genre = volumeInfo.Categories?.FirstOrDefault(),
                PageCount = volumeInfo.PageCount,
                Source = BookSource.GoogleBooks,
                ExternalId = item.Id,
                Confidence = 1.0,
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error looking up ISBN {Isbn} from Google Books", isbn);
            return null;
        }
    }

    private async Task<IEnumerable<BookPreviewDto>> SearchGoogleBooksAsync(
        string title,
        string? author,
        int maxResults,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var client = httpClientFactory.CreateClient("GoogleBooks");
            var query = Uri.EscapeDataString(title);
            if (!string.IsNullOrEmpty(author))
                query += $"+inauthor:{Uri.EscapeDataString(author)}";

            var url = $"/volumes?q={query}&maxResults={maxResults}";
            if (!string.IsNullOrEmpty(_googleBooksApiKey))
                url += $"&key={_googleBooksApiKey}";

            var response = await client.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
                return [];

            var result = await response.Content.ReadFromJsonAsync<GoogleBooksSearchResult>(
                cancellationToken: cancellationToken
            );
            if (result?.Items is null)
                return [];

            return result
                .Items.Take(maxResults)
                .Select(item =>
                {
                    var volumeInfo = item.VolumeInfo!;
                    var isbn =
                        volumeInfo
                            .IndustryIdentifiers?.FirstOrDefault(id => id.Type == "ISBN_13")
                            ?.Identifier
                        ?? volumeInfo.IndustryIdentifiers?.FirstOrDefault()?.Identifier;

                    return new BookPreviewDto
                    {
                        Title = volumeInfo.Title ?? "Unknown Title",
                        Author = volumeInfo.Authors is not null
                            ? string.Join(", ", volumeInfo.Authors)
                            : null,
                        Isbn = isbn is not null ? IsbnHelper.NormalizeToIsbn13(isbn) : null,
                        Publisher = volumeInfo.Publisher,
                        PublishedYear = ParseYear(volumeInfo.PublishedDate),
                        Description = volumeInfo.Description,
                        CoverImageUrl = volumeInfo.ImageLinks?.Thumbnail?.Replace(
                            "http://",
                            "https://"
                        ),
                        Genre = volumeInfo.Categories?.FirstOrDefault(),
                        PageCount = volumeInfo.PageCount,
                        Source = BookSource.GoogleBooks,
                        ExternalId = item.Id,
                        Confidence = 0.9,
                    };
                });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error searching Google Books for '{Title}'", title);
            return [];
        }
    }

    #endregion

    private static int? ParseYear(string? dateString)
    {
        if (string.IsNullOrEmpty(dateString))
            return null;

        // Try to extract a 4-digit year from the string
        if (
            dateString.Length >= 4
            && int.TryParse(dateString[..4], out var year)
            && year >= 1000
            && year <= 9999
        )
            return year;

        return null;
    }

    /// <summary>
    /// Extracts description from Open Library's description field which can be either a string or an object with a "value" property.
    /// </summary>
    private static string? ExtractDescription(JsonElement? element)
    {
        if (
            element is null
            || element.Value.ValueKind == JsonValueKind.Undefined
            || element.Value.ValueKind == JsonValueKind.Null
        )
            return null;

        // If it's a string, return it directly
        if (element.Value.ValueKind == JsonValueKind.String)
            return element.Value.GetString();

        // If it's an object with a "value" property, extract that
        if (
            element.Value.ValueKind == JsonValueKind.Object
            && element.Value.TryGetProperty("value", out var valueProperty)
        )
            return valueProperty.GetString();

        return null;
    }

    #region API Response Models

    private class OpenLibraryEdition
    {
        [JsonPropertyName("key")]
        public string? Key { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("authors")]
        public List<OpenLibraryRef>? Authors { get; set; }

        [JsonPropertyName("publishers")]
        public List<string>? Publishers { get; set; }

        [JsonPropertyName("publish_date")]
        public string? PublishDate { get; set; }

        [JsonPropertyName("number_of_pages")]
        public int? NumberOfPages { get; set; }

        [JsonPropertyName("covers")]
        public List<int>? Covers { get; set; }

        [JsonPropertyName("works")]
        public List<OpenLibraryRef>? Works { get; set; }
    }

    private class OpenLibraryRef
    {
        [JsonPropertyName("key")]
        public string? Key { get; set; }
    }

    private class OpenLibraryWork
    {
        [JsonPropertyName("description")]
        public JsonElement? Description { get; set; }
    }

    private class OpenLibraryAuthor
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    private class OpenLibrarySearchResult
    {
        [JsonPropertyName("docs")]
        public List<OpenLibrarySearchDoc>? Docs { get; set; }
    }

    private class OpenLibrarySearchDoc
    {
        [JsonPropertyName("key")]
        public string? Key { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("author_name")]
        public List<string>? AuthorName { get; set; }

        [JsonPropertyName("isbn")]
        public List<string>? Isbn { get; set; }

        [JsonPropertyName("first_publish_year")]
        public int? FirstPublishYear { get; set; }

        [JsonPropertyName("cover_i")]
        public int? CoverId { get; set; }
    }

    private class GoogleBooksSearchResult
    {
        [JsonPropertyName("items")]
        public List<GoogleBooksItem>? Items { get; set; }
    }

    private class GoogleBooksItem
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("volumeInfo")]
        public GoogleBooksVolumeInfo? VolumeInfo { get; set; }
    }

    private class GoogleBooksVolumeInfo
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("authors")]
        public List<string>? Authors { get; set; }

        [JsonPropertyName("publisher")]
        public string? Publisher { get; set; }

        [JsonPropertyName("publishedDate")]
        public string? PublishedDate { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("pageCount")]
        public int? PageCount { get; set; }

        [JsonPropertyName("categories")]
        public List<string>? Categories { get; set; }

        [JsonPropertyName("imageLinks")]
        public GoogleBooksImageLinks? ImageLinks { get; set; }

        [JsonPropertyName("industryIdentifiers")]
        public List<GoogleBooksIdentifier>? IndustryIdentifiers { get; set; }
    }

    private class GoogleBooksImageLinks
    {
        [JsonPropertyName("thumbnail")]
        public string? Thumbnail { get; set; }
    }

    private class GoogleBooksIdentifier
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("identifier")]
        public string? Identifier { get; set; }
    }

    #endregion
}
