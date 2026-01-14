namespace Alexandria.API.Models;

public class Library
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsPublic { get; set; }
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<LibraryBook> LibraryBooks { get; set; } = new List<LibraryBook>();
}
