using System.ComponentModel.DataAnnotations;
using Alexandria.Api.Models;

namespace Alexandria.Api.Dtos;

/// <summary>
/// A book from the catalog. <see cref="Id"/> is null for results from Open Library or Google Books
/// that are not in Alexandria yet; create them with POST /api/books before adding to a library.
/// </summary>
public sealed record BookDto(
    Guid? Id,
    string Title,
    string? Author,
    string? Isbn,
    string? Publisher,
    int? PublishedYear,
    string? Description,
    string? CoverImageUrl,
    string? Genre,
    int? PageCount)
{
    public static BookDto FromBook(Book book) => new(
        book.Id,
        book.Title,
        book.Author,
        book.Isbn,
        book.Publisher,
        book.PublishedYear,
        book.Description,
        book.CoverImageUrl,
        book.Genre,
        book.PageCount);
}

public class BookSearchQuery
{
    /// <summary>Matches title or author.</summary>
    [StringLength(200)]
    public string? Query { get; set; }

    [StringLength(200)]
    public string? Author { get; set; }

    [StringLength(100)]
    public string? Genre { get; set; }

    /// <summary>ISBN-10 or ISBN-13, with or without hyphens.</summary>
    [StringLength(20)]
    public string? Isbn { get; set; }

    [Range(0, 3000)]
    public int? PublishedYear { get; set; }

    [Range(1, 1000)]
    public int Page { get; set; } = 1;

    [Range(1, 50)]
    public int PageSize { get; set; } = 20;
}

public class CreateBookRequest
{
    [Required]
    [StringLength(500, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Author { get; set; }

    /// <summary>ISBN-10 or ISBN-13, with or without hyphens; stored as ISBN-13.</summary>
    [StringLength(20)]
    public string? Isbn { get; set; }

    [StringLength(300)]
    public string? Publisher { get; set; }

    [Range(0, 3000)]
    public int? PublishedYear { get; set; }

    [StringLength(10000)]
    public string? Description { get; set; }

    [StringLength(2048)]
    public string? CoverImageUrl { get; set; }

    [StringLength(100)]
    public string? Genre { get; set; }

    [Range(1, 100000)]
    public int? PageCount { get; set; }
}

/// <summary>The catalog entry for a create request, and whether it was newly created or already existed (matched by ISBN).</summary>
public sealed record CreateBookResult(BookDto Book, bool Created);
