using Alexandria.Api.Controllers;
using Alexandria.Api.Dtos;
using Alexandria.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Alexandria.Api.Tests.Controllers;

public class BooksControllerTests
{
    private readonly Mock<IBookService> _service = new();
    private readonly BooksController _controller;
    private readonly BookDto _book = BookDto.FromBook(TestData.Book());

    public BooksControllerTests()
    {
        _controller = new BooksController(_service.Object).WithContext();
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
    public async Task CreateBook_Invalid_ReturnsBadRequest()
    {
        var request = new CreateBookRequest { Title = "x", Isbn = "123" };
        _service.Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<CreateBookResult>.Failure(ServiceErrorType.Validation, BookService.InvalidIsbnMessage));

        var result = await _controller.CreateBook(request, CancellationToken.None);

        ControllerTestHelpers.AssertProblem(result.Result, StatusCodes.Status400BadRequest);
    }
}
