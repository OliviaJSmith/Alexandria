using Alexandria.Api.Dtos;
using Alexandria.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alexandria.Api.Controllers;

/// <summary>
/// The signed-in user's libraries and the copies in them. Private libraries are visible only to their owner;
/// public ones also to the owner's friends. Only the owner can change a library.
/// </summary>
[Route("api/libraries")]
[Authorize]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public class LibrariesController(ILibraryService libraryService) : ApiControllerBase
{
    /// <summary>Libraries the signed-in user owns.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<LibraryDto>>(StatusCodes.Status200OK)]
    public Task<ActionResult<IReadOnlyList<LibraryDto>>> GetMyLibraries(CancellationToken cancellationToken) =>
        ForCurrentUserAsync(userId => libraryService.GetMyLibrariesAsync(userId, cancellationToken));

    /// <summary>Copies in the signed-in user's libraries that are currently lent out.</summary>
    [HttpGet("lent-out")]
    [ProducesResponseType<IReadOnlyList<LibraryBookDto>>(StatusCodes.Status200OK)]
    public Task<ActionResult<IReadOnlyList<LibraryBookDto>>> GetLentOutBooks(CancellationToken cancellationToken) =>
        ForCurrentUserAsync(userId => libraryService.GetLentOutBooksAsync(userId, cancellationToken));

    [HttpGet("{libraryId:guid}")]
    [ProducesResponseType<LibraryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<LibraryDto>> GetLibrary(Guid libraryId, CancellationToken cancellationToken) =>
        ForCurrentUserAsync(userId => libraryService.GetLibraryAsync(libraryId, userId, cancellationToken));

    [HttpPost]
    [ProducesResponseType<LibraryDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<ActionResult<LibraryDto>> CreateLibrary([FromBody] CreateLibraryRequest request, CancellationToken cancellationToken) =>
        ForCurrentUserAsync(
            userId => libraryService.CreateLibraryAsync(userId, request, cancellationToken),
            library => CreatedAtAction(nameof(GetLibrary), new { libraryId = library.Id }, library));

    /// <summary>Renames a library or switches it between public and private. Owner only.</summary>
    [HttpPatch("{libraryId:guid}")]
    [ProducesResponseType<LibraryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<LibraryDto>> UpdateLibrary(Guid libraryId, [FromBody] UpdateLibraryRequest request, CancellationToken cancellationToken) =>
        ForCurrentUserAsync(userId => libraryService.UpdateLibraryAsync(libraryId, userId, request, cancellationToken));

    [HttpGet("{libraryId:guid}/books")]
    [ProducesResponseType<IReadOnlyList<LibraryBookDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<IReadOnlyList<LibraryBookDto>>> GetLibraryBooks(Guid libraryId, CancellationToken cancellationToken) =>
        ForCurrentUserAsync(userId => libraryService.GetLibraryBooksAsync(libraryId, userId, cancellationToken));

    /// <summary>Adds a catalog book to a library. Returns 409 if the library already has it, unless forceAdd is set.</summary>
    [HttpPost("{libraryId:guid}/books")]
    [ProducesResponseType<LibraryBookDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<ActionResult<LibraryBookDto>> AddBook(Guid libraryId, [FromBody] AddLibraryBookRequest request, CancellationToken cancellationToken) =>
        ForCurrentUserAsync(
            userId => libraryService.AddBookAsync(libraryId, userId, request, cancellationToken),
            copy => StatusCode(StatusCodes.Status201Created, copy));

    /// <summary>Updates a copy's status or loan note, or the book's genre.</summary>
    [HttpPatch("{libraryId:guid}/books/{libraryBookId:guid}")]
    [ProducesResponseType<LibraryBookDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<LibraryBookDto>> UpdateLibraryBook(
        Guid libraryId,
        Guid libraryBookId,
        [FromBody] UpdateLibraryBookRequest request,
        CancellationToken cancellationToken) =>
        ForCurrentUserAsync(userId => libraryService.UpdateLibraryBookAsync(libraryId, libraryBookId, userId, request, cancellationToken));

    /// <summary>Removes a copy from a library. Returns 409 while the copy is lent out.</summary>
    [HttpDelete("{libraryId:guid}/books/{libraryBookId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> RemoveLibraryBook(Guid libraryId, Guid libraryBookId, CancellationToken cancellationToken)
    {
        var result = await ForCurrentUserAsync(
            userId => libraryService.RemoveLibraryBookAsync(libraryId, libraryBookId, userId, cancellationToken),
            _ => NoContent());
        return result.Result!;
    }

    /// <summary>Moves a copy to another library the user owns. Returns 409 while the copy is lent out.</summary>
    [HttpPost("{libraryId:guid}/books/{libraryBookId:guid}/move")]
    [ProducesResponseType<LibraryBookDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<ActionResult<LibraryBookDto>> MoveLibraryBook(
        Guid libraryId,
        Guid libraryBookId,
        [FromBody] MoveLibraryBookRequest request,
        CancellationToken cancellationToken) =>
        ForCurrentUserAsync(userId => libraryService.MoveLibraryBookAsync(libraryId, libraryBookId, userId, request, cancellationToken));
}
