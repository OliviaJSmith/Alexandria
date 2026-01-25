using Alexandria.API.DTOs;
using Alexandria.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alexandria.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LibrariesController(ILibraryService libraryService, ILogger<LibrariesController> logger) : BaseController
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<LibraryDto>>> GetLibraries([FromQuery] bool? isPublic = null)
    {
        var userId = GetCurrentUserId();
        var libraries = await libraryService.GetLibrariesAsync(userId, isPublic);
        return Ok(libraries);
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
    public async Task<ActionResult<LibraryBookDto>> AddBookToLibrary(int id, AddBookToLibraryRequest request)
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

        var removed = await libraryService.RemoveBookFromLibraryAsync(libraryId, libraryBookId, userId);

        if (!removed)
            return NotFound("Book not found in library");

        return NoContent();
    }
}
