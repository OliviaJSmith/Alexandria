using Alexandria.Api.Services.BookCatalog;
using Moq;

namespace Alexandria.Api.Tests.Services.BookCatalog;

public class BookLookupServiceTests
{
    private readonly Mock<IBookCatalogSource> _primary = new();
    private readonly Mock<IBookCatalogSource> _fallback = new();
    private readonly BookLookupService _service;

    public BookLookupServiceTests()
    {
        _service = new BookLookupService([_primary.Object, _fallback.Object]);
    }

    [Fact]
    public async Task Lookup_PrimaryFindsIt_DoesNotAskFallback()
    {
        var preview = Preview("A", "9780547928227");
        _primary.Setup(s => s.LookupByIsbnAsync("9780547928227", It.IsAny<CancellationToken>())).ReturnsAsync(preview);

        var result = await _service.LookupByIsbnAsync("9780547928227");

        Assert.Same(preview, result);
        _fallback.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Lookup_PrimaryMisses_UsesFallback()
    {
        var preview = Preview("A", "9780547928227");
        _fallback.Setup(s => s.LookupByIsbnAsync("9780547928227", It.IsAny<CancellationToken>())).ReturnsAsync(preview);

        Assert.Same(preview, await _service.LookupByIsbnAsync("9780547928227"));
    }

    [Fact]
    public async Task Lookup_NoSourceFindsIt_ReturnsNull()
    {
        Assert.Null(await _service.LookupByIsbnAsync("9780547928227"));
    }

    [Fact]
    public async Task Search_FillsFromFallbackAndSkipsDuplicateIsbns()
    {
        _primary.Setup(s => s.SearchAsync("hobbit", null, 4, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Preview("A", "1"), Preview("B", null)]);
        _fallback.Setup(s => s.SearchAsync("hobbit", null, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Preview("A again", "1"), Preview("C", "3"), Preview("D", "4")]);

        var results = await _service.SearchAsync("hobbit", null, 4);

        Assert.Equal(["A", "B", "C", "D"], results.Select(r => r.Title));
    }

    [Fact]
    public async Task Search_PrimaryFillsLimit_DoesNotAskFallback()
    {
        _primary.Setup(s => s.SearchAsync("hobbit", "Tolkien", 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Preview("A", "1"), Preview("B", "2"), Preview("C", "3")]);

        var results = await _service.SearchAsync("hobbit", "Tolkien", 2);

        Assert.Equal(["A", "B"], results.Select(r => r.Title));
        _fallback.VerifyNoOtherCalls();
    }

    private static BookPreview Preview(string title, string? isbn) =>
        new(title, null, isbn, null, null, null, null, null, null);
}
