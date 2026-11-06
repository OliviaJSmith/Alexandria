using System.Security.Claims;
using Alexandria.Api.Controllers;
using Alexandria.Api.Dtos;
using Alexandria.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Moq;

namespace Alexandria.Api.Tests.Controllers;

public class UsersControllerTests
{
    private readonly Mock<IUserService> _userService = new();

    [Fact]
    public async Task GetCurrentUser_SignedIn_ReturnsProfile()
    {
        var user = UserDto.FromUser(TestData.User());
        _userService.Setup(s => s.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<UserDto>.Success(user));
        var controller = new UsersController(_userService.Object)
            .WithContext(new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()));

        var result = await controller.GetCurrentUser(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(user, ok.Value);
    }

    [Fact]
    public async Task GetCurrentUser_UserNoLongerExists_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        _userService.Setup(s => s.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<UserDto>.Failure(ServiceErrorType.NotFound, UserService.UserNotFoundMessage));
        var controller = new UsersController(_userService.Object)
            .WithContext(new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()));

        var result = await controller.GetCurrentUser(CancellationToken.None);

        ControllerTestHelpers.AssertProblem(result.Result, StatusCodes.Status404NotFound);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-guid")]
    public async Task GetCurrentUser_MissingOrMalformedSubject_ReturnsUnauthorizedWithoutCallingService(string? subject)
    {
        var claims = subject is null ? [] : new[] { new Claim(JwtRegisteredClaimNames.Sub, subject) };
        var controller = new UsersController(_userService.Object).WithContext(claims);

        var result = await controller.GetCurrentUser(CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result.Result);
        _userService.VerifyNoOtherCalls();
    }
}
