using Alexandria.Api.Dtos;
using Alexandria.Api.Models;
using Alexandria.Api.Repositories;
using Alexandria.Api.Services.BookCatalog;

namespace Alexandria.Api.Services;

public class BookService(
    IBookRepository books,
    IBookLookupService bookLookup,
    TimeProvider timeProvider) : IBookService
{
    public const string SearchCriteriaRequiredMessage = "Provide at least one of query, author, genre, isbn or publishedYear.";
    public const string InvalidIsbnMessage = "The ISBN is not a valid ISBN-10 or ISBN-13.";
    public const string InvalidCoverUrlMessage = "The cover image URL must be an absolute http or https URL.";
    public const string TitleRequiredMessage = "Title cannot be blank.";
    public const string BookNotFoundMessage = "Book not found.";

    public async Task<ServiceResult<IReadOnlyList<BookDto>>> SearchAsync(BookSearchQuery query, CancellationToken cancellationToken = default)
    {
        if (IsBlank(query.Query) && IsBlank(query.Author) && IsBlank(query.Genre) && IsBlank(query.Isbn) && query.PublishedYear is null)
        {
            return ServiceResult<IReadOnlyList<BookDto>>.Failure(ServiceErrorType.Validation, SearchCriteriaRequiredMessage);
        }

        string? isbn13 = null;
        if (!IsBlank(query.Isbn) && !Isbn.TryNormalize(query.Isbn, out isbn13))
        {
            return ServiceResult<IReadOnlyList<BookDto>>.Failure(ServiceErrorType.Validation, InvalidIsbnMessage);
        }

        var criteria = new BookSearchCriteria(TrimToNull(query.Query), TrimToNull(query.Author), TrimToNull(query.Genre), isbn13, query.PublishedYear);
        var local = await books.SearchAsync(criteria, (query.Page - 1) * query.PageSize, query.PageSize, cancellationToken);
        if (local.Count > 0 || query.Page > 1)
        {
            return ServiceResult<IReadOnlyList<BookDto>>.Success(local.Select(BookDto.FromBook).ToList());
        }

        var external = await SearchExternalAsync(criteria, query.PageSize, cancellationToken);
        return ServiceResult<IReadOnlyList<BookDto>>.Success(external);
    }

    public async Task<ServiceResult<BookDto>> GetByIdAsync(Guid bookId, CancellationToken cancellationToken = default)
    {
        var book = await books.GetByIdAsync(bookId, cancellationToken);
        return book is null
            ? ServiceResult<BookDto>.Failure(ServiceErrorType.NotFound, BookNotFoundMessage)
            : ServiceResult<BookDto>.Success(BookDto.FromBook(book));
    }

    public async Task<ServiceResult<CreateBookResult>> CreateAsync(CreateBookRequest request, CancellationToken cancellationToken = default)
    {
        if (IsBlank(request.Title))
        {
            return ServiceResult<CreateBookResult>.Failure(ServiceErrorType.Validation, TitleRequiredMessage);
        }

        string? isbn13 = null;
        if (!IsBlank(request.Isbn) && !Isbn.TryNormalize(request.Isbn, out isbn13))
        {
            return ServiceResult<CreateBookResult>.Failure(ServiceErrorType.Validation, InvalidIsbnMessage);
        }

        if (!IsBlank(request.CoverImageUrl) && !IsHttpUrl(request.CoverImageUrl!))
        {
            return ServiceResult<CreateBookResult>.Failure(ServiceErrorType.Validation, InvalidCoverUrlMessage);
        }

        if (isbn13 is not null && await books.GetByIsbnAsync(isbn13, cancellationToken) is { } existing)
        {
            return ServiceResult<CreateBookResult>.Success(new CreateBookResult(BookDto.FromBook(existing), Created: false));
        }

        var now = timeProvider.GetUtcNow();
        var book = new Book
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Author = TrimToNull(request.Author),
            Isbn = isbn13,
            Publisher = TrimToNull(request.Publisher),
            PublishedYear = request.PublishedYear,
            Description = TrimToNull(request.Description),
            CoverImageUrl = TrimToNull(request.CoverImageUrl),
            Genre = TrimToNull(request.Genre),
            PageCount = request.PageCount,
            CreatedAt = now,
            UpdatedAt = now,
        };
        books.Add(book);
        await books.SaveChangesAsync(cancellationToken);
        return ServiceResult<CreateBookResult>.Success(new CreateBookResult(BookDto.FromBook(book), Created: true));
    }

    public async Task<ServiceResult<BookDto>> LookupByIsbnAsync(string isbn, CancellationToken cancellationToken = default)
    {
        if (!Isbn.TryNormalize(isbn, out var isbn13))
        {
            return ServiceResult<BookDto>.Failure(ServiceErrorType.Validation, InvalidIsbnMessage);
        }

        if (await books.GetByIsbnAsync(isbn13, cancellationToken) is { } existing)
        {
            return ServiceResult<BookDto>.Success(BookDto.FromBook(existing));
        }

        var preview = await bookLookup.LookupByIsbnAsync(isbn13, cancellationToken);
        return preview is null
            ? ServiceResult<BookDto>.Failure(ServiceErrorType.NotFound, BookNotFoundMessage)
            : ServiceResult<BookDto>.Success(FromPreview(preview));
    }

    private async Task<IReadOnlyList<BookDto>> SearchExternalAsync(BookSearchCriteria criteria, int limit, CancellationToken cancellationToken)
    {
        if (criteria.Isbn is not null)
        {
            var preview = await bookLookup.LookupByIsbnAsync(criteria.Isbn, cancellationToken);
            return preview is null ? [] : [FromPreview(preview)];
        }

        // External catalogs only support free-text and author search.
        if (criteria.Query is null && criteria.Author is null)
        {
            return [];
        }

        var previews = await bookLookup.SearchAsync(criteria.Query, criteria.Author, limit, cancellationToken);
        return previews.Select(FromPreview).ToList();
    }

    private static BookDto FromPreview(BookPreview preview) => new(
        Id: null,
        preview.Title,
        preview.Author,
        preview.Isbn,
        preview.Publisher,
        preview.PublishedYear,
        preview.Description,
        preview.CoverImageUrl,
        preview.Genre,
        preview.PageCount);

    private static bool IsBlank(string? value) => string.IsNullOrWhiteSpace(value);

    private static string? TrimToNull(string? value) => IsBlank(value) ? null : value!.Trim();

    private static bool IsHttpUrl(string value) =>
        Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
}
