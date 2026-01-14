namespace Alexandria.API.DTOs;

public class BookSearchRequest
{
    public string? Query { get; set; }
    public string? Author { get; set; }
    public string? Genre { get; set; }
    public string? Isbn { get; set; }
    public int? PublishedYear { get; set; }
}

public class BookDto
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
}

public class CreateBookRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Author { get; set; }
    public string? Isbn { get; set; }
    public string? Publisher { get; set; }
    public int? PublishedYear { get; set; }
    public string? Description { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? Genre { get; set; }
    public int? PageCount { get; set; }
}
