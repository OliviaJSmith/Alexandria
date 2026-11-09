using Alexandria.Api.Dtos;
using Alexandria.Api.Services;
using Alexandria.Api.Services.Scanning;
using Moq;

namespace Alexandria.Api.Tests.Services.Scanning;

public class BookScanServiceTests
{
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10];
    private const string HobbitIsbn = "9780547928227";

    private readonly Mock<ITextRecognizer> _recognizer = new();
    private readonly Mock<IBookTextInterpreter> _interpreter = new();
    private readonly Mock<IBookService> _books = new();
    private readonly BookScanService _service;
    private readonly BookDto _hobbit = BookDto.FromBook(TestData.Book());

    public BookScanServiceTests()
    {
        _recognizer.SetupGet(r => r.IsConfigured).Returns(true);
        _interpreter.SetupGet(i => i.IsConfigured).Returns(true);
        _books.Setup(b => b.SearchAsync(It.IsAny<BookSearchQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<IReadOnlyList<BookDto>>.Success([]));
        _books.Setup(b => b.LookupByIsbnAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<BookDto>.Failure(ServiceErrorType.NotFound, "nope"));
        _service = new BookScanService(_recognizer.Object, _interpreter.Object, _books.Object);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task Scan_NotConfigured_ReturnsUnavailable(bool recognizerConfigured, bool interpreterConfigured)
    {
        _recognizer.SetupGet(r => r.IsConfigured).Returns(recognizerConfigured);
        _interpreter.SetupGet(i => i.IsConfigured).Returns(interpreterConfigured);

        var result = await _service.ScanAsync(Jpeg, ScanMode.Single);

        Assert.Equal(ServiceErrorType.Unavailable, result.Error!.Type);
        Assert.Equal(BookScanService.NotConfiguredMessage, result.Error.Message);
    }

    [Fact]
    public async Task Scan_NotAnImage_ReturnsValidationErrorWithoutCallingAzure()
    {
        var result = await _service.ScanAsync("hello"u8.ToArray(), ScanMode.Single);

        Assert.Equal(ServiceErrorType.Validation, result.Error!.Type);
        _recognizer.Verify(r => r.ReadTextAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Scan_TooLarge_ReturnsValidationError()
    {
        var huge = new byte[BookScanService.MaxImageBytes + 1];
        Jpeg.CopyTo(huge, 0);

        var result = await _service.ScanAsync(huge, ScanMode.Single);

        Assert.Equal(ServiceErrorType.Validation, result.Error!.Type);
    }

    [Fact]
    public async Task Scan_RecognitionFails_ReturnsUnavailable()
    {
        _recognizer.Setup(r => r.ReadTextAsync(Jpeg, It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);

        var result = await _service.ScanAsync(Jpeg, ScanMode.Single);

        Assert.Equal(BookScanService.RecognitionFailedMessage, result.Error!.Message);
    }

    [Fact]
    public async Task Scan_NoText_ReturnsEmptyResultWithMessage()
    {
        _recognizer.Setup(r => r.ReadTextAsync(Jpeg, It.IsAny<CancellationToken>())).ReturnsAsync("  ");

        var result = await _service.ScanAsync(Jpeg, ScanMode.Single);

        Assert.Empty(result.Value!.Books);
        Assert.Equal(BookScanService.NoTextMessage, result.Value.Message);
        _interpreter.Verify(i => i.InterpretAsync(It.IsAny<string>(), It.IsAny<ScanMode>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Scan_NoBooksRecognized_ReturnsEmptyResultWithMessage()
    {
        SetupText("Just some words");

        var result = await _service.ScanAsync(Jpeg, ScanMode.Shelf);

        Assert.Equal(BookScanService.NoBooksMessage, result.Value!.Message);
    }

    [Fact]
    public async Task Scan_Single_PrefersIsbnReadFromTextAndLooksItUp()
    {
        SetupText($"THE HOBBIT\nTolkien\nISBN 978-0-547-92822-7", new BookCandidate("The Hobbit", "J.R.R. Tolkien", "not-an-isbn"), new BookCandidate("Extra", null, null));
        _books.Setup(b => b.LookupByIsbnAsync(HobbitIsbn, It.IsAny<CancellationToken>())).ReturnsAsync(ServiceResult<BookDto>.Success(_hobbit));

        var result = await _service.ScanAsync(Jpeg, ScanMode.Single);

        var scanned = Assert.Single(result.Value!.Books);
        Assert.Equal("The Hobbit", scanned.DetectedTitle);
        Assert.Equal(HobbitIsbn, scanned.DetectedIsbn);
        Assert.Same(_hobbit, scanned.Match);
        _books.Verify(b => b.SearchAsync(It.IsAny<BookSearchQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Scan_Single_IsbnOnlyWithNoTitle_StillLooksItUp()
    {
        SetupText("9780547928227");
        _books.Setup(b => b.LookupByIsbnAsync(HobbitIsbn, It.IsAny<CancellationToken>())).ReturnsAsync(ServiceResult<BookDto>.Success(_hobbit));

        var result = await _service.ScanAsync(Jpeg, ScanMode.Single);

        Assert.Same(_hobbit, Assert.Single(result.Value!.Books).Match);
    }

    [Fact]
    public async Task Scan_Single_NoIsbnInText_KeepsOnlyFirstCandidate()
    {
        SetupText("DUNE\nHERBERT", new BookCandidate("Dune", "Frank Herbert", null), new BookCandidate("Other", null, null));

        var result = await _service.ScanAsync(Jpeg, ScanMode.Single);

        Assert.Equal("Dune", Assert.Single(result.Value!.Books).DetectedTitle);
    }

    [Fact]
    public async Task Scan_Shelf_MatchesEachBookBySearch_UnmatchedHaveNullMatch()
    {
        SetupText("spines", new BookCandidate("The Hobbit", "Tolkien", null), new BookCandidate("Unknown Zine", null, null));
        _books.Setup(b => b.SearchAsync(It.Is<BookSearchQuery>(q => q.Query == "The Hobbit" && q.Author == "Tolkien" && q.PageSize == 1), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<IReadOnlyList<BookDto>>.Success([_hobbit]));

        var result = await _service.ScanAsync(Jpeg, ScanMode.Shelf);

        Assert.Equal(2, result.Value!.Books.Count);
        Assert.Same(_hobbit, result.Value.Books[0].Match);
        Assert.Null(result.Value.Books[1].Match);
        Assert.Null(result.Value.Message);
    }

    [Fact]
    public async Task Scan_IsbnLookupMisses_FallsBackToSearch()
    {
        SetupText("Hobbit 9780547928227", new BookCandidate("The Hobbit", null, HobbitIsbn));
        _books.Setup(b => b.SearchAsync(It.Is<BookSearchQuery>(q => q.Query == "The Hobbit"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<IReadOnlyList<BookDto>>.Success([_hobbit]));

        var result = await _service.ScanAsync(Jpeg, ScanMode.Shelf);

        Assert.Same(_hobbit, Assert.Single(result.Value!.Books).Match);
    }

    [Fact]
    public async Task Scan_SearchFails_LeavesMatchEmpty()
    {
        SetupText("words", new BookCandidate("Book", null, null));
        _books.Setup(b => b.SearchAsync(It.IsAny<BookSearchQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<IReadOnlyList<BookDto>>.Failure(ServiceErrorType.Validation, "bad"));

        var result = await _service.ScanAsync(Jpeg, ScanMode.Shelf);

        Assert.Null(Assert.Single(result.Value!.Books).Match);
    }

    private void SetupText(string text, params BookCandidate[] candidates)
    {
        _recognizer.Setup(r => r.ReadTextAsync(Jpeg, It.IsAny<CancellationToken>())).ReturnsAsync(text);
        _interpreter.Setup(i => i.InterpretAsync(text, It.IsAny<ScanMode>(), It.IsAny<CancellationToken>())).ReturnsAsync(candidates);
    }
}
