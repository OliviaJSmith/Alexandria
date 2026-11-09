using System.Security.Claims;
using Alexandria.Api.Controllers;
using Alexandria.Api.Dtos;
using Alexandria.Api.Models;
using Alexandria.Api.Repositories;
using Alexandria.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Moq;

namespace Alexandria.Api.Tests.Controllers;

public class FriendsControllerTests
{
    private readonly Mock<IFriendService> _service = new();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly FriendsController _controller;
    private readonly FriendshipDto _friendship = new(
        Guid.NewGuid(), new UserSummaryDto(Guid.NewGuid(), "Friend", "A Friend", null), FriendshipStatus.Pending, true, TestData.Now, TestData.Now);

    public FriendsControllerTests()
    {
        _controller = new FriendsController(_service.Object).WithContext(new Claim(JwtRegisteredClaimNames.Sub, _userId.ToString()));
    }

    [Fact]
    public async Task GetFriends_ReturnsOk()
    {
        _service.Setup(s => s.GetFriendsAsync(_userId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<IReadOnlyList<FriendshipDto>>.Success([_friendship]));

        Assert.IsType<OkObjectResult>((await _controller.GetFriends(null, CancellationToken.None)).Result);
    }

    [Fact]
    public async Task GetFriends_PassesSearchThrough()
    {
        _service.Setup(s => s.GetFriendsAsync(_userId, "jane", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<IReadOnlyList<FriendshipDto>>.Success([_friendship]));

        Assert.IsType<OkObjectResult>((await _controller.GetFriends("jane", CancellationToken.None)).Result);
    }

    [Fact]
    public async Task GetFriends_InvalidSearch_ReturnsBadRequest()
    {
        _service.Setup(s => s.GetFriendsAsync(_userId, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<IReadOnlyList<FriendshipDto>>.Failure(ServiceErrorType.Validation, "too long"));

        var result = await _controller.GetFriends(new string('a', 101), CancellationToken.None);

        ControllerTestHelpers.AssertProblem(result.Result, StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task GetRequests_ReturnsOk()
    {
        _service.Setup(s => s.GetRequestsAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<FriendRequestsDto>.Success(new FriendRequestsDto([_friendship], [])));

        Assert.IsType<OkObjectResult>((await _controller.GetRequests(CancellationToken.None)).Result);
    }

    [Fact]
    public async Task Search_TooShort_ReturnsBadRequest()
    {
        _service.Setup(s => s.SearchUsersAsync(_userId, "a", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<IReadOnlyList<UserSearchResultDto>>.Failure(ServiceErrorType.Validation, FriendService.SearchTooShortMessage));

        var result = await _controller.Search("a", CancellationToken.None);

        ControllerTestHelpers.AssertProblem(result.Result, StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task SendRequest_AlreadyFriends_ReturnsConflict()
    {
        _service.Setup(s => s.SendRequestAsync(_userId, "Friend", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<FriendshipDto>.Failure(ServiceErrorType.Conflict, FriendService.AlreadyFriendsMessage));

        var result = await _controller.SendRequest(new SendFriendRequest { UserName = "Friend" }, CancellationToken.None);

        ControllerTestHelpers.AssertProblem(result.Result, StatusCodes.Status409Conflict);
    }

    [Fact]
    public async Task AcceptAndDecline_ReturnOk()
    {
        _service.Setup(s => s.AcceptAsync(_userId, _friendship.FriendshipId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<FriendshipDto>.Success(_friendship));
        _service.Setup(s => s.DeclineAsync(_userId, _friendship.FriendshipId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<FriendshipDto>.Success(_friendship));

        Assert.IsType<OkObjectResult>((await _controller.Accept(_friendship.FriendshipId, CancellationToken.None)).Result);
        Assert.IsType<OkObjectResult>((await _controller.Decline(_friendship.FriendshipId, CancellationToken.None)).Result);
    }

    [Fact]
    public async Task Remove_Success_ReturnsNoContent_Missing_ReturnsNotFound()
    {
        var missing = Guid.NewGuid();
        _service.Setup(s => s.RemoveAsync(_userId, _friendship.FriendshipId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<bool>.Success(true));
        _service.Setup(s => s.RemoveAsync(_userId, missing, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<bool>.Failure(ServiceErrorType.NotFound, FriendService.FriendshipNotFoundMessage));

        Assert.IsType<NoContentResult>(await _controller.Remove(_friendship.FriendshipId, CancellationToken.None));
        ControllerTestHelpers.AssertProblem(await _controller.Remove(missing, CancellationToken.None), StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task GetFriendLibraries_NotFriends_ReturnsNotFound()
    {
        var friendId = Guid.NewGuid();
        _service.Setup(s => s.GetFriendLibrariesAsync(_userId, friendId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<IReadOnlyList<LibraryDto>>.Failure(ServiceErrorType.NotFound, FriendService.NotFriendsMessage));

        var result = await _controller.GetFriendLibraries(friendId, CancellationToken.None);

        ControllerTestHelpers.AssertProblem(result.Result, StatusCodes.Status404NotFound);
    }
}

public class LoansControllerTests
{
    private readonly Mock<ILoanService> _service = new();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly LoansController _controller;
    private readonly LoanDto _loan;

    public LoansControllerTests()
    {
        _controller = new LoansController(_service.Object).WithContext(new Claim(JwtRegisteredClaimNames.Sub, _userId.ToString()));
        var person = new UserSummaryDto(Guid.NewGuid(), "Someone", "Someone", null);
        _loan = new LoanDto(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Shelf", BookDto.FromBook(TestData.Book()), person, person,
            LoanStatus.Pending, false, TestData.Now, null, null, null);
    }

    [Fact]
    public async Task GetLoans_PassesFilters()
    {
        _service.Setup(s => s.GetLoansAsync(_userId, LoanRole.Borrower, LoanStatus.Active, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<IReadOnlyList<LoanDto>>.Success([_loan]));

        var result = await _controller.GetLoans(LoanRole.Borrower, LoanStatus.Active, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task LendAndRequest_ReturnCreated()
    {
        var lend = new LendBookRequest { LibraryBookId = Guid.NewGuid(), BorrowerId = Guid.NewGuid() };
        var borrow = new BorrowRequest { LibraryBookId = Guid.NewGuid() };
        _service.Setup(s => s.LendAsync(_userId, lend, It.IsAny<CancellationToken>())).ReturnsAsync(ServiceResult<LoanDto>.Success(_loan));
        _service.Setup(s => s.RequestAsync(_userId, borrow, It.IsAny<CancellationToken>())).ReturnsAsync(ServiceResult<LoanDto>.Success(_loan));

        Assert.Equal(StatusCodes.Status201Created, Assert.IsType<ObjectResult>((await _controller.Lend(lend, CancellationToken.None)).Result).StatusCode);
        Assert.Equal(StatusCodes.Status201Created, Assert.IsType<ObjectResult>((await _controller.RequestToBorrow(borrow, CancellationToken.None)).Result).StatusCode);
    }

    [Fact]
    public async Task Approve_Unavailable_ReturnsConflict()
    {
        _service.Setup(s => s.ApproveAsync(_userId, _loan.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<LoanDto>.Failure(ServiceErrorType.Conflict, LoanService.NotAvailableMessage));

        ControllerTestHelpers.AssertProblem((await _controller.Approve(_loan.Id, CancellationToken.None)).Result, StatusCodes.Status409Conflict);
    }

    [Fact]
    public async Task DeclineCancelReturn_ReturnOk()
    {
        _service.Setup(s => s.DeclineAsync(_userId, _loan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ServiceResult<LoanDto>.Success(_loan));
        _service.Setup(s => s.CancelAsync(_userId, _loan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ServiceResult<LoanDto>.Success(_loan));
        _service.Setup(s => s.ReturnAsync(_userId, _loan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ServiceResult<LoanDto>.Success(_loan));

        Assert.IsType<OkObjectResult>((await _controller.Decline(_loan.Id, CancellationToken.None)).Result);
        Assert.IsType<OkObjectResult>((await _controller.Cancel(_loan.Id, CancellationToken.None)).Result);
        Assert.IsType<OkObjectResult>((await _controller.Return(_loan.Id, CancellationToken.None)).Result);
    }
}

public class UsersControllerProfileTests
{
    private readonly Mock<IUserService> _service = new();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly UsersController _controller;

    public UsersControllerProfileTests()
    {
        _controller = new UsersController(_service.Object).WithContext(new Claim(JwtRegisteredClaimNames.Sub, _userId.ToString()));
    }

    [Fact]
    public async Task UpdateProfile_Taken_ReturnsConflict()
    {
        var request = new UpdateProfileRequest { UserName = "taken" };
        _service.Setup(s => s.UpdateProfileAsync(_userId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<UserDto>.Failure(ServiceErrorType.Conflict, UserService.UserNameTakenMessage));

        ControllerTestHelpers.AssertProblem((await _controller.UpdateProfile(request, CancellationToken.None)).Result, StatusCodes.Status409Conflict);
    }

    [Fact]
    public async Task CheckUserName_ReturnsOk()
    {
        _service.Setup(s => s.CheckUserNameAsync(_userId, "fresh", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<UserNameAvailabilityDto>.Success(new UserNameAvailabilityDto("fresh", true, true)));

        Assert.IsType<OkObjectResult>((await _controller.CheckUserName("fresh", CancellationToken.None)).Result);
    }
}
