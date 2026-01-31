using Alexandria.API.DTOs;
using Alexandria.API.Services;
using Alexandria.API.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alexandria.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BooksController(
    IBookService bookService,
    IBookLookupService bookLookupService,
    IOcrService ocrService,
    ILogger<BooksController> logger
) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<BookDto>>> SearchBooks(
        [FromQuery] BookSearchRequest request,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default
    )
    {
        // Normalize ISBN if provided
        if (!string.IsNullOrEmpty(request.Isbn))
        {
            request.Isbn = IsbnHelper.NormalizeToIsbn13(request.Isbn) ?? request.Isbn;
        }

        // First, search local database
        var books = await bookService.SearchBooksAsync(request, page, pageSize);
        
        // If local results found, return them
        if (books.Any())
        {
            return Ok(books);
        }

        // No local results - search external APIs
        logger.LogInformation("No local results found, searching external APIs for query: {Query}", request.Query);

        // If ISBN provided, do ISBN lookup
        if (!string.IsNullOrEmpty(request.Isbn))
        {
            var isbnResult = await bookLookupService.LookupByIsbnAsync(request.Isbn, cancellationToken);
            if (isbnResult is not null)
            {
                return Ok(new[] { MapPreviewToDto(isbnResult) });
            }
        }

        // Search by title/author
        if (!string.IsNullOrEmpty(request.Query) || !string.IsNullOrEmpty(request.Author))
        {
            var searchQuery = request.Query ?? request.Author ?? "";
            var externalResults = await bookLookupService.SearchAsync(
                searchQuery,
                request.Author,
                pageSize,
                cancellationToken
            );
            
            return Ok(externalResults.Select(MapPreviewToDto));
        }

        return Ok(Array.Empty<BookDto>());
    }

    private static BookDto MapPreviewToDto(BookPreviewDto preview) => new()
    {
        Id = preview.ExistingBookId ?? 0,
        Title = preview.Title,
        Author = preview.Author,
        Isbn = preview.Isbn,
        Publisher = preview.Publisher,
        PublishedYear = preview.PublishedYear,
        Description = preview.Description,
        CoverImageUrl = preview.CoverImageUrl,
        Genre = preview.Genre,
        PageCount = preview.PageCount
    };

    [HttpGet("{id}")]
    public async Task<ActionResult<BookDto>> GetBook(int id)
    {
        var book = await bookService.GetBookByIdAsync(id);
        return book is null ? NotFound() : Ok(book);
    }

    [HttpPost]
    public async Task<ActionResult<BookDto>> CreateBook(CreateBookRequest request)
    {
        // Normalize ISBN before saving
        if (!string.IsNullOrEmpty(request.Isbn))
        {
            request.Isbn = IsbnHelper.NormalizeToIsbn13(request.Isbn) ?? request.Isbn;
        }

        var book = await bookService.CreateBookAsync(request);
        return CreatedAtAction(nameof(GetBook), new { id = book.Id }, book);
    }

    [HttpPost("search-by-image")]
    public async Task<ActionResult<IEnumerable<BookDto>>> SearchBooksByImage(
        [FromForm] IFormFile image
    )
    {
        if (image is null || image.Length == 0)
            return BadRequest("No image provided");

        await using var stream = image.OpenReadStream();
        var books = await bookService.SearchBooksByImageAsync(stream, image.FileName);
        return Ok(books);
    }

    /// <summary>
    /// Scans a single book image (cover or barcode) and returns a preview for confirmation.
    /// </summary>
    [HttpPost("scan-single")]
    public async Task<ActionResult<BookPreviewDto>> ScanSingleBook(
        [FromForm] IFormFile image,
        CancellationToken cancellationToken
    )
    {
        if (image is null || image.Length == 0)
            return BadRequest("No image provided");

        logger.LogInformation(
            "Processing single book scan: {FileName}, Size: {Size} bytes",
            image.FileName,
            image.Length
        );

        await using var stream = image.OpenReadStream();
        var ocrResult = await ocrService.ExtractSingleBookAsync(
            stream,
            image.FileName,
            cancellationToken
        );

        // First priority: try ISBN lookup if we found any
        if (ocrResult.DetectedIsbns.Count > 0)
        {
            var isbn = ocrResult.DetectedIsbns.First();
            logger.LogInformation("Found ISBN {Isbn}, looking up book details", isbn);

            // Check local database first
            var localBooks = await bookService.SearchBooksAsync(
                new BookSearchRequest { Isbn = isbn },
                1,
                1
            );
            var localBook = localBooks.FirstOrDefault();

            if (localBook is not null)
            {
                return Ok(
                    new BookPreviewDto
                    {
                        ExistingBookId = localBook.Id,
                        Title = localBook.Title,
                        Author = localBook.Author,
                        Isbn = localBook.Isbn,
                        Publisher = localBook.Publisher,
                        PublishedYear = localBook.PublishedYear,
                        Description = localBook.Description,
                        CoverImageUrl = localBook.CoverImageUrl,
                        Genre = localBook.Genre,
                        PageCount = localBook.PageCount,
                        Source = BookSource.Local,
                        Confidence = ocrResult.Confidence,
                    }
                );
            }

            // Look up from external APIs
            var externalBook = await bookLookupService.LookupByIsbnAsync(isbn, cancellationToken);
            if (externalBook is not null)
            {
                externalBook.Confidence = ocrResult.Confidence;
                return Ok(externalBook);
            }
        }

        // Second priority: try title search if we found potential titles
        if (ocrResult.DetectedTitles.Count > 0)
        {
            var title = ocrResult.DetectedTitles.First();
            logger.LogInformation("No ISBN found, searching by title: '{Title}'", title);

            var searchResults = await bookLookupService.SearchAsync(
                title,
                maxResults: 1,
                cancellationToken: cancellationToken
            );
            var topResult = searchResults.FirstOrDefault();

            if (topResult is not null)
            {
                topResult.Confidence = ocrResult.Confidence * 0.8; // Lower confidence for title-based search
                return Ok(topResult);
            }
        }

        // Return OCR text result if we couldn't find a match
        if (ocrResult.DetectedTitles.Count > 0)
        {
            return Ok(
                new BookPreviewDto
                {
                    Title = ocrResult.DetectedTitles.First(),
                    Source = BookSource.OcrText,
                    Confidence = ocrResult.Confidence * 0.5,
                }
            );
        }

        return NotFound(
            new
            {
                message = "Could not extract book information from image",
                rawText = ocrResult.RawText,
            }
        );
    }

    /// <summary>
    /// Scans a bookshelf image and returns multiple book previews for bulk confirmation.
    /// </summary>
    [HttpPost("scan-bookshelf")]
    public async Task<ActionResult<List<BookPreviewDto>>> ScanBookshelf(
        [FromForm] IFormFile image,
        CancellationToken cancellationToken
    )
    {
        if (image is null || image.Length == 0)
            return BadRequest("No image provided");

        logger.LogInformation(
            "Processing bookshelf scan: {FileName}, Size: {Size} bytes",
            image.FileName,
            image.Length
        );

        await using var stream = image.OpenReadStream();
        var ocrResult = await ocrService.ExtractBookshelfAsync(
            stream,
            image.FileName,
            cancellationToken
        );

        var previews = new List<BookPreviewDto>();

        // Process detected ISBNs first (most reliable)
        if (ocrResult.DetectedIsbns.Count > 0)
        {
            logger.LogInformation(
                "Found {Count} ISBNs in bookshelf image",
                ocrResult.DetectedIsbns.Count
            );

            foreach (var isbn in ocrResult.DetectedIsbns)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                // Check local database first
                var localBooks = await bookService.SearchBooksAsync(
                    new BookSearchRequest { Isbn = isbn },
                    1,
                    1
                );
                var localBook = localBooks.FirstOrDefault();

                if (localBook is not null)
                {
                    previews.Add(
                        new BookPreviewDto
                        {
                            ExistingBookId = localBook.Id,
                            Title = localBook.Title,
                            Author = localBook.Author,
                            Isbn = localBook.Isbn,
                            Publisher = localBook.Publisher,
                            PublishedYear = localBook.PublishedYear,
                            Description = localBook.Description,
                            CoverImageUrl = localBook.CoverImageUrl,
                            Genre = localBook.Genre,
                            PageCount = localBook.PageCount,
                            Source = BookSource.Local,
                            Confidence = ocrResult.Confidence,
                        }
                    );
                }
                else
                {
                    // Look up from external APIs (with rate limiting built into the service)
                    var externalBook = await bookLookupService.LookupByIsbnAsync(
                        isbn,
                        cancellationToken
                    );
                    if (externalBook is not null)
                    {
                        externalBook.Confidence = ocrResult.Confidence;
                        previews.Add(externalBook);
                    }
                }
            }
        }

        // Process detected titles for books without ISBNs
        if (ocrResult.DetectedTitles.Count > 0)
        {
            var titlesToSearch = ocrResult
                .DetectedTitles.Where(t =>
                    !previews.Any(p => p.Title.Equals(t, StringComparison.OrdinalIgnoreCase))
                )
                .Take(10) // Limit title searches to avoid too many API calls
                .ToList();

            logger.LogInformation("Searching for {Count} additional titles", titlesToSearch.Count);

            foreach (var title in titlesToSearch)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var searchResults = await bookLookupService.SearchAsync(
                    title,
                    maxResults: 1,
                    cancellationToken: cancellationToken
                );
                var topResult = searchResults.FirstOrDefault();

                if (topResult is not null)
                {
                    topResult.Confidence = ocrResult.Confidence * 0.7; // Lower confidence for title-based

                    // Avoid duplicates
                    if (!previews.Any(p => p.Isbn == topResult.Isbn && topResult.Isbn is not null))
                    {
                        previews.Add(topResult);
                    }
                }
            }
        }

        logger.LogInformation("Bookshelf scan complete. Found {Count} books.", previews.Count);

        return Ok(previews);
    }

    /// <summary>
    /// Looks up a book by ISBN from external sources (useful for manual ISBN entry).
    /// </summary>
    [HttpGet("lookup/{isbn}")]
    public async Task<ActionResult<BookPreviewDto>> LookupByIsbn(
        string isbn,
        CancellationToken cancellationToken
    )
    {
        var normalizedIsbn = IsbnHelper.NormalizeToIsbn13(isbn);
        if (normalizedIsbn is null)
            return BadRequest("Invalid ISBN format");

        // Check local database first
        var localBooks = await bookService.SearchBooksAsync(
            new BookSearchRequest { Isbn = normalizedIsbn },
            1,
            1
        );
        var localBook = localBooks.FirstOrDefault();

        if (localBook is not null)
        {
            return Ok(
                new BookPreviewDto
                {
                    ExistingBookId = localBook.Id,
                    Title = localBook.Title,
                    Author = localBook.Author,
                    Isbn = localBook.Isbn,
                    Publisher = localBook.Publisher,
                    PublishedYear = localBook.PublishedYear,
                    Description = localBook.Description,
                    CoverImageUrl = localBook.CoverImageUrl,
                    Genre = localBook.Genre,
                    PageCount = localBook.PageCount,
                    Source = BookSource.Local,
                    Confidence = 1.0,
                }
            );
        }

        // Look up from external APIs
        var result = await bookLookupService.LookupByIsbnAsync(normalizedIsbn, cancellationToken);
        if (result is null)
            return NotFound(new { message = "Book not found", isbn = normalizedIsbn });

        return Ok(result);
    }
}
