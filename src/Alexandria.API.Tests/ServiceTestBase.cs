using Alexandria.API.Data;
using Alexandria.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Alexandria.API.Tests;

public abstract class ServiceTestBase : IDisposable
{
    protected readonly AlexandriaDbContext Context;

    protected ServiceTestBase()
    {
        var options = new DbContextOptionsBuilder<AlexandriaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        Context = new AlexandriaDbContext(options);
    }

    protected static Mock<ILogger<T>> CreateMockLogger<T>() => new();

    protected async Task SeedBooksAsync()
    {
        var books = new List<Book>
        {
            new()
            {
                Id = 1,
                Title = "The Great Gatsby",
                Author = "F. Scott Fitzgerald",
                Isbn = "978-0743273565",
                Genre = "Fiction",
                PublishedYear = 1925,
                Description = "A story of the mysteriously wealthy Jay Gatsby",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = 2,
                Title = "To Kill a Mockingbird",
                Author = "Harper Lee",
                Isbn = "978-0061120084",
                Genre = "Fiction",
                PublishedYear = 1960,
                Description = "A classic of modern American literature",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = 3,
                Title = "Clean Code",
                Author = "Robert C. Martin",
                Isbn = "978-0132350884",
                Genre = "Technology",
                PublishedYear = 2008,
                Description = "A handbook of agile software craftsmanship",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        Context.Books.AddRange(books);
        await Context.SaveChangesAsync();
    }

    protected async Task<User> SeedUserAsync(int id = 1)
    {
        var user = new User
        {
            Id = id,
            GoogleId = $"google-{id}",
            Email = $"user{id}@test.com",
            Name = $"Test User {id}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        Context.Users.Add(user);
        await Context.SaveChangesAsync();
        return user;
    }

    protected async Task<Library> SeedLibraryAsync(int userId, bool isPublic = false)
    {
        var library = new Library
        {
            Name = "Test Library",
            IsPublic = isPublic,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        Context.Libraries.Add(library);
        await Context.SaveChangesAsync();
        return library;
    }

    protected async Task<LibraryBook> SeedLibraryBookAsync(int libraryId, int bookId, BookStatus status = BookStatus.Available)
    {
        var libraryBook = new LibraryBook
        {
            LibraryId = libraryId,
            BookId = bookId,
            Status = status,
            AddedAt = DateTime.UtcNow
        };

        Context.LibraryBooks.Add(libraryBook);
        await Context.SaveChangesAsync();
        return libraryBook;
    }

    public void Dispose()
    {
        Context.Dispose();
        GC.SuppressFinalize(this);
    }
}
