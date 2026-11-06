using Alexandria.Api.Controllers;
using Alexandria.Api.Dtos;
using Alexandria.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Alexandria.Api.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authService = new();
    private readonly AuthController _controller;
    private readonly GoogleSignInRequest _request = new() { AccessToken = "google-access-token", AcceptedTerms = true };

    public AuthControllerTests()
    {
        _controller = new AuthController(_authService.Object).WithContext();
    }

    [Fact]
    public async Task SignInWithGoogle_Success_ReturnsOkWithTokenAndUser()
    {
        var response = new AuthResponse("jwt", TestData.Now.AddDays(7), UserDto.FromUser(TestData.User()));
        _authService.Setup(s => s.SignInWithGoogleAsync(_request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<AuthResponse>.Success(response));

        var result = await _controller.SignInWithGoogle(_request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(response, ok.Value);
    }

    [Theory]
    [InlineData(ServiceErrorType.Validation, StatusCodes.Status400BadRequest)]
    [InlineData(ServiceErrorType.Unauthorized, StatusCodes.Status401Unauthorized)]
    [InlineData(ServiceErrorType.Forbidden, StatusCodes.Status403Forbidden)]
    [InlineData(ServiceErrorType.NotFound, StatusCodes.Status404NotFound)]
    [InlineData(ServiceErrorType.Conflict, StatusCodes.Status409Conflict)]
    public async Task SignInWithGoogle_Failure_MapsErrorTypeToProblemStatus(ServiceErrorType errorType, int expectedStatus)
    {
        _authService.Setup(s => s.SignInWithGoogleAsync(_request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<AuthResponse>.Failure(errorType, "Something went wrong."));

        var result = await _controller.SignInWithGoogle(_request, CancellationToken.None);

        var problem = ControllerTestHelpers.AssertProblem(result.Result, expectedStatus);
        Assert.Equal("Something went wrong.", problem.Detail);
    }
}
