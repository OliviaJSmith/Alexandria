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

/// <summary>
/// Represents a book found via OCR/external API before confirmation.
/// </summary>
public class BookPreviewDto
{
    /// <summary>
    /// If the book already exists in our database, this is its ID.
    /// Null if the book is from an external source only.
    /// </summary>
    public int? ExistingBookId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Author { get; set; }
    public string? Isbn { get; set; }
    public string? Publisher { get; set; }
    public int? PublishedYear { get; set; }
    public string? Description { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? Genre { get; set; }
    public int? PageCount { get; set; }

    /// <summary>
    /// Where this book information came from.
    /// </summary>
    public BookSource Source { get; set; }

    /// <summary>
    /// Confidence score from OCR extraction (0.0 - 1.0).
    /// </summary>
    public double Confidence { get; set; } = 1.0;

    /// <summary>
    /// External identifier for deduplication (e.g., Open Library work ID).
    /// </summary>
    public string? ExternalId { get; set; }
}

/// <summary>
/// Source of book data.
/// </summary>
public enum BookSource
{
    Local = 0,
    OpenLibrary = 1,
    GoogleBooks = 2,
    OcrText = 3
}

/// <summary>
/// Request to confirm and add scanned books to a library.
/// </summary>
public class ConfirmBooksRequest
{
    public List<BookPreviewDto> Books { get; set; } = [];
}

/// <summary>
/// Result of confirming books - includes created/matched book IDs.
/// </summary>
public class ConfirmBooksResult
{
    public List<ConfirmedBookResult> Results { get; set; } = [];
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
}

public class ConfirmedBookResult
{
    public int BookId { get; set; }
    public int? LibraryBookId { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool WasCreated { get; set; }
    public bool AddedToLibrary { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Result from OCR text extraction.
/// </summary>
public class OcrExtractionResult
{
    public List<string> DetectedIsbns { get; set; } = [];
    public List<string> DetectedTitles { get; set; } = [];
    public string RawText { get; set; } = string.Empty;
    public double Confidence { get; set; }
}

