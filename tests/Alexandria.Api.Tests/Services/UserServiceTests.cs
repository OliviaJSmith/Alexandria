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
        _service = new UserService(_users.Object);
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
}
