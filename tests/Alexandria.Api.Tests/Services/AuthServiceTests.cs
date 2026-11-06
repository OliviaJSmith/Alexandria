using Alexandria.Api.Dtos;
using Alexandria.Api.Models;
using Alexandria.Api.Repositories;
using Alexandria.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Alexandria.Api.Tests.Services;

public class AuthServiceTests
{
    private const string AccessToken = "google-access-token";

    private readonly Mock<IGoogleTokenValidator> _googleTokenValidator = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IUsernameGenerator> _usernameGenerator = new();
    private readonly Mock<IJwtTokenService> _jwtTokenService = new();
    private readonly AuthService _service;

    private readonly GoogleUser _googleUser = new("google-sub-1", "new@example.com", "New Name", "https://example.com/new.png");
    private readonly IssuedToken _issuedToken = new("jwt-token", TestData.Now.AddDays(7));

    public AuthServiceTests()
    {
        _jwtTokenService.Setup(j => j.CreateToken(It.IsAny<User>())).Returns(_issuedToken);
        _service = new AuthService(
            _googleTokenValidator.Object,
            _users.Object,
            _usernameGenerator.Object,
            _jwtTokenService.Object,
            TestData.TimeProviderAt(TestData.Now).Object,
            NullLogger<AuthService>.Instance);
    }

    [Fact]
    public async Task SignIn_WithoutAcceptingTerms_ReturnsValidationErrorWithoutCallingGoogle()
    {
        var result = await _service.SignInWithGoogleAsync(Request(acceptedTerms: false));

        Assert.False(result.IsSuccess);
        Assert.Equal(ServiceErrorType.Validation, result.Error!.Type);
        Assert.Equal(AuthService.TermsNotAcceptedMessage, result.Error.Message);
        _googleTokenValidator.VerifyNoOtherCalls();
        _users.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SignIn_WithInvalidGoogleToken_ReturnsUnauthorizedAndSavesNothing()
    {
        _googleTokenValidator
            .Setup(v => v.ValidateAccessTokenAsync(AccessToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GoogleUser?)null);

        var result = await _service.SignInWithGoogleAsync(Request());

        Assert.Equal(ServiceErrorType.Unauthorized, result.Error!.Type);
        _users.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _jwtTokenService.Verify(j => j.CreateToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task SignIn_FirstTime_CreatesUserWithGeneratedUsernameAndRecordsTerms()
    {
        SetupValidGoogleToken();
        _users.Setup(u => u.GetByGoogleSubjectIdAsync(_googleUser.SubjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _usernameGenerator.Setup(g => g.GenerateUniqueAsync(It.IsAny<CancellationToken>())).ReturnsAsync("CozyQuill42");
        User? added = null;
        _users.Setup(u => u.Add(It.IsAny<User>())).Callback<User>(u => added = u);

        var result = await _service.SignInWithGoogleAsync(Request());

        Assert.True(result.IsSuccess);
        Assert.NotNull(added);
        Assert.NotEqual(Guid.Empty, added.Id);
        Assert.Equal(_googleUser.SubjectId, added.GoogleSubjectId);
        Assert.Equal(_googleUser.Email, added.Email);
        Assert.Equal(_googleUser.Name, added.DisplayName);
        Assert.Equal(_googleUser.PictureUrl, added.ProfilePictureUrl);
        Assert.Equal("CozyQuill42", added.UserName);
        Assert.Equal("COZYQUILL42", added.NormalizedUserName);
        Assert.Equal(TestData.Now, added.TermsAcceptedAt);
        Assert.Equal(TestData.Now, added.CreatedAt);
        _users.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        var response = result.Value!;
        Assert.Equal(_issuedToken.Token, response.Token);
        Assert.Equal(_issuedToken.ExpiresAt, response.ExpiresAt);
        Assert.Equal(added.Id, response.User.Id);
        Assert.Equal("CozyQuill42", response.User.UserName);
    }

    [Fact]
    public async Task SignIn_ReturningUser_SyncsGoogleProfileKeepsUsernameAndRecordsTerms()
    {
        SetupValidGoogleToken();
        var existing = TestData.User("QuietScribe7");
        _users.Setup(u => u.GetByGoogleSubjectIdAsync(_googleUser.SubjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await _service.SignInWithGoogleAsync(Request());

        Assert.True(result.IsSuccess);
        Assert.Equal(_googleUser.Email, existing.Email);
        Assert.Equal(_googleUser.Name, existing.DisplayName);
        Assert.Equal(_googleUser.PictureUrl, existing.ProfilePictureUrl);
        Assert.Equal("QuietScribe7", existing.UserName);
        Assert.Equal(TestData.Now, existing.TermsAcceptedAt);
        Assert.Equal(TestData.Now, existing.UpdatedAt);
        _users.Verify(u => u.Add(It.IsAny<User>()), Times.Never);
        _usernameGenerator.VerifyNoOtherCalls();
        _users.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _jwtTokenService.Verify(j => j.CreateToken(existing), Times.Once);
    }

    private void SetupValidGoogleToken() =>
        _googleTokenValidator
            .Setup(v => v.ValidateAccessTokenAsync(AccessToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_googleUser);

    private static GoogleSignInRequest Request(bool acceptedTerms = true) =>
        new() { AccessToken = AccessToken, AcceptedTerms = acceptedTerms };
}
