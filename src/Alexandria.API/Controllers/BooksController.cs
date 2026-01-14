using Alexandria.API.Data;
using Alexandria.API.DTOs;
using Alexandria.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Alexandria.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BooksController : ControllerBase
{
    private readonly AlexandriaDbContext _context;
    private readonly ILogger<BooksController> _logger;

    public BooksController(AlexandriaDbContext context, ILogger<BooksController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BookDto>>> SearchBooks([FromQuery] BookSearchRequest request)
    {
        var query = _context.Books.AsQueryable();

        if (!string.IsNullOrEmpty(request.Query))
        {
            query = query.Where(b => 
                b.Title.Contains(request.Query) || 
                (b.Author != null && b.Author.Contains(request.Query)) ||
                (b.Description != null && b.Description.Contains(request.Query)));
        }

        if (!string.IsNullOrEmpty(request.Author))
        {
            query = query.Where(b => b.Author != null && b.Author.Contains(request.Author));
        }

        if (!string.IsNullOrEmpty(request.Genre))
        {
            query = query.Where(b => b.Genre != null && b.Genre.Contains(request.Genre));
        }

        if (!string.IsNullOrEmpty(request.Isbn))
        {
            query = query.Where(b => b.Isbn == request.Isbn);
        }

        if (request.PublishedYear.HasValue)
        {
            query = query.Where(b => b.PublishedYear == request.PublishedYear);
        }

        var books = await query.Take(50).ToListAsync();

        return Ok(books.Select(b => new BookDto
        {
            Id = b.Id,
            Title = b.Title,
            Author = b.Author,
            Isbn = b.Isbn,
            Publisher = b.Publisher,
            PublishedYear = b.PublishedYear,
            Description = b.Description,
            CoverImageUrl = b.CoverImageUrl,
            Genre = b.Genre,
            PageCount = b.PageCount
        }));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BookDto>> GetBook(int id)
    {
        var book = await _context.Books.FindAsync(id);
        if (book == null)
        {
            return NotFound();
        }

        return Ok(new BookDto
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
        });
    }

    [HttpPost]
    public async Task<ActionResult<BookDto>> CreateBook(CreateBookRequest request)
    {
        var book = new Book
        {
            Title = request.Title,
            Author = request.Author,
            Isbn = request.Isbn,
            Publisher = request.Publisher,
            PublishedYear = request.PublishedYear,
            Description = request.Description,
            CoverImageUrl = request.CoverImageUrl,
            Genre = request.Genre,
            PageCount = request.PageCount,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Books.Add(book);
        await _context.SaveChangesAsync();

        var bookDto = new BookDto
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
        };

        return CreatedAtAction(nameof(GetBook), new { id = book.Id }, bookDto);
    }

    [HttpPost("search-by-image")]
    public async Task<ActionResult<IEnumerable<BookDto>>> SearchBooksByImage([FromForm] IFormFile image)
    {
        // Placeholder for image-based book search
        // In a real implementation, this would integrate with an OCR service
        // to extract text from the image (like book cover or barcode)
        // and then search for matching books
        
        if (image == null || image.Length == 0)
        {
            return BadRequest("No image provided");
        }

        _logger.LogInformation($"Received image search request: {image.FileName}, {image.Length} bytes");
        
        // For now, return empty results
        // TODO: Implement OCR/image recognition service integration
        return Ok(new List<BookDto>());
    }
}
