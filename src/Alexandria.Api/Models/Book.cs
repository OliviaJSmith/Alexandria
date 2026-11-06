namespace Alexandria.Api.Models;

/// <summary>Bibliographic record shared by every library that holds a copy.</summary>
public class Book
{
    public Guid Id { get; set; }

    public required string Title { get; set; }

    public string? Author { get; set; }

    public string? Isbn { get; set; }

    public string? Publisher { get; set; }

    public int? PublishedYear { get; set; }

    public string? Description { get; set; }

    public string? CoverImageUrl { get; set; }

    public string? Genre { get; set; }

    public int? PageCount { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<LibraryBook> LibraryBooks { get; set; } = [];
}
