using Alexandria.API.Data;
using Alexandria.API.DTOs;
using Alexandria.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Alexandria.API.Services;

public class BookService(AlexandriaDbContext context, ILogger<BookService> logger) : IBookService
{
    public async Task<IEnumerable<BookDto>> SearchBooksAsync(BookSearchRequest request, int page, int pageSize)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(1, page);

        var query = context.Books.AsQueryable();

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

        var books = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return books.Select(MapToDto);
    }

    public async Task<BookDto?> GetBookByIdAsync(int id)
    {
        var book = await context.Books.FindAsync(id);
        return book is null ? null : MapToDto(book);
    }

    public async Task<BookDto> CreateBookAsync(CreateBookRequest request)
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

        context.Books.Add(book);
        await context.SaveChangesAsync();

        return MapToDto(book);
    }

    public async Task<IEnumerable<BookDto>> SearchBooksByImageAsync(Stream imageStream, string fileName)
    {
        // Placeholder for image-based book search
        // In a real implementation, this would integrate with an OCR service
        logger.LogInformation("Received image search request: {FileName}", fileName);

        await Task.CompletedTask;
        return [];
    }

    private static BookDto MapToDto(Book book) => new()
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
}
