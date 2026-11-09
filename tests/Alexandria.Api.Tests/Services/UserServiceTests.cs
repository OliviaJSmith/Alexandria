using Alexandria.Api.Dtos;
using Alexandria.Api.Models;
using Alexandria.Api.Repositories;
using Alexandria.Api.Services;
using Moq;

namespace Alexandria.Api.Tests.Services;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly UserService _service;

    public UserServiceTests()
    {
        _service = new UserService(_users.Object, TestData.TimeProviderAt(TestData.Now).Object);
    }

    [Fact]
    public async Task GetById_ExistingUser_ReturnsProfile()
    {
        var user = TestData.User();
        user.TermsAcceptedAt = TestData.Now;
        _users.Setup(u => u.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await _service.GetByIdAsync(user.Id);

        Assert.True(result.IsSuccess);
        var dto = result.Value!;
        Assert.Equal(user.Id, dto.Id);
        Assert.Equal(user.Email, dto.Email);
        Assert.Equal(user.DisplayName, dto.DisplayName);
        Assert.Equal(user.UserName, dto.UserName);
        Assert.Equal(user.ProfilePictureUrl, dto.ProfilePictureUrl);
        Assert.Equal(TestData.Now, dto.TermsAcceptedAt);
    }

    [Fact]
    public async Task GetById_UnknownUser_ReturnsNotFound()
    {
        _users.Setup(u => u.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var result = await _service.GetByIdAsync(Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal(ServiceErrorType.NotFound, result.Error!.Type);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task UpdateProfile_FreeUserName_SavesItAndItsNormalizedForm()
    {
        var user = SetupUser();

        var result = await _service.UpdateProfileAsync(user.Id, new UpdateProfileRequest { UserName = " Book_Worm " });

        Assert.True(result.IsSuccess);
        Assert.Equal("Book_Worm", user.UserName);
        Assert.Equal("BOOK_WORM", user.NormalizedUserName);
        Assert.Equal(TestData.Now, user.UpdatedAt);
        _users.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateProfile_ChangingOnlyCase_IsAllowedWithoutUniquenessCheck()
    {
        var user = SetupUser("quietscribe7");

        var result = await _service.UpdateProfileAsync(user.Id, new UpdateProfileRequest { UserName = "QuietScribe7" });

        Assert.True(result.IsSuccess);
        _users.Verify(u => u.NormalizedUserNameExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateProfile_TakenUserName_ReturnsConflict()
    {
        var user = SetupUser();
        _users.Setup(u => u.NormalizedUserNameExistsAsync("TAKEN", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _service.UpdateProfileAsync(user.Id, new UpdateProfileRequest { UserName = "taken" });

        Assert.Equal(ServiceErrorType.Conflict, result.Error!.Type);
        _users.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("has space")]
    [InlineData("emoji😀")]
    [InlineData("waaaaaaaaaaaaaaaaaaaaaaaaaaaytoolong")]
    public async Task UpdateProfile_InvalidUserName_ReturnsValidationError(string userName)
    {
        var user = SetupUser();

        var result = await _service.UpdateProfileAsync(user.Id, new UpdateProfileRequest { UserName = userName });

        Assert.Equal(ServiceErrorType.Validation, result.Error!.Type);
    }

    [Fact]
    public async Task UpdateProfile_UnknownUser_ReturnsNotFound()
    {
        var result = await _service.UpdateProfileAsync(Guid.NewGuid(), new UpdateProfileRequest { UserName = "valid_name" });

        Assert.Equal(ServiceErrorType.NotFound, result.Error!.Type);
    }

    [Fact]
    public async Task CheckUserName_Free_IsAvailable()
    {
        var result = await _service.CheckUserNameAsync(Guid.NewGuid(), "fresh_name");

        Assert.Equal(new UserNameAvailabilityDto("fresh_name", true, true), result.Value);
    }

    [Fact]
    public async Task CheckUserName_OwnedBySomeoneElse_IsUnavailable_OwnedBySelf_IsAvailable()
    {
        var owner = TestData.User("TakenName");
        _users.Setup(u => u.GetByNormalizedUserNameAsync("TAKENNAME", It.IsAny<CancellationToken>())).ReturnsAsync(owner);

        var someoneElse = await _service.CheckUserNameAsync(Guid.NewGuid(), "takenname");
        var self = await _service.CheckUserNameAsync(owner.Id, "takenname");

        Assert.False(someoneElse.Value!.IsAvailable);
        Assert.True(self.Value!.IsAvailable);
    }

    [Fact]
    public async Task CheckUserName_Invalid_IsNeitherValidNorAvailable()
    {
        var result = await _service.CheckUserNameAsync(Guid.NewGuid(), "x");

        Assert.False(result.Value!.IsValid);
        Assert.False(result.Value.IsAvailable);
        _users.VerifyNoOtherCalls();
    }

    private User SetupUser(string userName = "QuietScribe7")
    {
        var user = TestData.User(userName);
        _users.Setup(u => u.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        return user;
    }
}
