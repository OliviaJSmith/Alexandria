using Alexandria.API.Data;
using Alexandria.API.DTOs;
using Alexandria.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Alexandria.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LibrariesController : BaseController
{
    private readonly AlexandriaDbContext _context;
    private readonly ILogger<LibrariesController> _logger;

    public LibrariesController(AlexandriaDbContext context, ILogger<LibrariesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LibraryDto>>> GetLibraries([FromQuery] bool? isPublic = null)
    {
        var userId = GetCurrentUserId();
        var query = _context.Libraries.AsQueryable();

        if (isPublic.HasValue)
        {
            if (isPublic.Value)
            {
                // Public libraries - show all public libraries
                query = query.Where(l => l.IsPublic);
            }
            else
            {
                // Private libraries - show only user's private libraries
                query = query.Where(l => !l.IsPublic && l.UserId == userId);
            }
        }
        else
        {
            // Show user's own libraries (both public and private)
            query = query.Where(l => l.UserId == userId);
        }

        var libraries = await query.ToListAsync();

        return Ok(libraries.Select(l => new LibraryDto
        {
            Id = l.Id,
            Name = l.Name,
            IsPublic = l.IsPublic,
            UserId = l.UserId,
            CreatedAt = l.CreatedAt
        }));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<LibraryDto>> GetLibrary(int id)
    {
        var userId = GetCurrentUserId();
        var library = await _context.Libraries.FindAsync(id);

        if (library == null)
        {
            return NotFound();
        }

        // Check if user has access to this library
        if (!library.IsPublic && library.UserId != userId)
        {
            return Forbid();
        }

        return Ok(new LibraryDto
        {
            Id = library.Id,
            Name = library.Name,
            IsPublic = library.IsPublic,
            UserId = library.UserId,
            CreatedAt = library.CreatedAt
        });
    }

    [HttpPost]
    public async Task<ActionResult<LibraryDto>> CreateLibrary(CreateLibraryRequest request)
    {
        var userId = GetCurrentUserId();

        var library = new Library
        {
            Name = request.Name,
            IsPublic = request.IsPublic,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Libraries.Add(library);
        await _context.SaveChangesAsync();

        var libraryDto = new LibraryDto
        {
            Id = library.Id,
            Name = library.Name,
            IsPublic = library.IsPublic,
            UserId = library.UserId,
            CreatedAt = library.CreatedAt
        };

        return CreatedAtAction(nameof(GetLibrary), new { id = library.Id }, libraryDto);
    }

    [HttpGet("{id}/books")]
    public async Task<ActionResult<IEnumerable<LibraryBookDto>>> GetLibraryBooks(int id)
    {
        var userId = GetCurrentUserId();
        var library = await _context.Libraries.FindAsync(id);

        if (library == null)
        {
            return NotFound();
        }

        // Check if user has access to this library
        if (!library.IsPublic && library.UserId != userId)
        {
            return Forbid();
        }

        var libraryBooks = await _context.LibraryBooks
            .Include(lb => lb.Book)
            .Where(lb => lb.LibraryId == id)
            .ToListAsync();

        return Ok(libraryBooks.Select(lb => new LibraryBookDto
        {
            Id = lb.Id,
            LibraryId = lb.LibraryId,
            Status = lb.Status,
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
        }));
    }

    [HttpPost("{id}/books")]
    public async Task<ActionResult<LibraryBookDto>> AddBookToLibrary(int id, AddBookToLibraryRequest request)
    {
        var userId = GetCurrentUserId();
        var library = await _context.Libraries.FindAsync(id);

        if (library == null)
        {
            return NotFound("Library not found");
        }

        // Check if user owns this library
        if (library.UserId != userId)
        {
            return Forbid();
        }

        var book = await _context.Books.FindAsync(request.BookId);
        if (book == null)
        {
            return NotFound("Book not found");
        }

        var libraryBook = new LibraryBook
        {
            LibraryId = id,
            BookId = request.BookId,
            Status = request.Status,
            AddedAt = DateTime.UtcNow
        };

        _context.LibraryBooks.Add(libraryBook);
        await _context.SaveChangesAsync();

        return Ok(new LibraryBookDto
        {
            Id = libraryBook.Id,
            LibraryId = libraryBook.LibraryId,
            Status = libraryBook.Status,
            AddedAt = libraryBook.AddedAt,
            Book = new BookDto
            {
                Id = book.Id,
                Title = book.Title,
                Author = book.Author,
                Isbn = book.Isbn,
                Publisher = book.Publisher,
                PublishedYear = book.PublishedYear,
                Description = book.Description,
                CoverImageUrl = book.CoverImageUrl,
                Genre = book.Genre,
                PageCount = book.PageCount
            }
        });
    }

    [HttpDelete("{libraryId}/books/{libraryBookId}")]
    public async Task<ActionResult> RemoveBookFromLibrary(int libraryId, int libraryBookId)
    {
        var userId = GetCurrentUserId();
        var library = await _context.Libraries.FindAsync(libraryId);

        if (library == null)
        {
            return NotFound("Library not found");
        }

        // Check if user owns this library
        if (library.UserId != userId)
        {
            return Forbid();
        }

        var libraryBook = await _context.LibraryBooks
            .FirstOrDefaultAsync(lb => lb.Id == libraryBookId && lb.LibraryId == libraryId);

        if (libraryBook == null)
        {
            return NotFound("Book not found in library");
        }

        _context.LibraryBooks.Remove(libraryBook);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
