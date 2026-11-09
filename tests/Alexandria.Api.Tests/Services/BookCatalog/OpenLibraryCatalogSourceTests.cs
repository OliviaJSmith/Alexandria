using System.Net;
using Alexandria.Api.Services.BookCatalog;
using Microsoft.Extensions.Logging.Abstractions;

namespace Alexandria.Api.Tests.Services.BookCatalog;

public class OpenLibraryCatalogSourceTests
{
    private const string Isbn = "9780547928227";

    private readonly FakeHttp _http = new();
    private readonly OpenLibraryCatalogSource _source;

    public OpenLibraryCatalogSourceTests()
    {
        _source = new OpenLibraryCatalogSource(_http.Client, NullLogger<OpenLibraryCatalogSource>.Instance);
    }

    [Fact]
    public async Task Lookup_ParsesBooksApiResponse()
    {
        _http.Json($$"""
        {
          "ISBN:{{Isbn}}": {
            "title": "The Hobbit",
            "authors": [{ "name": "J.R.R. Tolkien" }, { "name": "Alan Lee" }],
            "publishers": [{ "name": "Houghton Mifflin Harcourt" }],
            "publish_date": "September 18, 2012",
            "number_of_pages": 300,
            "cover": { "medium": "http://covers.openlibrary.org/b/id/1-M.jpg", "large": "http://covers.openlibrary.org/b/id/1-L.jpg" },
            "subjects": [{ "name": "Fantasy fiction" }, { "name": "Hobbits" }]
          }
        }
        """);

        var preview = await _source.LookupByIsbnAsync(Isbn);

        Assert.NotNull(preview);
        Assert.Equal("The Hobbit", preview.Title);
        Assert.Equal("J.R.R. Tolkien, Alan Lee", preview.Author);
        Assert.Equal(Isbn, preview.Isbn);
        Assert.Equal("Houghton Mifflin Harcourt", preview.Publisher);
        Assert.Equal(2012, preview.PublishedYear);
        Assert.Equal(300, preview.PageCount);
        Assert.Equal("https://covers.openlibrary.org/b/id/1-L.jpg", preview.CoverImageUrl);
        Assert.Equal("Fantasy fiction", preview.Genre);
        Assert.Equal($"{OpenLibraryCatalogSource.BaseUrl}/api/books?bibkeys=ISBN:{Isbn}&format=json&jscmd=data", _http.Requests[0].RequestUri!.ToString());
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData("""{"ISBN:9780547928227": { "publishers": [] }}""")]
    public async Task Lookup_UnknownOrUntitledEdition_ReturnsNull(string json)
    {
        _http.Json(json);

        Assert.Null(await _source.LookupByIsbnAsync(Isbn));
    }

    [Fact]
    public async Task Lookup_HttpError_ReturnsNull()
    {
        _http.Status(HttpStatusCode.ServiceUnavailable);

        Assert.Null(await _source.LookupByIsbnAsync(Isbn));
    }

    [Fact]
    public async Task Lookup_NetworkFailure_ReturnsNull()
    {
        _http.Throws(new HttpRequestException("down"));

        Assert.Null(await _source.LookupByIsbnAsync(Isbn));
    }

    [Fact]
    public async Task Lookup_MalformedJson_ReturnsNull()
    {
        _http.Json("{ not json");

        Assert.Null(await _source.LookupByIsbnAsync(Isbn));
    }

    [Fact]
    public async Task Search_ParsesDocsAndEncodesParameters()
    {
        _http.Json("""
        {
          "docs": [
            {
              "title": "The Hobbit",
              "author_name": ["J.R.R. Tolkien"],
              "isbn": ["not-an-isbn", "054792822X"],
              "publisher": ["HMH"],
              "first_publish_year": 1937,
              "cover_i": 42,
              "number_of_pages_median": 310,
              "subject": ["Fantasy"]
            },
            { "author_name": ["No Title"] },
            { "title": "Bare" }
          ]
        }
        """);

        var results = await _source.SearchAsync("the hobbit", "J R R Tolkien", 5);

        Assert.Equal(2, results.Count);
        var hobbit = results[0];
        Assert.Equal("J.R.R. Tolkien", hobbit.Author);
        Assert.Equal(Isbn, hobbit.Isbn);
        Assert.Equal("HMH", hobbit.Publisher);
        Assert.Equal(1937, hobbit.PublishedYear);
        Assert.Equal("https://covers.openlibrary.org/b/id/42-L.jpg", hobbit.CoverImageUrl);
        Assert.Equal(310, hobbit.PageCount);
        Assert.Equal("Fantasy", hobbit.Genre);

        var bare = results[1];
        Assert.Equal("Bare", bare.Title);
        Assert.Null(bare.Author);
        Assert.Null(bare.Isbn);
        Assert.Null(bare.CoverImageUrl);

        var query = _http.Requests[0].RequestUri!.Query;
        Assert.Contains("q=the%20hobbit", query);
        Assert.Contains("author=J%20R%20R%20Tolkien", query);
        Assert.Contains("limit=5", query);
    }

    [Fact]
    public async Task Search_AuthorOnly_OmitsQueryParameter()
    {
        _http.Json("""{"docs": []}""");

        await _source.SearchAsync(null, "Tolkien", 5);

        Assert.DoesNotContain("q=", _http.Requests[0].RequestUri!.Query);
    }

    [Theory]
    [InlineData("""{"numFound": 0}""")]
    [InlineData("""{"docs": "oops"}""")]
    [InlineData("[]")]
    public async Task Search_UnexpectedShape_ReturnsEmpty(string json)
    {
        _http.Json(json);

        Assert.Empty(await _source.SearchAsync("hobbit", null, 5));
    }

    [Fact]
    public async Task Search_HttpError_ReturnsEmpty()
    {
        _http.Status(HttpStatusCode.TooManyRequests);

        Assert.Empty(await _source.SearchAsync("hobbit", null, 5));
    }

    [Fact]
    public void Name_IsOpenLibrary()
    {
        Assert.Equal("Open Library", _source.Name);
    }
}
