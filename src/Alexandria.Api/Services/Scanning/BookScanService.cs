using Alexandria.Api.Dtos;

namespace Alexandria.Api.Services.Scanning;

public interface IBookScanService
{
    /// <summary>
    /// Reads a photo of one book or a shelf, identifies the books and matches each against the catalog
    /// (Alexandria first, then Open Library / Google Books). Nothing is saved; the caller adds the ones the user confirms.
    /// </summary>
    Task<ServiceResult<ScanResultDto>> ScanAsync(byte[] image, ScanMode mode, CancellationToken cancellationToken = default);
}

public class BookScanService(
    ITextRecognizer textRecognizer,
    IBookTextInterpreter interpreter,
    IBookService bookService) : IBookScanService
{
    public const int MaxImageBytes = 10 * 1024 * 1024;
    public const string NotConfiguredMessage = "Book scanning isn't set up on this server.";
    public const string InvalidImageMessage = "Upload a JPEG, PNG, WebP, GIF or BMP photo under 10 MB.";
    public const string RecognitionFailedMessage = "The photo couldn't be read right now. Please try again.";
    public const string NoTextMessage = "No text was found in the photo. Try a closer, well-lit picture.";
    public const string NoBooksMessage = "No books were recognized. Try a closer picture with the titles readable.";

    public async Task<ServiceResult<ScanResultDto>> ScanAsync(byte[] image, ScanMode mode, CancellationToken cancellationToken = default)
    {
        if (!textRecognizer.IsConfigured || !interpreter.IsConfigured)
        {
            return ServiceResult<ScanResultDto>.Failure(ServiceErrorType.Unavailable, NotConfiguredMessage);
        }

        if (image.Length == 0 || image.Length > MaxImageBytes || !ImageFormat.IsSupported(image))
        {
            return ServiceResult<ScanResultDto>.Failure(ServiceErrorType.Validation, InvalidImageMessage);
        }

        var text = await textRecognizer.ReadTextAsync(image, cancellationToken);
        if (text is null)
        {
            return ServiceResult<ScanResultDto>.Failure(ServiceErrorType.Unavailable, RecognitionFailedMessage);
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return ServiceResult<ScanResultDto>.Success(new ScanResultDto([], NoTextMessage));
        }

        var candidates = MergeIsbns(await interpreter.InterpretAsync(text, mode, cancellationToken), Isbn.ExtractFromText(text), mode);
        if (candidates.Count == 0)
        {
            return ServiceResult<ScanResultDto>.Success(new ScanResultDto([], NoBooksMessage));
        }

        // Sequential on purpose: the catalog lookups share one database context per request.
        var books = new List<ScannedBookDto>();
        foreach (var candidate in candidates)
        {
            books.Add(new ScannedBookDto(candidate.Title, candidate.Author, candidate.Isbn, await MatchAsync(candidate, cancellationToken)));
        }

        return ServiceResult<ScanResultDto>.Success(new ScanResultDto(books, null));
    }

    private async Task<BookDto?> MatchAsync(BookCandidate candidate, CancellationToken cancellationToken)
    {
        if (candidate.Isbn is not null)
        {
            var byIsbn = await bookService.LookupByIsbnAsync(candidate.Isbn, cancellationToken);
            if (byIsbn.IsSuccess)
            {
                return byIsbn.Value;
            }
        }

        var search = await bookService.SearchAsync(
            new BookSearchQuery { Query = candidate.Title, Author = candidate.Author, PageSize = 1 },
            cancellationToken);
        return search.IsSuccess ? search.Value!.FirstOrDefault() : null;
    }

    /// <summary>
    /// Keeps only valid ISBNs from the model. For a single book, an ISBN read straight from the text wins;
    /// a lone ISBN with no recognized title still becomes a candidate so the lookup can name it.
    /// </summary>
    private static List<BookCandidate> MergeIsbns(IReadOnlyList<BookCandidate> fromModel, IReadOnlyList<string> fromText, ScanMode mode)
    {
        var candidates = fromModel
            .Select(c => c with { Isbn = Isbn.TryNormalize(c.Isbn, out var isbn13) ? isbn13 : null })
            .ToList();

        if (mode == ScanMode.Single && fromText.Count > 0)
        {
            return candidates.Count == 0
                ? [new BookCandidate(fromText[0], null, fromText[0])]
                : [candidates[0] with { Isbn = candidates[0].Isbn ?? fromText[0] }];
        }

        return mode == ScanMode.Single ? candidates.Take(1).ToList() : candidates;
    }
}
