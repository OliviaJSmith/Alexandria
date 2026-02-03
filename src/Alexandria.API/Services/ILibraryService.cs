using Alexandria.API.DTOs;

namespace Alexandria.API.Services;

public interface ILibraryService
{
    Task<IEnumerable<LibraryDto>> GetLibrariesAsync(int userId, bool? isPublic);
    Task<LibraryDto?> GetLibraryByIdAsync(int id, int userId);
    Task<LibraryDto> CreateLibraryAsync(int userId, CreateLibraryRequest request);
    Task<IEnumerable<LibraryBookDto>> GetLibraryBooksAsync(int libraryId, int userId);
    Task<IEnumerable<LibraryBookDto>> GetLentOutBooksAsync(int userId);
    Task<LibraryBookDto?> AddBookToLibraryAsync(int libraryId, int userId, AddBookToLibraryRequest request);
    Task<bool> RemoveBookFromLibraryAsync(int libraryId, int libraryBookId, int userId);
    Task<LibraryBookDto?> UpdateLibraryBookAsync(int libraryId, int libraryBookId, int userId, UpdateLibraryBookRequest request);
    Task<LibraryBookDto?> MoveBookToLibraryAsync(int sourceLibraryId, int libraryBookId, int targetLibraryId, int userId);
    Task<bool> UserHasAccessToLibraryAsync(int libraryId, int userId);
    Task<bool> UserOwnsLibraryAsync(int libraryId, int userId);
}
