namespace Alexandria.API.Models;

public class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Author { get; set; }
    public string? Isbn { get; set; }
    public string? Publisher { get; set; }
    public int? PublishedYear { get; set; }
    public string? Description { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? Genre { get; set; }
    public int? PageCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<LibraryBook> LibraryBooks { get; set; } = new List<LibraryBook>();
}
