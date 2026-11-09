using Alexandria.Api.Dtos;
using Alexandria.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alexandria.Api.Controllers;

/// <summary>The shared book catalog. Signed-in users only, which also protects the external catalogs' quotas.</summary>
[Route("api/books")]
[Authorize]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public class BooksController(IBookService bookService) : ApiControllerBase
{
    /// <summary>
    /// Searches Alexandria's catalog, falling back to Open Library and Google Books when nothing matches.
    /// External results have a null id; create them with POST /api/books before adding them to a library.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<BookDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    // Not named "query": that matches the ?query= key and makes model binding look for "query.Query" instead.
    public async Task<ActionResult<IReadOnlyList<BookDto>>> Search([FromQuery] BookSearchQuery search, CancellationToken cancellationToken) =>
        ToActionResult(await bookService.SearchAsync(search, cancellationToken));

    [HttpGet("{bookId:guid}")]
    [ProducesResponseType<BookDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookDto>> GetBook(Guid bookId, CancellationToken cancellationToken) =>
        ToActionResult(await bookService.GetByIdAsync(bookId, cancellationToken));

    /// <summary>Looks up a book by ISBN-10 or ISBN-13 in Alexandria, then in the external catalogs.</summary>
    [HttpGet("lookup/{isbn}")]
    [ProducesResponseType<BookDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookDto>> LookupByIsbn(string isbn, CancellationToken cancellationToken) =>
        ToActionResult(await bookService.LookupByIsbnAsync(isbn, cancellationToken));

    /// <summary>Adds a book to the catalog: 201 when created, 200 with the existing entry when its ISBN is already known.</summary>
    [HttpPost]
    [ProducesResponseType<BookDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<BookDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BookDto>> CreateBook([FromBody] CreateBookRequest request, CancellationToken cancellationToken)
    {
        var result = await bookService.CreateAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return ToActionResult(result).Result!;
        }

        var (book, created) = result.Value!;
        return created
            ? CreatedAtAction(nameof(GetBook), new { bookId = book.Id }, book)
            : Ok(book);
    }
}
