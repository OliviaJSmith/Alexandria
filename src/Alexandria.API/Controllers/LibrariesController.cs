using Alexandria.API.DTOs;
using Alexandria.API.Services;
using Alexandria.API.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alexandria.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LibrariesController(
    ILibraryService libraryService,
    IBookService bookService,
    ILogger<LibrariesController> logger
) : BaseController
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<LibraryDto>>> GetLibraries(
        [FromQuery] bool? isPublic = null
    )
    {
        var userId = GetCurrentUserId();
        var libraries = await libraryService.GetLibrariesAsync(userId, isPublic);
        return Ok(libraries);
    }

    [HttpGet("lent-out")]
    public async Task<ActionResult<IEnumerable<LibraryBookDto>>> GetLentOutBooks()
    {
        var userId = GetCurrentUserId();
        var books = await libraryService.GetLentOutBooksAsync(userId);
        return Ok(books);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<LibraryDto>> GetLibrary(int id)
    {
        var userId = GetCurrentUserId();
        var library = await libraryService.GetLibraryByIdAsync(id, userId);

        if (library is null)
            return NotFound();

        return Ok(library);
    }

    [HttpPost]
    public async Task<ActionResult<LibraryDto>> CreateLibrary(CreateLibraryRequest request)
    {
        var userId = GetCurrentUserId();
        var library = await libraryService.CreateLibraryAsync(userId, request);
        return CreatedAtAction(nameof(GetLibrary), new { id = library.Id }, library);
    }

    [HttpGet("{id}/books")]
    public async Task<ActionResult<IEnumerable<LibraryBookDto>>> GetLibraryBooks(int id)
    {
        var userId = GetCurrentUserId();

        if (!await libraryService.UserHasAccessToLibraryAsync(id, userId))
            return NotFound();

        var books = await libraryService.GetLibraryBooksAsync(id, userId);
        return Ok(books);
    }

    [HttpPost("{id}/books")]
    public async Task<ActionResult<LibraryBookDto>> AddBookToLibrary(
        int id,
        AddBookToLibraryRequest request
    )
    {
        var userId = GetCurrentUserId();

        if (!await libraryService.UserOwnsLibraryAsync(id, userId))
            return Forbid();

        var result = await libraryService.AddBookToLibraryAsync(id, userId, request);

        if (result is null)
            return NotFound("Library or book not found");

        return Ok(result);
    }

    [HttpDelete("{libraryId}/books/{libraryBookId}")]
    public async Task<ActionResult> RemoveBookFromLibrary(int libraryId, int libraryBookId)
    {
        var userId = GetCurrentUserId();

        if (!await libraryService.UserOwnsLibraryAsync(libraryId, userId))
            return Forbid();

        var removed = await libraryService.RemoveBookFromLibraryAsync(
            libraryId,
            libraryBookId,
            userId
        );

        if (!removed)
            return NotFound("Book not found in library");

        return NoContent();
    }

    [HttpPatch("{libraryId}/books/{libraryBookId}")]
    public async Task<ActionResult<LibraryBookDto>> UpdateLibraryBook(
        int libraryId, 
        int libraryBookId, 
        UpdateLibraryBookRequest request)
    {
        var userId = GetCurrentUserId();

        if (!await libraryService.UserOwnsLibraryAsync(libraryId, userId))
            return Forbid();

        var result = await libraryService.UpdateLibraryBookAsync(libraryId, libraryBookId, userId, request);

        if (result is null)
            return NotFound("Book not found in library");

        return Ok(result);
    }

    [HttpPost("{libraryId}/books/{libraryBookId}/move")]
    public async Task<ActionResult<LibraryBookDto>> MoveBookToLibrary(
        int libraryId, 
        int libraryBookId, 
        MoveBookToLibraryRequest request)
    {
        var userId = GetCurrentUserId();

        if (!await libraryService.UserOwnsLibraryAsync(libraryId, userId))
            return Forbid();

        if (!await libraryService.UserOwnsLibraryAsync(request.TargetLibraryId, userId))
            return Forbid();

        var result = await libraryService.MoveBookToLibraryAsync(
            libraryId, 
            libraryBookId, 
            request.TargetLibraryId, 
            userId);

        if (result is null)
            return NotFound("Book not found in library");

        return Ok(result);
    }

    /// <summary>
    /// Confirms and adds scanned books to a library.
    /// Creates new book records if they don't exist, then adds them to the library.
    /// </summary>
    [HttpPost("{id}/confirm-books")]
    public async Task<ActionResult<ConfirmBooksResult>> ConfirmBooks(
        int id,
        ConfirmBooksRequest request
    )
    {
        var userId = GetCurrentUserId();

        if (!await libraryService.UserOwnsLibraryAsync(id, userId))
            return Forbid();

        var result = new ConfirmBooksResult();

        foreach (var preview in request.Books)
        {
            var confirmedResult = new ConfirmedBookResult { Title = preview.Title };

            try
            {
                int bookId;

                // If the book already exists in our database, use that ID
                if (preview.ExistingBookId.HasValue)
                {
                    bookId = preview.ExistingBookId.Value;
                    confirmedResult.WasCreated = false;
                }
                else
                {
                    // Create a new book record
                    string? normalizedIsbn = null;
                    if (!string.IsNullOrEmpty(preview.Isbn))
                    {
                        normalizedIsbn = IsbnHelper.NormalizeToIsbn13(preview.Isbn);
                        if (normalizedIsbn is null)
                        {
                            logger.LogWarning(
                                "Invalid ISBN '{Isbn}' for book '{Title}'. The ISBN will not be stored.",
                                preview.Isbn,
                                preview.Title
                            );
                        }
                    }

                    var createRequest = new CreateBookRequest
                    {
                        Title = preview.Title,
                        Author = preview.Author,
                        Isbn = normalizedIsbn,
                        Publisher = preview.Publisher,
                        PublishedYear = preview.PublishedYear,
                        Description = preview.Description,
                        CoverImageUrl = preview.CoverImageUrl,
                        Genre = preview.Genre,
                        PageCount = preview.PageCount,
                    };

                    var createdBook = await bookService.CreateBookAsync(createRequest);
                    bookId = createdBook.Id;
                    confirmedResult.WasCreated = true;
                }

                confirmedResult.BookId = bookId;

                // Add book to library
                var addRequest = new AddBookToLibraryRequest { BookId = bookId };
                var libraryBook = await libraryService.AddBookToLibraryAsync(
                    id,
                    userId,
                    addRequest
                );

                if (libraryBook is not null)
                {
                    confirmedResult.LibraryBookId = libraryBook.Id;
                    confirmedResult.AddedToLibrary = true;
                    result.SuccessCount++;
                }
                else
                {
                    confirmedResult.Error = "Failed to add book to library";
                    result.FailedCount++;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error confirming book '{Title}'", preview.Title);
                confirmedResult.Error = "An error occurred while processing this book";
                result.FailedCount++;
            }

            result.Results.Add(confirmedResult);
        }

        logger.LogInformation(
            "Confirmed {SuccessCount} books (failed: {FailedCount}) to library {LibraryId}",
            result.SuccessCount,
            result.FailedCount,
            id
        );

        return Ok(result);
    }
}
