using Alexandria.Api.Models;

namespace Alexandria.Api.Repositories;

public interface ILibraryRepository
{
    Task<Library?> GetByIdAsync(Guid libraryId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Library>> GetByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default);

    void Add(Library library);

    /// <summary>Copies in a library, with their <see cref="Book"/> loaded, newest first.</summary>
    Task<IReadOnlyList<LibraryBook>> GetLibraryBooksAsync(Guid libraryId, CancellationToken cancellationToken = default);

    /// <summary>A copy in the given library, with its <see cref="Book"/> loaded.</summary>
    Task<LibraryBook?> GetLibraryBookAsync(Guid libraryId, Guid libraryBookId, CancellationToken cancellationToken = default);

    /// <summary>A copy by id alone, with its <see cref="Book"/> and <see cref="Library"/> loaded.</summary>
    Task<LibraryBook?> GetLibraryBookByIdAsync(Guid libraryBookId, CancellationToken cancellationToken = default);

    Task<bool> ContainsBookAsync(Guid libraryId, Guid bookId, CancellationToken cancellationToken = default);

    /// <summary>Copies in any of the owner's libraries with the given status, with their <see cref="Book"/> loaded.</summary>
    Task<IReadOnlyList<LibraryBook>> GetOwnerBooksByStatusAsync(Guid ownerId, LibraryBookStatus status, CancellationToken cancellationToken = default);

    void AddLibraryBook(LibraryBook libraryBook);

    void RemoveLibraryBook(LibraryBook libraryBook);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
