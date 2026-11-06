namespace Alexandria.Api.Models;

public class Library
{
    public Guid Id { get; set; }

    public Guid OwnerId { get; set; }

    public User? Owner { get; set; }

    public required string Name { get; set; }

    /// <summary>Public libraries are visible to the owner's friends.</summary>
    public bool IsPublic { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<LibraryBook> LibraryBooks { get; set; } = [];
}
