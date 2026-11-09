using Alexandria.Api.Controllers;
using Alexandria.Api.Dtos;
using Alexandria.Api.Services;
using Alexandria.Api.Services.Scanning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Alexandria.Api.Tests.Controllers;

public class BooksControllerTests
{
    private readonly Mock<IBookService> _service = new();
    private readonly Mock<IBookScanService> _scan = new();
    private readonly BooksController _controller;
    private readonly BookDto _book = BookDto.FromBook(TestData.Book());

    public BooksControllerTests()
    {
        _controller = new BooksController(_service.Object, _scan.Object).WithContext();
    }

    [Fact]
    public async Task Search_ReturnsOk()
    {
        var query = new BookSearchQuery { Query = "hobbit" };
        _service.Setup(s => s.SearchAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<IReadOnlyList<BookDto>>.Success([_book]));

        var result = await _controller.Search(query, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task Search_InvalidCriteria_ReturnsBadRequest()
    {
        var query = new BookSearchQuery();
        _service.Setup(s => s.SearchAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<IReadOnlyList<BookDto>>.Failure(ServiceErrorType.Validation, BookService.SearchCriteriaRequiredMessage));

        var result = await _controller.Search(query, CancellationToken.None);

        ControllerTestHelpers.AssertProblem(result.Result, StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task GetBook_Missing_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _service.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<BookDto>.Failure(ServiceErrorType.NotFound, BookService.BookNotFoundMessage));

        var result = await _controller.GetBook(id, CancellationToken.None);

        ControllerTestHelpers.AssertProblem(result.Result, StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task LookupByIsbn_ReturnsOk()
    {
        _service.Setup(s => s.LookupByIsbnAsync("9780547928227", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<BookDto>.Success(_book));

        var result = await _controller.LookupByIsbn("9780547928227", CancellationToken.None);

        Assert.Same(_book, Assert.IsType<OkObjectResult>(result.Result).Value);
    }

    [Fact]
    public async Task CreateBook_New_ReturnsCreatedAtGetBook()
    {
        var request = new CreateBookRequest { Title = "The Hobbit" };
        _service.Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<CreateBookResult>.Success(new CreateBookResult(_book, Created: true)));

        var result = await _controller.CreateBook(request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(BooksController.GetBook), created.ActionName);
        Assert.Equal(_book.Id, created.RouteValues!["bookId"]);
    }

    [Fact]
    public async Task CreateBook_ExistingIsbn_ReturnsOkWithExisting()
    {
        var request = new CreateBookRequest { Title = "The Hobbit", Isbn = "9780547928227" };
        _service.Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<CreateBookResult>.Success(new CreateBookResult(_book, Created: false)));

        var result = await _controller.CreateBook(request, CancellationToken.None);

        Assert.Same(_book, Assert.IsType<OkObjectResult>(result.Result).Value);
    }

    [Fact]
    public async Task Scan_PassesImageBytesAndMode_ReturnsOk()
    {
        var bytes = new byte[] { 0xFF, 0xD8, 0xFF, 0x01 };
        var scanResult = new ScanResultDto([new ScannedBookDto("Dune", "Frank Herbert", null, _book)], null);
        _scan.Setup(s => s.ScanAsync(It.Is<byte[]>(b => b.SequenceEqual(bytes)), ScanMode.Shelf, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<ScanResultDto>.Success(scanResult));

        var result = await _controller.Scan(FormFile(bytes), ScanMode.Shelf, CancellationToken.None);

        Assert.Same(scanResult, Assert.IsType<OkObjectResult>(result.Result).Value);
    }

    [Fact]
    public async Task Scan_EmptyFile_ReturnsBadRequestWithoutScanning()
    {
        var result = await _controller.Scan(FormFile([]), ScanMode.Single, CancellationToken.None);

        ControllerTestHelpers.AssertProblem(result.Result, StatusCodes.Status400BadRequest);
        _scan.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Scan_NotConfigured_ReturnsServiceUnavailable()
    {
        _scan.Setup(s => s.ScanAsync(It.IsAny<byte[]>(), It.IsAny<ScanMode>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<ScanResultDto>.Failure(ServiceErrorType.Unavailable, BookScanService.NotConfiguredMessage));

        var result = await _controller.Scan(FormFile([1, 2, 3]), ScanMode.Single, CancellationToken.None);

        ControllerTestHelpers.AssertProblem(result.Result, StatusCodes.Status503ServiceUnavailable);
    }

    private static FormFile FormFile(byte[] bytes) => new(new MemoryStream(bytes), 0, bytes.Length, "image", "photo.jpg");

    [Fact]
    public async Task CreateBook_Invalid_ReturnsBadRequest()
    {
        var request = new CreateBookRequest { Title = "x", Isbn = "123" };
        _service.Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<CreateBookResult>.Failure(ServiceErrorType.Validation, BookService.InvalidIsbnMessage));

        var result = await _controller.CreateBook(request, CancellationToken.None);

        ControllerTestHelpers.AssertProblem(result.Result, StatusCodes.Status400BadRequest);
    }
}
