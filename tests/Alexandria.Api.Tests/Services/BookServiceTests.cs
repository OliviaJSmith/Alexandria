using Alexandria.Api.Dtos;
using Alexandria.Api.Models;
using Alexandria.Api.Repositories;
using Alexandria.Api.Services;
using Alexandria.Api.Services.BookCatalog;
using Moq;

namespace Alexandria.Api.Tests.Services;

public class BookServiceTests
{
    private const string HobbitIsbn = "9780547928227";

    private readonly Mock<IBookRepository> _books = new();
    private readonly Mock<IBookLookupService> _lookup = new();
    private readonly BookService _service;

    private readonly BookPreview _preview = new("The Hobbit", "J.R.R. Tolkien", HobbitIsbn, "HMH", 2012, "There and back again.", "https://covers.example/h.jpg", "Fantasy", 300);

    public BookServiceTests()
    {
        _books.Setup(b => b.SearchAsync(It.IsAny<BookSearchCriteria>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _service = new BookService(_books.Object, _lookup.Object, TestData.TimeProviderAt(TestData.Now).Object);
    }

    // ---------- Search ----------

    [Fact]
    public async Task Search_NoCriteria_ReturnsValidationError()
    {
        var result = await _service.SearchAsync(new BookSearchQuery { Query = "  " });

        Assert.Equal(ServiceErrorType.Validation, result.Error!.Type);
        Assert.Equal(BookService.SearchCriteriaRequiredMessage, result.Error.Message);
    }

    [Fact]
    public async Task Search_InvalidIsbn_ReturnsValidationError()
    {
        var result = await _service.SearchAsync(new BookSearchQuery { Isbn = "123" });

        Assert.Equal(BookService.InvalidIsbnMessage, result.Error!.Message);
    }

    [Fact]
    public async Task Search_LocalMatches_ReturnsThemWithoutCallingExternalCatalogs()
    {
        var book = TestData.Book();
        BookSearchCriteria? criteria = null;
        _books.Setup(b => b.SearchAsync(It.IsAny<BookSearchCriteria>(), 20, 10, It.IsAny<CancellationToken>()))
            .Callback<BookSearchCriteria, int, int, CancellationToken>((c, _, _, _) => criteria = c)
            .ReturnsAsync([book]);

        var result = await _service.SearchAsync(new BookSearchQuery
        {
            Query = " hobbit ",
            Author = "Tolkien",
            Genre = " ",
            Isbn = "978-0-547-92822-7",
            PublishedYear = 2012,
            Page = 3,
            PageSize = 10,
        });

        Assert.Equal(book.Id, Assert.Single(result.Value!).Id);
        Assert.Equal(new BookSearchCriteria("hobbit", "Tolkien", null, HobbitIsbn, 2012), criteria);
        _lookup.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Search_NoLocalMatchesOnLaterPage_DoesNotSearchExternally()
    {
        var result = await _service.SearchAsync(new BookSearchQuery { Query = "hobbit", Page = 2 });

        Assert.Empty(result.Value!);
        _lookup.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Search_NoLocalMatchesByIsbn_LooksUpExternally()
    {
        _lookup.Setup(l => l.LookupByIsbnAsync(HobbitIsbn, It.IsAny<CancellationToken>())).ReturnsAsync(_preview);

        var result = await _service.SearchAsync(new BookSearchQuery { Isbn = HobbitIsbn });

        var dto = Assert.Single(result.Value!);
        Assert.Null(dto.Id);
        Assert.Equal("The Hobbit", dto.Title);
        Assert.Equal("There and back again.", dto.Description);
    }

    [Fact]
    public async Task Search_NoLocalOrExternalMatchByIsbn_ReturnsEmpty()
    {
        var result = await _service.SearchAsync(new BookSearchQuery { Isbn = HobbitIsbn });

        Assert.Empty(result.Value!);
    }

    [Fact]
    public async Task Search_NoLocalMatchesByText_SearchesExternally()
    {
        _lookup.Setup(l => l.SearchAsync("hobbit", "Tolkien", 15, It.IsAny<CancellationToken>())).ReturnsAsync([_preview]);

        var result = await _service.SearchAsync(new BookSearchQuery { Query = "hobbit", Author = "Tolkien", PageSize = 15 });

        Assert.Equal("The Hobbit", Assert.Single(result.Value!).Title);
    }

    [Fact]
    public async Task Search_NoLocalMatchesByGenreOnly_DoesNotSearchExternally()
    {
        var result = await _service.SearchAsync(new BookSearchQuery { Genre = "Fantasy", PublishedYear = 1937 });

        Assert.Empty(result.Value!);
        _lookup.VerifyNoOtherCalls();
    }

    // ---------- Get ----------

    [Fact]
    public async Task GetById_Existing_ReturnsBook()
    {
        var book = TestData.Book();
        _books.Setup(b => b.GetByIdAsync(book.Id, It.IsAny<CancellationToken>())).ReturnsAsync(book);

        var result = await _service.GetByIdAsync(book.Id);

        Assert.Equal(book.Id, result.Value!.Id);
    }

    [Fact]
    public async Task GetById_Missing_ReturnsNotFound()
    {
        var result = await _service.GetByIdAsync(Guid.NewGuid());

        Assert.Equal(ServiceErrorType.NotFound, result.Error!.Type);
    }

    // ---------- Create ----------

    [Fact]
    public async Task Create_NewBook_NormalizesAndSaves()
    {
        Book? added = null;
        _books.Setup(b => b.Add(It.IsAny<Book>())).Callback<Book>(b => added = b);

        var result = await _service.CreateAsync(new CreateBookRequest
        {
            Title = "  The Hobbit ",
            Author = " J.R.R. Tolkien ",
            Isbn = "054792822X",
            Publisher = " ",
            PublishedYear = 2012,
            CoverImageUrl = "https://covers.example/h.jpg",
            PageCount = 300,
        });

        Assert.True(result.Value!.Created);
        Assert.NotNull(added);
        Assert.Equal("The Hobbit", added.Title);
        Assert.Equal("J.R.R. Tolkien", added.Author);
        Assert.Equal(HobbitIsbn, added.Isbn);
        Assert.Null(added.Publisher);
        Assert.Equal(TestData.Now, added.CreatedAt);
        Assert.Equal(added.Id, result.Value.Book.Id);
        _books.Verify(b => b.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_WithoutIsbn_CreatesBook()
    {
        var result = await _service.CreateAsync(new CreateBookRequest { Title = "Family Recipes" });

        Assert.True(result.Value!.Created);
        Assert.Null(result.Value.Book.Isbn);
        _books.Verify(b => b.GetByIsbnAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_KnownIsbn_ReturnsExistingWithoutSaving()
    {
        var existing = TestData.Book();
        _books.Setup(b => b.GetByIsbnAsync(HobbitIsbn, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var result = await _service.CreateAsync(new CreateBookRequest { Title = "The Hobbit", Isbn = "978-0-547-92822-7" });

        Assert.False(result.Value!.Created);
        Assert.Equal(existing.Id, result.Value.Book.Id);
        _books.Verify(b => b.Add(It.IsAny<Book>()), Times.Never);
    }

    [Theory]
    [InlineData(" ", null, null, BookService.TitleRequiredMessage)]
    [InlineData("Title", "123", null, BookService.InvalidIsbnMessage)]
    [InlineData("Title", null, "javascript:alert(1)", BookService.InvalidCoverUrlMessage)]
    [InlineData("Title", null, "/relative/cover.jpg", BookService.InvalidCoverUrlMessage)]
    public async Task Create_InvalidInput_ReturnsValidationError(string title, string? isbn, string? coverUrl, string expectedMessage)
    {
        var result = await _service.CreateAsync(new CreateBookRequest { Title = title, Isbn = isbn, CoverImageUrl = coverUrl });

        Assert.Equal(ServiceErrorType.Validation, result.Error!.Type);
        Assert.Equal(expectedMessage, result.Error.Message);
        _books.Verify(b => b.Add(It.IsAny<Book>()), Times.Never);
    }

    // ---------- Lookup ----------

    [Fact]
    public async Task Lookup_KnownLocally_ReturnsCatalogEntry()
    {
        var existing = TestData.Book();
        _books.Setup(b => b.GetByIsbnAsync(HobbitIsbn, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var result = await _service.LookupByIsbnAsync("054792822X");

        Assert.Equal(existing.Id, result.Value!.Id);
        _lookup.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Lookup_KnownExternally_ReturnsPreviewWithoutId()
    {
        _lookup.Setup(l => l.LookupByIsbnAsync(HobbitIsbn, It.IsAny<CancellationToken>())).ReturnsAsync(_preview);

        var result = await _service.LookupByIsbnAsync(HobbitIsbn);

        Assert.Null(result.Value!.Id);
        Assert.Equal("HMH", result.Value.Publisher);
    }

    [Fact]
    public async Task Lookup_Unknown_ReturnsNotFound()
    {
        var result = await _service.LookupByIsbnAsync(HobbitIsbn);

        Assert.Equal(ServiceErrorType.NotFound, result.Error!.Type);
    }

    [Fact]
    public async Task Lookup_InvalidIsbn_ReturnsValidationError()
    {
        var result = await _service.LookupByIsbnAsync("not-an-isbn");

        Assert.Equal(ServiceErrorType.Validation, result.Error!.Type);
    }
}
