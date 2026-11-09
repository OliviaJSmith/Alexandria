using Alexandria.Api.Dtos;

namespace Alexandria.Api.Services;

/// <summary>
/// Libraries and the copies in them. Visibility: a private library is visible only to its owner;
/// a public library is visible to its owner and the owner's accepted friends. Only owners can change a library.
/// Libraries the caller cannot see are reported as not found, so their existence is not revealed.
/// </summary>
public interface ILibraryService
{
    Task<ServiceResult<IReadOnlyList<LibraryDto>>> GetMyLibrariesAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<ServiceResult<LibraryDto>> GetLibraryAsync(Guid libraryId, Guid userId, CancellationToken cancellationToken = default);

    Task<ServiceResult<LibraryDto>> CreateLibraryAsync(Guid userId, CreateLibraryRequest request, CancellationToken cancellationToken = default);

    Task<ServiceResult<LibraryDto>> UpdateLibraryAsync(Guid libraryId, Guid userId, UpdateLibraryRequest request, CancellationToken cancellationToken = default);

    Task<ServiceResult<IReadOnlyList<LibraryBookDto>>> GetLibraryBooksAsync(Guid libraryId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Copies in any of the user's libraries that are currently lent out.</summary>
    Task<ServiceResult<IReadOnlyList<LibraryBookDto>>> GetLentOutBooksAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<ServiceResult<LibraryBookDto>> AddBookAsync(Guid libraryId, Guid userId, AddLibraryBookRequest request, CancellationToken cancellationToken = default);

    Task<ServiceResult<LibraryBookDto>> UpdateLibraryBookAsync(Guid libraryId, Guid libraryBookId, Guid userId, UpdateLibraryBookRequest request, CancellationToken cancellationToken = default);

    Task<ServiceResult<bool>> RemoveLibraryBookAsync(Guid libraryId, Guid libraryBookId, Guid userId, CancellationToken cancellationToken = default);

    Task<ServiceResult<LibraryBookDto>> MoveLibraryBookAsync(Guid libraryId, Guid libraryBookId, Guid userId, MoveLibraryBookRequest request, CancellationToken cancellationToken = default);
}
