using Alexandria.API.DTOs;
using Alexandria.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alexandria.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BooksController(IBookService bookService, ILogger<BooksController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<BookDto>>> SearchBooks(
        [FromQuery] BookSearchRequest request,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var books = await bookService.SearchBooksAsync(request, page, pageSize);
        return Ok(books);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BookDto>> GetBook(int id)
    {
        var book = await bookService.GetBookByIdAsync(id);
        return book is null ? NotFound() : Ok(book);
    }

    [HttpPost]
    public async Task<ActionResult<BookDto>> CreateBook(CreateBookRequest request)
    {
        var book = await bookService.CreateBookAsync(request);
        return CreatedAtAction(nameof(GetBook), new { id = book.Id }, book);
    }

    [HttpPost("search-by-image")]
    public async Task<ActionResult<IEnumerable<BookDto>>> SearchBooksByImage([FromForm] IFormFile image)
    {
        if (image is null || image.Length == 0)
            return BadRequest("No image provided");

        await using var stream = image.OpenReadStream();
        var books = await bookService.SearchBooksByImageAsync(stream, image.FileName);
        return Ok(books);
    }
}
