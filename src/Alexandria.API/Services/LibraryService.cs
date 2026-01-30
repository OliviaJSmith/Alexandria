using Alexandria.API.Data;
using Alexandria.API.DTOs;
using Alexandria.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Alexandria.API.Services;

public class LibraryService(AlexandriaDbContext context, ILogger<LibraryService> logger) : ILibraryService
{
    public async Task<IEnumerable<LibraryDto>> GetLibrariesAsync(int userId, bool? isPublic)
    {
        var query = context.Libraries.AsQueryable();

        if (isPublic.HasValue)
        {
            query = isPublic.Value
                ? query.Where(l => l.IsPublic)
                : query.Where(l => !l.IsPublic && l.UserId == userId);
        }
        else
        {
            query = query.Where(l => l.UserId == userId);
        }

        var libraries = await query.ToListAsync();
        return libraries.Select(MapToDto);
    }

    public async Task<LibraryDto?> GetLibraryByIdAsync(int id, int userId)
    {
        var library = await context.Libraries.FindAsync(id);

        if (library is null)
            return null;

        if (!library.IsPublic && library.UserId != userId)
            return null;

        return MapToDto(library);
    }

    public async Task<LibraryDto> CreateLibraryAsync(int userId, CreateLibraryRequest request)
    {
        var library = new Library
        {
            Name = request.Name,
            IsPublic = request.IsPublic,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Libraries.Add(library);
        await context.SaveChangesAsync();

        return MapToDto(library);
    }

    public async Task<IEnumerable<LibraryBookDto>> GetLibraryBooksAsync(int libraryId, int userId)
    {
        if (!await UserHasAccessToLibraryAsync(libraryId, userId))
            return [];

        var libraryBooks = await context.LibraryBooks
            .Include(lb => lb.Book)
            .Where(lb => lb.LibraryId == libraryId)
            .ToListAsync();

        return libraryBooks.Select(MapToLibraryBookDto);
    }

    public async Task<IEnumerable<LibraryBookDto>> GetLentOutBooksAsync(int userId)
    {
        // Get all books with CheckedOut status from user's libraries
        var lentBooks = await context.LibraryBooks
            .Include(lb => lb.Book)
            .Include(lb => lb.Library)
            .Where(lb => lb.Library.UserId == userId && lb.Status == BookStatus.CheckedOut)
            .ToListAsync();

        return lentBooks.Select(MapToLibraryBookDto);
    }

    public async Task<LibraryBookDto?> AddBookToLibraryAsync(int libraryId, int userId, AddBookToLibraryRequest request)
    {
        var library = await context.Libraries.FindAsync(libraryId);

        if (library is null || library.UserId != userId)
            return null;

        var book = await context.Books.FindAsync(request.BookId);
        if (book is null)
            return null;

        var libraryBook = new LibraryBook
        {
            LibraryId = libraryId,
            BookId = request.BookId,
            Status = request.Status,
            AddedAt = DateTime.UtcNow
        };

        context.LibraryBooks.Add(libraryBook);
        await context.SaveChangesAsync();

        libraryBook.Book = book;
        return MapToLibraryBookDto(libraryBook);
    }

    public async Task<bool> RemoveBookFromLibraryAsync(int libraryId, int libraryBookId, int userId)
    {
        var library = await context.Libraries.FindAsync(libraryId);

        if (library is null || library.UserId != userId)
            return false;

        var libraryBook = await context.LibraryBooks
            .FirstOrDefaultAsync(lb => lb.Id == libraryBookId && lb.LibraryId == libraryId);

        if (libraryBook is null)
            return false;

        context.LibraryBooks.Remove(libraryBook);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<LibraryBookDto?> UpdateLibraryBookAsync(int libraryId, int libraryBookId, int userId, UpdateLibraryBookRequest request)
    {
        var library = await context.Libraries.FindAsync(libraryId);

        if (library is null || library.UserId != userId)
            return null;

        var libraryBook = await context.LibraryBooks
            .Include(lb => lb.Book)
            .FirstOrDefaultAsync(lb => lb.Id == libraryBookId && lb.LibraryId == libraryId);

        if (libraryBook is null)
            return null;

        // Update status if provided
        if (request.Status.HasValue)
        {
            libraryBook.Status = request.Status.Value;
        }

        // Update loan note if provided
        if (request.LoanNote is not null)
        {
            libraryBook.LoanNote = string.IsNullOrWhiteSpace(request.LoanNote) ? null : request.LoanNote;
        }

        // Update genre on the book itself if provided
        if (request.Genre is not null)
        {
            libraryBook.Book.Genre = string.IsNullOrWhiteSpace(request.Genre) ? null : request.Genre;
        }

        await context.SaveChangesAsync();
        return MapToLibraryBookDto(libraryBook);
    }

    public async Task<LibraryBookDto?> MoveBookToLibraryAsync(int sourceLibraryId, int libraryBookId, int targetLibraryId, int userId)
    {
        // Verify user owns both libraries
        var sourceLibrary = await context.Libraries.FindAsync(sourceLibraryId);
        var targetLibrary = await context.Libraries.FindAsync(targetLibraryId);

        if (sourceLibrary is null || sourceLibrary.UserId != userId)
            return null;

        if (targetLibrary is null || targetLibrary.UserId != userId)
            return null;

        var libraryBook = await context.LibraryBooks
            .Include(lb => lb.Book)
            .FirstOrDefaultAsync(lb => lb.Id == libraryBookId && lb.LibraryId == sourceLibraryId);

        if (libraryBook is null)
            return null;

        // Move to target library
        libraryBook.LibraryId = targetLibraryId;
        await context.SaveChangesAsync();

        return MapToLibraryBookDto(libraryBook);
    }

    public async Task<bool> UserHasAccessToLibraryAsync(int libraryId, int userId)
    {
        var library = await context.Libraries.FindAsync(libraryId);
        return library is not null && (library.IsPublic || library.UserId == userId);
    }

    public async Task<bool> UserOwnsLibraryAsync(int libraryId, int userId)
    {
        var library = await context.Libraries.FindAsync(libraryId);
        return library is not null && library.UserId == userId;
    }

    private static LibraryDto MapToDto(Library library) => new()
    {
        Id = library.Id,
        Name = library.Name,
        IsPublic = library.IsPublic,
        UserId = library.UserId,
        CreatedAt = library.CreatedAt
    };

    private static LibraryBookDto MapToLibraryBookDto(LibraryBook lb) => new()
    {
        Id = lb.Id,
        LibraryId = lb.LibraryId,
        Status = lb.Status,
        LoanNote = lb.LoanNote,
        AddedAt = lb.AddedAt,
        Book = new BookDto
        {
            Id = lb.Book.Id,
            Title = lb.Book.Title,
            Author = lb.Book.Author,
            Isbn = lb.Book.Isbn,
            Publisher = lb.Book.Publisher,
            PublishedYear = lb.Book.PublishedYear,
            Description = lb.Book.Description,
            CoverImageUrl = lb.Book.CoverImageUrl,
            Genre = lb.Book.Genre,
            PageCount = lb.Book.PageCount
        }
    };
}
