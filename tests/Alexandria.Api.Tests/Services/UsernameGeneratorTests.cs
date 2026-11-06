using Alexandria.Api.Repositories;
using Alexandria.Api.Services;
using Moq;

namespace Alexandria.Api.Tests.Services;

public class UsernameGeneratorTests
{
    private readonly Mock<IUserRepository> _users = new();

    [Fact]
    public async Task GenerateUnique_WhenAvailable_ReturnsAdjectiveNounNumber()
    {
        _users.Setup(u => u.NormalizedUserNameExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var generator = new UsernameGenerator(_users.Object, new Random(1));

        var userName = await generator.GenerateUniqueAsync();

        Assert.Matches("^[A-Z][a-z]+[A-Z][a-z]+[1-9][0-9]{0,2}$", userName);
        _users.Verify(u => u.NormalizedUserNameExistsAsync(userName.ToUpperInvariant(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateUnique_WhenCandidateTaken_TriesAgain()
    {
        _users.SetupSequence(u => u.NormalizedUserNameExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(true)
            .ReturnsAsync(false);
        var generator = new UsernameGenerator(_users.Object, new Random(2));

        await generator.GenerateUniqueAsync();

        _users.Verify(u => u.NormalizedUserNameExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task GenerateUnique_WhenEveryCandidateTaken_FallsBackToRandomSuffix()
    {
        _users.Setup(u => u.NormalizedUserNameExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var generator = new UsernameGenerator(_users.Object, new Random(3));

        var userName = await generator.GenerateUniqueAsync();

        Assert.Equal(30, userName.Length);
        Assert.Matches("^[A-Z][a-z]+[A-Z][a-z]+[0-9a-f]{10,}$", userName);
        _users.Verify(
            u => u.NormalizedUserNameExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(UsernameGenerator.MaxAttempts));
    }

    [Fact]
    public void Normalize_UpperCasesInvariantly()
    {
        Assert.Equal("COZYQUILL42", UsernameGenerator.Normalize("CozyQuill42"));
    }
}
