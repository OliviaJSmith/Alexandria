using Alexandria.API.DTOs;
using Alexandria.API.Services;

namespace Alexandria.API.Tests.Services;

public class BookServiceTests : ServiceTestBase
{
    private readonly BookService _sut;

    public BookServiceTests()
    {
        var logger = CreateMockLogger<BookService>();
        _sut = new BookService(Context, logger.Object);
    }

    [Fact]
    public async Task SearchBooksAsync_WithEmptyRequest_ReturnsAllBooks()
    {
        // Arrange
        await SeedBooksAsync();
        var request = new BookSearchRequest();

        // Act
        var result = await _sut.SearchBooksAsync(request, 1, 50);

        // Assert
        Assert.Equal(3, result.Count());
    }

    [Fact]
    public async Task SearchBooksAsync_WithQueryFilter_ReturnsMatchingBooks()
    {
        // Arrange
        await SeedBooksAsync();
        var request = new BookSearchRequest { Query = "Gatsby" };

        // Act
        var result = await _sut.SearchBooksAsync(request, 1, 50);

        // Assert
        var books = result.ToList();
        Assert.Single(books);
        Assert.Equal("The Great Gatsby", books[0].Title);
    }

    [Fact]
    public async Task SearchBooksAsync_WithAuthorFilter_ReturnsMatchingBooks()
    {
        // Arrange
        await SeedBooksAsync();
        var request = new BookSearchRequest { Author = "Harper Lee" };

        // Act
        var result = await _sut.SearchBooksAsync(request, 1, 50);

        // Assert
        var books = result.ToList();
        Assert.Single(books);
        Assert.Equal("To Kill a Mockingbird", books[0].Title);
    }

    [Fact]
    public async Task SearchBooksAsync_WithGenreFilter_ReturnsMatchingBooks()
    {
        // Arrange
        await SeedBooksAsync();
        var request = new BookSearchRequest { Genre = "Technology" };

        // Act
        var result = await _sut.SearchBooksAsync(request, 1, 50);

        // Assert
        var books = result.ToList();
        Assert.Single(books);
        Assert.Equal("Clean Code", books[0].Title);
    }

    [Fact]
    public async Task SearchBooksAsync_WithIsbnFilter_ReturnsExactMatch()
    {
        // Arrange
        await SeedBooksAsync();
        var request = new BookSearchRequest { Isbn = "978-0743273565" };

        // Act
        var result = await _sut.SearchBooksAsync(request, 1, 50);

        // Assert
        var books = result.ToList();
        Assert.Single(books);
        Assert.Equal("The Great Gatsby", books[0].Title);
    }

    [Fact]
    public async Task SearchBooksAsync_WithPublishedYearFilter_ReturnsMatchingBooks()
    {
        // Arrange
        await SeedBooksAsync();
        var request = new BookSearchRequest { PublishedYear = 1960 };

        // Act
        var result = await _sut.SearchBooksAsync(request, 1, 50);

        // Assert
        var books = result.ToList();
        Assert.Single(books);
        Assert.Equal("To Kill a Mockingbird", books[0].Title);
    }

    [Fact]
    public async Task SearchBooksAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        await SeedBooksAsync();
        var request = new BookSearchRequest();

        // Act
        var result = await _sut.SearchBooksAsync(request, 2, 2);

        // Assert
        var books = result.ToList();
        Assert.Single(books);
    }

    [Fact]
    public async Task SearchBooksAsync_ClampsPageSizeToMax100()
    {
        // Arrange
        await SeedBooksAsync();
        var request = new BookSearchRequest();

        // Act - request 200 items, should be clamped to 100
        var result = await _sut.SearchBooksAsync(request, 1, 200);

        // Assert - should return all 3 since we only have 3 items
        Assert.Equal(3, result.Count());
    }

    [Fact]
    public async Task GetBookByIdAsync_WithValidId_ReturnsBook()
    {
        // Arrange
        await SeedBooksAsync();

        // Act
        var result = await _sut.GetBookByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("The Great Gatsby", result.Title);
        Assert.Equal("F. Scott Fitzgerald", result.Author);
    }

    [Fact]
    public async Task GetBookByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        await SeedBooksAsync();

        // Act
        var result = await _sut.GetBookByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateBookAsync_CreatesAndReturnsBook()
    {
        // Arrange
        var request = new CreateBookRequest
        {
            Title = "New Book",
            Author = "New Author",
            Isbn = "123-4567890123",
            Genre = "Science Fiction",
            PublishedYear = 2025
        };

        // Act
        var result = await _sut.CreateBookAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal("New Book", result.Title);
        Assert.Equal("New Author", result.Author);

        // Verify persisted
        var persisted = await Context.Books.FindAsync(result.Id);
        Assert.NotNull(persisted);
        Assert.Equal("New Book", persisted.Title);
    }

    [Fact]
    public async Task SearchBooksByImageAsync_ReturnsEmptyCollection()
    {
        // Arrange
        using var stream = new MemoryStream();

        // Act
        var result = await _sut.SearchBooksByImageAsync(stream, "test.jpg");

        // Assert
        Assert.Empty(result);
    }
}
