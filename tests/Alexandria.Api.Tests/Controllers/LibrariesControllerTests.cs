using System.Security.Claims;
using Alexandria.Api.Controllers;
using Alexandria.Api.Dtos;
using Alexandria.Api.Models;
using Alexandria.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Moq;

namespace Alexandria.Api.Tests.Controllers;

public class LibrariesControllerTests
{
    private readonly Mock<ILibraryService> _service = new();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly LibrariesController _controller;

    private readonly LibraryDto _library;
    private readonly LibraryBookDto _copy;

    public LibrariesControllerTests()
    {
        _controller = new LibrariesController(_service.Object).WithContext(new Claim(JwtRegisteredClaimNames.Sub, _userId.ToString()));
        var library = TestData.Library(_userId);
        _library = LibraryDto.FromLibrary(library);
        _copy = LibraryBookDto.FromLibraryBook(TestData.Copy(library, TestData.Book()));
    }

    [Fact]
    public async Task GetMyLibraries_ReturnsOk()
    {
        _service.Setup(s => s.GetMyLibrariesAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<IReadOnlyList<LibraryDto>>.Success([_library]));

        var result = await _controller.GetMyLibraries(CancellationToken.None);

        Assert.Equal([_library], Assert.IsAssignableFrom<IReadOnlyList<LibraryDto>>(Assert.IsType<OkObjectResult>(result.Result).Value));
    }

    [Fact]
    public async Task GetLentOutBooks_ReturnsOk()
    {
        _service.Setup(s => s.GetLentOutBooksAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<IReadOnlyList<LibraryBookDto>>.Success([_copy]));

        var result = await _controller.GetLentOutBooks(CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetLibrary_NotVisible_ReturnsNotFound()
    {
        _service.Setup(s => s.GetLibraryAsync(_library.Id, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<LibraryDto>.Failure(ServiceErrorType.NotFound, LibraryService.LibraryNotFoundMessage));

        var result = await _controller.GetLibrary(_library.Id, CancellationToken.None);

        ControllerTestHelpers.AssertProblem(result.Result, StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task CreateLibrary_ReturnsCreatedAtGetLibrary()
    {
        var request = new CreateLibraryRequest { Name = "Study" };
        _service.Setup(s => s.CreateLibraryAsync(_userId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<LibraryDto>.Success(_library));

        var result = await _controller.CreateLibrary(request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(LibrariesController.GetLibrary), created.ActionName);
        Assert.Equal(_library.Id, created.RouteValues!["libraryId"]);
        Assert.Same(_library, created.Value);
    }

    [Fact]
    public async Task UpdateLibrary_NotOwner_ReturnsForbidden()
    {
        var request = new UpdateLibraryRequest { IsPublic = true };
        _service.Setup(s => s.UpdateLibraryAsync(_library.Id, _userId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<LibraryDto>.Failure(ServiceErrorType.Forbidden, LibraryService.NotOwnerMessage));

        var result = await _controller.UpdateLibrary(_library.Id, request, CancellationToken.None);

        ControllerTestHelpers.AssertProblem(result.Result, StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task GetLibraryBooks_ReturnsOk()
    {
        _service.Setup(s => s.GetLibraryBooksAsync(_library.Id, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<IReadOnlyList<LibraryBookDto>>.Success([_copy]));

        var result = await _controller.GetLibraryBooks(_library.Id, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task AddBook_ReturnsCreated()
    {
        var request = new AddLibraryBookRequest { BookId = _copy.Book.Id!.Value };
        _service.Setup(s => s.AddBookAsync(_library.Id, _userId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<LibraryBookDto>.Success(_copy));

        var result = await _controller.AddBook(_library.Id, request, CancellationToken.None);

        var created = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        Assert.Same(_copy, created.Value);
    }

    [Fact]
    public async Task AddBook_Duplicate_ReturnsConflict()
    {
        var request = new AddLibraryBookRequest { BookId = Guid.NewGuid() };
        _service.Setup(s => s.AddBookAsync(_library.Id, _userId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<LibraryBookDto>.Failure(ServiceErrorType.Conflict, LibraryService.DuplicateBookMessage));

        var result = await _controller.AddBook(_library.Id, request, CancellationToken.None);

        var problem = ControllerTestHelpers.AssertProblem(result.Result, StatusCodes.Status409Conflict);
        Assert.Equal(LibraryService.DuplicateBookMessage, problem.Detail);
    }

    [Fact]
    public async Task UpdateLibraryBook_ReturnsOk()
    {
        var request = new UpdateLibraryBookRequest { Status = LibraryBookStatus.CheckedOut };
        _service.Setup(s => s.UpdateLibraryBookAsync(_library.Id, _copy.Id, _userId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<LibraryBookDto>.Success(_copy));

        var result = await _controller.UpdateLibraryBook(_library.Id, _copy.Id, request, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task RemoveLibraryBook_Success_ReturnsNoContent()
    {
        _service.Setup(s => s.RemoveLibraryBookAsync(_library.Id, _copy.Id, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<bool>.Success(true));

        var result = await _controller.RemoveLibraryBook(_library.Id, _copy.Id, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task RemoveLibraryBook_LentOut_ReturnsConflict()
    {
        _service.Setup(s => s.RemoveLibraryBookAsync(_library.Id, _copy.Id, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<bool>.Failure(ServiceErrorType.Conflict, LibraryService.BookLentOutMessage));

        var result = await _controller.RemoveLibraryBook(_library.Id, _copy.Id, CancellationToken.None);

        ControllerTestHelpers.AssertProblem(result, StatusCodes.Status409Conflict);
    }

    [Fact]
    public async Task MoveLibraryBook_ReturnsOk()
    {
        var request = new MoveLibraryBookRequest { TargetLibraryId = Guid.NewGuid() };
        _service.Setup(s => s.MoveLibraryBookAsync(_library.Id, _copy.Id, _userId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<LibraryBookDto>.Success(_copy));

        var result = await _controller.MoveLibraryBook(_library.Id, _copy.Id, request, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task AnyAction_WithoutValidUserId_ReturnsUnauthorizedWithoutCallingService()
    {
        var controller = new LibrariesController(_service.Object).WithContext();

        Assert.IsType<UnauthorizedResult>((await controller.GetMyLibraries(CancellationToken.None)).Result);
        Assert.IsType<UnauthorizedResult>(await controller.RemoveLibraryBook(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None));
        _service.VerifyNoOtherCalls();
    }
}
