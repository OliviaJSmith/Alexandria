using System.Net;
using Alexandria.Api.Options;
using Alexandria.Api.Services.BookCatalog;
using Microsoft.Extensions.Logging.Abstractions;

namespace Alexandria.Api.Tests.Services.BookCatalog;

public class GoogleBooksCatalogSourceTests
{
    private const string Isbn = "9780547928227";

    private const string HobbitVolume = """
    {
      "items": [
        {
          "volumeInfo": {
            "title": "The Hobbit",
            "authors": ["J.R.R. Tolkien"],
            "publisher": "HMH",
            "publishedDate": "2012-09-18",
            "description": "There and back again.",
            "pageCount": 300,
            "categories": ["Fiction"],
            "imageLinks": { "thumbnail": "http://books.google.com/books/content?id=1" },
            "industryIdentifiers": [
              { "type": "ISBN_10", "identifier": "054792822X" },
              { "type": "OTHER", "identifier": "UOM:123" },
              { "type": "ISBN_13", "identifier": "9780547928227" }
            ]
          }
        },
        { "volumeInfo": { "authors": ["No title"] } },
        { "id": "no-volume-info" },
        { "volumeInfo": { "title": "Bare" } }
      ]
    }
    """;

    private readonly FakeHttp _http = new();

    [Fact]
    public async Task Lookup_ParsesFirstVolume()
    {
        _http.Json(HobbitVolume);

        var preview = await Source().LookupByIsbnAsync(Isbn);

        Assert.NotNull(preview);
        Assert.Equal("The Hobbit", preview.Title);
        Assert.Equal("J.R.R. Tolkien", preview.Author);
        Assert.Equal(Isbn, preview.Isbn);
        Assert.Equal("HMH", preview.Publisher);
        Assert.Equal(2012, preview.PublishedYear);
        Assert.Equal("There and back again.", preview.Description);
        Assert.Equal(300, preview.PageCount);
        Assert.Equal("Fiction", preview.Genre);
        Assert.Equal("https://books.google.com/books/content?id=1", preview.CoverImageUrl);
        Assert.Contains("q=isbn%3A9780547928227", _http.Requests[0].RequestUri!.Query);
    }

    [Fact]
    public async Task Lookup_VolumeWithoutIsbn_UsesRequestedIsbn()
    {
        _http.Json("""{"items": [{ "volumeInfo": { "title": "Bare" } }]}""");

        var preview = await Source().LookupByIsbnAsync(Isbn);

        Assert.Equal(Isbn, preview!.Isbn);
    }

    [Fact]
    public async Task Lookup_NoItems_ReturnsNull()
    {
        _http.Json("""{"totalItems": 0}""");

        Assert.Null(await Source().LookupByIsbnAsync(Isbn));
    }

    [Fact]
    public async Task Search_SkipsUntitledVolumesAndCombinesQueryWithAuthor()
    {
        _http.Json(HobbitVolume);

        var results = await Source().SearchAsync("hobbit", "Tolkien", 100);

        Assert.Equal(["The Hobbit", "Bare"], results.Select(r => r.Title));
        var query = _http.Requests[0].RequestUri!.Query;
        Assert.Contains("q=hobbit%20inauthor%3ATolkien", query);
        Assert.Contains("maxResults=40", query);
    }

    [Fact]
    public async Task Search_NoTerms_ReturnsEmptyWithoutCallingGoogle()
    {
        Assert.Empty(await Source().SearchAsync(" ", null, 5));
        Assert.Empty(_http.Requests);
    }

    [Fact]
    public async Task ApiKey_IsSentAsHeaderNotInUrl()
    {
        _http.Json(HobbitVolume);

        await Source(apiKey: "secret-key").SearchAsync("hobbit", null, 5);

        var request = _http.Requests[0];
        Assert.Equal("secret-key", Assert.Single(request.Headers.GetValues(GoogleBooksCatalogSource.ApiKeyHeader)));
        Assert.DoesNotContain("secret-key", request.RequestUri!.ToString());
    }

    [Fact]
    public async Task NoApiKey_SendsNoKeyHeader()
    {
        _http.Json(HobbitVolume);

        await Source().SearchAsync("hobbit", null, 5);

        Assert.False(_http.Requests[0].Headers.Contains(GoogleBooksCatalogSource.ApiKeyHeader));
    }

    [Fact]
    public async Task HttpError_ReturnsEmpty()
    {
        _http.Status(HttpStatusCode.Forbidden);

        Assert.Empty(await Source().SearchAsync("hobbit", null, 5));
    }

    [Fact]
    public async Task NetworkFailure_ReturnsNull()
    {
        _http.Throws(new HttpRequestException("down"));

        Assert.Null(await Source().LookupByIsbnAsync(Isbn));
    }

    [Fact]
    public async Task MalformedJson_ReturnsEmpty()
    {
        _http.Json("{ nope");

        Assert.Empty(await Source().SearchAsync("hobbit", null, 5));
    }

    [Fact]
    public async Task UnexpectedShape_ReturnsEmpty()
    {
        _http.Json("[]");

        Assert.Empty(await Source().SearchAsync("hobbit", null, 5));
    }

    [Fact]
    public void Name_IsGoogleBooks()
    {
        Assert.Equal("Google Books", Source().Name);
    }

    private GoogleBooksCatalogSource Source(string? apiKey = null) => new(
        _http.Client,
        Microsoft.Extensions.Options.Options.Create(new GoogleBooksOptions { ApiKey = apiKey }),
        NullLogger<GoogleBooksCatalogSource>.Instance);
}
