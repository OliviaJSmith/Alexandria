using Alexandria.Api.Dtos;
using Alexandria.Api.Models;
using Alexandria.Api.Repositories;

namespace Alexandria.Api.Services;

public class LibraryService(
    ILibraryRepository libraries,
    IBookRepository books,
    IFriendshipRepository friendships,
    TimeProvider timeProvider) : ILibraryService
{
    public const string LibraryNotFoundMessage = "Library not found.";
    public const string NotOwnerMessage = "Only the library's owner can change it.";
    public const string BookNotFoundMessage = "Book not found.";
    public const string LibraryBookNotFoundMessage = "Book not found in this library.";
    public const string DuplicateBookMessage = "This book is already in the library. Set forceAdd to add another copy.";
    public const string BookLentOutMessage = "This book is lent out. Mark it returned before removing or moving it.";
    public const string NameRequiredMessage = "Library name cannot be blank.";

    public async Task<ServiceResult<IReadOnlyList<LibraryDto>>> GetMyLibrariesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var owned = await libraries.GetByOwnerAsync(userId, cancellationToken);
        return ServiceResult<IReadOnlyList<LibraryDto>>.Success(owned.Select(LibraryDto.FromLibrary).ToList());
    }

    public async Task<ServiceResult<LibraryDto>> GetLibraryAsync(Guid libraryId, Guid userId, CancellationToken cancellationToken = default)
    {
        var library = await GetVisibleLibraryAsync(libraryId, userId, cancellationToken);
        return library is null
            ? ServiceResult<LibraryDto>.Failure(ServiceErrorType.NotFound, LibraryNotFoundMessage)
            : ServiceResult<LibraryDto>.Success(LibraryDto.FromLibrary(library));
    }

    public async Task<ServiceResult<LibraryDto>> CreateLibraryAsync(Guid userId, CreateLibraryRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return ServiceResult<LibraryDto>.Failure(ServiceErrorType.Validation, NameRequiredMessage);
        }

        var now = timeProvider.GetUtcNow();
        var library = new Library
        {
            Id = Guid.NewGuid(),
            OwnerId = userId,
            Name = request.Name.Trim(),
            IsPublic = request.IsPublic,
            CreatedAt = now,
            UpdatedAt = now,
        };
        libraries.Add(library);
        await libraries.SaveChangesAsync(cancellationToken);
        return ServiceResult<LibraryDto>.Success(LibraryDto.FromLibrary(library));
    }

    public async Task<ServiceResult<LibraryDto>> UpdateLibraryAsync(
        Guid libraryId,
        Guid userId,
        UpdateLibraryRequest request,
        CancellationToken cancellationToken = default)
    {
        var (library, error) = await GetOwnedLibraryAsync(libraryId, userId, cancellationToken);
        if (library is null)
        {
            return ServiceResult<LibraryDto>.Failure(error!.Type, error.Message);
        }

        if (request.Name is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return ServiceResult<LibraryDto>.Failure(ServiceErrorType.Validation, NameRequiredMessage);
            }

            library.Name = request.Name.Trim();
        }

        if (request.IsPublic is { } isPublic)
        {
            library.IsPublic = isPublic;
        }

        library.UpdatedAt = timeProvider.GetUtcNow();
        await libraries.SaveChangesAsync(cancellationToken);
        return ServiceResult<LibraryDto>.Success(LibraryDto.FromLibrary(library));
    }

    public async Task<ServiceResult<IReadOnlyList<LibraryBookDto>>> GetLibraryBooksAsync(
        Guid libraryId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (await GetVisibleLibraryAsync(libraryId, userId, cancellationToken) is null)
        {
            return ServiceResult<IReadOnlyList<LibraryBookDto>>.Failure(ServiceErrorType.NotFound, LibraryNotFoundMessage);
        }

        var copies = await libraries.GetLibraryBooksAsync(libraryId, cancellationToken);
        return ServiceResult<IReadOnlyList<LibraryBookDto>>.Success(copies.Select(LibraryBookDto.FromLibraryBook).ToList());
    }

    public async Task<ServiceResult<IReadOnlyList<LibraryBookDto>>> GetLentOutBooksAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var lentOut = await libraries.GetOwnerBooksByStatusAsync(userId, LibraryBookStatus.CheckedOut, cancellationToken);
        return ServiceResult<IReadOnlyList<LibraryBookDto>>.Success(lentOut.Select(LibraryBookDto.FromLibraryBook).ToList());
    }

    public async Task<ServiceResult<LibraryBookDto>> AddBookAsync(
        Guid libraryId,
        Guid userId,
        AddLibraryBookRequest request,
        CancellationToken cancellationToken = default)
    {
        var (library, error) = await GetOwnedLibraryAsync(libraryId, userId, cancellationToken);
        if (library is null)
        {
            return ServiceResult<LibraryBookDto>.Failure(error!.Type, error.Message);
        }

        var book = await books.GetByIdAsync(request.BookId, cancellationToken);
        if (book is null)
        {
            return ServiceResult<LibraryBookDto>.Failure(ServiceErrorType.NotFound, BookNotFoundMessage);
        }

        if (!request.ForceAdd && await libraries.ContainsBookAsync(libraryId, book.Id, cancellationToken))
        {
            return ServiceResult<LibraryBookDto>.Failure(ServiceErrorType.Conflict, DuplicateBookMessage);
        }

        var copy = new LibraryBook
        {
            Id = Guid.NewGuid(),
            LibraryId = library.Id,
            BookId = book.Id,
            Book = book,
            Status = request.Status,
            AddedAt = timeProvider.GetUtcNow(),
        };
        libraries.AddLibraryBook(copy);
        await libraries.SaveChangesAsync(cancellationToken);
        return ServiceResult<LibraryBookDto>.Success(LibraryBookDto.FromLibraryBook(copy));
    }

    public async Task<ServiceResult<LibraryBookDto>> UpdateLibraryBookAsync(
        Guid libraryId,
        Guid libraryBookId,
        Guid userId,
        UpdateLibraryBookRequest request,
        CancellationToken cancellationToken = default)
    {
        var (copy, error) = await GetOwnedCopyAsync(libraryId, libraryBookId, userId, cancellationToken);
        if (copy is null)
        {
            return ServiceResult<LibraryBookDto>.Failure(error!.Type, error.Message);
        }

        if (request.Status is { } status)
        {
            copy.Status = status;
        }

        if (request.LoanNote is not null)
        {
            copy.LoanNote = NullIfBlank(request.LoanNote);
        }

        if (request.Genre is not null)
        {
            // Genre lives on the shared catalog entry, as in the previous app.
            copy.Book!.Genre = NullIfBlank(request.Genre);
            copy.Book.UpdatedAt = timeProvider.GetUtcNow();
        }

        await libraries.SaveChangesAsync(cancellationToken);
        return ServiceResult<LibraryBookDto>.Success(LibraryBookDto.FromLibraryBook(copy));
    }

    public async Task<ServiceResult<bool>> RemoveLibraryBookAsync(
        Guid libraryId,
        Guid libraryBookId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var (copy, error) = await GetOwnedCopyAsync(libraryId, libraryBookId, userId, cancellationToken);
        if (copy is null)
        {
            return ServiceResult<bool>.Failure(error!.Type, error.Message);
        }

        if (copy.Status == LibraryBookStatus.CheckedOut)
        {
            return ServiceResult<bool>.Failure(ServiceErrorType.Conflict, BookLentOutMessage);
        }

        libraries.RemoveLibraryBook(copy);
        await libraries.SaveChangesAsync(cancellationToken);
        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<LibraryBookDto>> MoveLibraryBookAsync(
        Guid libraryId,
        Guid libraryBookId,
        Guid userId,
        MoveLibraryBookRequest request,
        CancellationToken cancellationToken = default)
    {
        var (copy, error) = await GetOwnedCopyAsync(libraryId, libraryBookId, userId, cancellationToken);
        if (copy is null)
        {
            return ServiceResult<LibraryBookDto>.Failure(error!.Type, error.Message);
        }

        var (target, targetError) = await GetOwnedLibraryAsync(request.TargetLibraryId, userId, cancellationToken);
        if (target is null)
        {
            return ServiceResult<LibraryBookDto>.Failure(targetError!.Type, targetError.Message);
        }

        if (copy.Status == LibraryBookStatus.CheckedOut)
        {
            return ServiceResult<LibraryBookDto>.Failure(ServiceErrorType.Conflict, BookLentOutMessage);
        }

        copy.LibraryId = target.Id;
        await libraries.SaveChangesAsync(cancellationToken);
        return ServiceResult<LibraryBookDto>.Success(LibraryBookDto.FromLibraryBook(copy));
    }

    private async Task<Library?> GetVisibleLibraryAsync(Guid libraryId, Guid userId, CancellationToken cancellationToken)
    {
        var library = await libraries.GetByIdAsync(libraryId, cancellationToken);
        if (library is null)
        {
            return null;
        }

        if (library.OwnerId == userId)
        {
            return library;
        }

        return library.IsPublic && await friendships.AreFriendsAsync(library.OwnerId, userId, cancellationToken)
            ? library
            : null;
    }

    /// <summary>Not found if the caller cannot see the library; forbidden if they can see it but do not own it.</summary>
    private async Task<(Library? Library, ServiceError? Error)> GetOwnedLibraryAsync(
        Guid libraryId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var library = await GetVisibleLibraryAsync(libraryId, userId, cancellationToken);
        if (library is null)
        {
            return (null, new ServiceError(ServiceErrorType.NotFound, LibraryNotFoundMessage));
        }

        return library.OwnerId == userId
            ? (library, null)
            : (null, new ServiceError(ServiceErrorType.Forbidden, NotOwnerMessage));
    }

    private async Task<(LibraryBook? Copy, ServiceError? Error)> GetOwnedCopyAsync(
        Guid libraryId,
        Guid libraryBookId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var (library, error) = await GetOwnedLibraryAsync(libraryId, userId, cancellationToken);
        if (library is null)
        {
            return (null, error);
        }

        var copy = await libraries.GetLibraryBookAsync(libraryId, libraryBookId, cancellationToken);
        return copy is null
            ? (null, new ServiceError(ServiceErrorType.NotFound, LibraryBookNotFoundMessage))
            : (copy, null);
    }

    private static string? NullIfBlank(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
