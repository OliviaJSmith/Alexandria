using Alexandria.API.DTOs;
using Alexandria.API.Models;
using Alexandria.API.Services;

namespace Alexandria.API.Tests.Services;

public class LibraryServiceTests : ServiceTestBase
{
    private readonly LibraryService _sut;

    public LibraryServiceTests()
    {
        var logger = CreateMockLogger<LibraryService>();
        _sut = new LibraryService(Context, logger.Object);
    }

    [Fact]
    public async Task GetLibrariesAsync_ReturnsUserLibraries()
    {
        // Arrange
        var user = await SeedUserAsync();
        await SeedLibraryAsync(user.Id);
        await SeedLibraryAsync(user.Id, isPublic: true);

        // Act
        var result = await _sut.GetLibrariesAsync(user.Id, null);

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetLibrariesAsync_WithPublicFilter_ReturnsPublicLibraries()
    {
        // Arrange
        var user = await SeedUserAsync();
        await SeedLibraryAsync(user.Id, isPublic: false);
        await SeedLibraryAsync(user.Id, isPublic: true);

        // Act
        var result = await _sut.GetLibrariesAsync(user.Id, isPublic: true);

        // Assert
        var libraries = result.ToList();
        Assert.Single(libraries);
        Assert.True(libraries[0].IsPublic);
    }

    [Fact]
    public async Task GetLibrariesAsync_WithPrivateFilter_ReturnsPrivateLibraries()
    {
        // Arrange
        var user = await SeedUserAsync();
        await SeedLibraryAsync(user.Id, isPublic: false);
        await SeedLibraryAsync(user.Id, isPublic: true);

        // Act
        var result = await _sut.GetLibrariesAsync(user.Id, isPublic: false);

        // Assert
        var libraries = result.ToList();
        Assert.Single(libraries);
        Assert.False(libraries[0].IsPublic);
    }

    [Fact]
    public async Task GetLibraryByIdAsync_WithValidIdAndAccess_ReturnsLibrary()
    {
        // Arrange
        var user = await SeedUserAsync();
        var library = await SeedLibraryAsync(user.Id);

        // Act
        var result = await _sut.GetLibraryByIdAsync(library.Id, user.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(library.Id, result.Id);
    }

    [Fact]
    public async Task GetLibraryByIdAsync_WithPublicLibrary_ReturnsLibraryForAnyUser()
    {
        // Arrange
        var owner = await SeedUserAsync(1);
        var otherUser = await SeedUserAsync(2);
        var library = await SeedLibraryAsync(owner.Id, isPublic: true);

        // Act
        var result = await _sut.GetLibraryByIdAsync(library.Id, otherUser.Id);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetLibraryByIdAsync_WithPrivateLibraryAndNoAccess_ReturnsNull()
    {
        // Arrange
        var owner = await SeedUserAsync(1);
        var otherUser = await SeedUserAsync(2);
        var library = await SeedLibraryAsync(owner.Id, isPublic: false);

        // Act
        var result = await _sut.GetLibraryByIdAsync(library.Id, otherUser.Id);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetLibraryByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var user = await SeedUserAsync();

        // Act
        var result = await _sut.GetLibraryByIdAsync(999, user.Id);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateLibraryAsync_CreatesAndReturnsLibrary()
    {
        // Arrange
        var user = await SeedUserAsync();
        var request = new CreateLibraryRequest
        {
            Name = "My New Library",
            IsPublic = true
        };

        // Act
        var result = await _sut.CreateLibraryAsync(user.Id, request);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal("My New Library", result.Name);
        Assert.True(result.IsPublic);
        Assert.Equal(user.Id, result.UserId);
    }

    [Fact]
    public async Task GetLibraryBooksAsync_ReturnsBooks()
    {
        // Arrange
        var user = await SeedUserAsync();
        await SeedBooksAsync();
        var library = await SeedLibraryAsync(user.Id);
        await SeedLibraryBookAsync(library.Id, 1);
        await SeedLibraryBookAsync(library.Id, 2);

        // Act
        var result = await _sut.GetLibraryBooksAsync(library.Id, user.Id);

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetLibraryBooksAsync_WithNoAccess_ReturnsEmpty()
    {
        // Arrange
        var owner = await SeedUserAsync(1);
        var otherUser = await SeedUserAsync(2);
        await SeedBooksAsync();
        var library = await SeedLibraryAsync(owner.Id, isPublic: false);
        await SeedLibraryBookAsync(library.Id, 1);

        // Act
        var result = await _sut.GetLibraryBooksAsync(library.Id, otherUser.Id);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task AddBookToLibraryAsync_AddsBookSuccessfully()
    {
        // Arrange
        var user = await SeedUserAsync();
        await SeedBooksAsync();
        var library = await SeedLibraryAsync(user.Id);
        var request = new AddBookToLibraryRequest
        {
            BookId = 1,
            Status = BookStatus.Available
        };

        // Act
        var result = await _sut.AddBookToLibraryAsync(library.Id, user.Id, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(library.Id, result.LibraryId);
        Assert.Equal(BookStatus.Available, result.Status);
        Assert.Equal("The Great Gatsby", result.Book.Title);
    }

    [Fact]
    public async Task AddBookToLibraryAsync_WithNonOwner_ReturnsNull()
    {
        // Arrange
        var owner = await SeedUserAsync(1);
        var otherUser = await SeedUserAsync(2);
        await SeedBooksAsync();
        var library = await SeedLibraryAsync(owner.Id);
        var request = new AddBookToLibraryRequest { BookId = 1 };

        // Act
        var result = await _sut.AddBookToLibraryAsync(library.Id, otherUser.Id, request);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task AddBookToLibraryAsync_WithInvalidBook_ReturnsNull()
    {
        // Arrange
        var user = await SeedUserAsync();
        var library = await SeedLibraryAsync(user.Id);
        var request = new AddBookToLibraryRequest { BookId = 999 };

        // Act
        var result = await _sut.AddBookToLibraryAsync(library.Id, user.Id, request);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task RemoveBookFromLibraryAsync_RemovesSuccessfully()
    {
        // Arrange
        var user = await SeedUserAsync();
        await SeedBooksAsync();
        var library = await SeedLibraryAsync(user.Id);
        var libraryBook = await SeedLibraryBookAsync(library.Id, 1);

        // Act
        var result = await _sut.RemoveBookFromLibraryAsync(library.Id, libraryBook.Id, user.Id);

        // Assert
        Assert.True(result);
        Assert.Null(await Context.LibraryBooks.FindAsync(libraryBook.Id));
    }

    [Fact]
    public async Task RemoveBookFromLibraryAsync_WithNonOwner_ReturnsFalse()
    {
        // Arrange
        var owner = await SeedUserAsync(1);
        var otherUser = await SeedUserAsync(2);
        await SeedBooksAsync();
        var library = await SeedLibraryAsync(owner.Id);
        var libraryBook = await SeedLibraryBookAsync(library.Id, 1);

        // Act
        var result = await _sut.RemoveBookFromLibraryAsync(library.Id, libraryBook.Id, otherUser.Id);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task UserHasAccessToLibraryAsync_ReturnsTrueForOwner()
    {
        // Arrange
        var user = await SeedUserAsync();
        var library = await SeedLibraryAsync(user.Id, isPublic: false);

        // Act
        var result = await _sut.UserHasAccessToLibraryAsync(library.Id, user.Id);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task UserHasAccessToLibraryAsync_ReturnsTrueForPublicLibrary()
    {
        // Arrange
        var owner = await SeedUserAsync(1);
        var otherUser = await SeedUserAsync(2);
        var library = await SeedLibraryAsync(owner.Id, isPublic: true);

        // Act
        var result = await _sut.UserHasAccessToLibraryAsync(library.Id, otherUser.Id);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task UserOwnsLibraryAsync_ReturnsTrueForOwner()
    {
        // Arrange
        var user = await SeedUserAsync();
        var library = await SeedLibraryAsync(user.Id);

        // Act
        var result = await _sut.UserOwnsLibraryAsync(library.Id, user.Id);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task UserOwnsLibraryAsync_ReturnsFalseForNonOwner()
    {
        // Arrange
        var owner = await SeedUserAsync(1);
        var otherUser = await SeedUserAsync(2);
        var library = await SeedLibraryAsync(owner.Id);

        // Act
        var result = await _sut.UserOwnsLibraryAsync(library.Id, otherUser.Id);

        // Assert
        Assert.False(result);
    }
}
