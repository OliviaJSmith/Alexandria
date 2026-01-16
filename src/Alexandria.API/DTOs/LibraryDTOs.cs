using Alexandria.API.Models;

namespace Alexandria.API.DTOs;

public class LibraryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsPublic { get; set; }
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateLibraryRequest
{
    public string Name { get; set; } = string.Empty;
    public bool IsPublic { get; set; }
}

public class LibraryBookDto
{
    public int Id { get; set; }
    public int LibraryId { get; set; }
    public BookDto Book { get; set; } = null!;
    public BookStatus Status { get; set; }
    public DateTime AddedAt { get; set; }
}

public class AddBookToLibraryRequest
{
    public int BookId { get; set; }
    public BookStatus Status { get; set; } = BookStatus.Available;
}
