using System.ComponentModel.DataAnnotations;
using Alexandria.Api.Models;

namespace Alexandria.Api.Dtos;

public sealed record LibraryDto(
    Guid Id,
    string Name,
    bool IsPublic,
    Guid OwnerId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static LibraryDto FromLibrary(Library library) => new(
        library.Id,
        library.Name,
        library.IsPublic,
        library.OwnerId,
        library.CreatedAt,
        library.UpdatedAt);
}

public sealed record LibraryBookDto(
    Guid Id,
    Guid LibraryId,
    LibraryBookStatus Status,
    string? LoanNote,
    DateTimeOffset AddedAt,
    BookDto Book)
{
    /// <summary>Maps a copy whose <see cref="LibraryBook.Book"/> is loaded.</summary>
    public static LibraryBookDto FromLibraryBook(LibraryBook libraryBook) => new(
        libraryBook.Id,
        libraryBook.LibraryId,
        libraryBook.Status,
        libraryBook.LoanNote,
        libraryBook.AddedAt,
        BookDto.FromBook(libraryBook.Book
            ?? throw new InvalidOperationException("LibraryBook.Book must be loaded before mapping.")));
}

public class CreateLibraryRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Public libraries are visible to the owner's friends; private ones only to the owner.</summary>
    public bool IsPublic { get; set; }
}

/// <summary>Fields left null are not changed.</summary>
public class UpdateLibraryRequest
{
    [StringLength(200, MinimumLength = 1)]
    public string? Name { get; set; }

    public bool? IsPublic { get; set; }
}

public class AddLibraryBookRequest
{
    [Required]
    public Guid BookId { get; set; }

    [EnumDataType(typeof(LibraryBookStatus))]
    public LibraryBookStatus Status { get; set; } = LibraryBookStatus.Available;

    /// <summary>Add another copy even if the library already holds this book.</summary>
    public bool ForceAdd { get; set; }
}

/// <summary>Fields left null are not changed; an empty string clears <see cref="LoanNote"/> or <see cref="Genre"/>.</summary>
public class UpdateLibraryBookRequest
{
    [EnumDataType(typeof(LibraryBookStatus))]
    public LibraryBookStatus? Status { get; set; }

    [StringLength(500)]
    public string? LoanNote { get; set; }

    [StringLength(100)]
    public string? Genre { get; set; }
}

public class MoveLibraryBookRequest
{
    [Required]
    public Guid TargetLibraryId { get; set; }
}
