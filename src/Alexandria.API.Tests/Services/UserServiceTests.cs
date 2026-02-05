using Alexandria.API.DTOs;
using Alexandria.API.Models;
using Alexandria.API.Services;
using Alexandria.API.Utilities;

namespace Alexandria.API.Tests.Services;

public class UserServiceTests : ServiceTestBase
{
    private readonly UserService _sut;

    public UserServiceTests()
    {
        var logger = CreateMockLogger<UserService>();
        _sut = new UserService(Context, logger.Object);
    }

    #region GetUserByIdAsync Tests

    [Fact]
    public async Task GetUserByIdAsync_WithValidId_ReturnsUser()
    {
        // Arrange
        var user = await SeedUserAsync();

        // Act
        var result = await _sut.GetUserByIdAsync(user.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Email, result.Email);
        Assert.Equal(user.Name, result.Name);
    }

    [Fact]
    public async Task GetUserByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Act
        var result = await _sut.GetUserByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetUserByGoogleIdAsync Tests

    [Fact]
    public async Task GetUserByGoogleIdAsync_WithValidGoogleId_ReturnsUser()
    {
        // Arrange
        var user = await SeedUserAsync();

        // Act
        var result = await _sut.GetUserByGoogleIdAsync(user.GoogleId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Email, result.Email);
    }

    [Fact]
    public async Task GetUserByGoogleIdAsync_WithInvalidGoogleId_ReturnsNull()
    {
        // Act
        var result = await _sut.GetUserByGoogleIdAsync("nonexistent-google-id");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region CreateUserAsync Tests

    [Fact]
    public async Task CreateUserAsync_WithValidData_CreatesUser()
    {
        // Arrange
        var createDto = new CreateUserDto
        {
            GoogleId = "google-new",
            Email = "newuser@test.com",
            Name = "New User",
            UserName = "newusername"
        };

        // Act
        var result = await _sut.CreateUserAsync(createDto);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(createDto.Email, result.Data.Email);
        Assert.Equal(createDto.Name, result.Data.Name);
        Assert.Equal("newusername", result.Data.UserName);
    }

    [Fact]
    public async Task CreateUserAsync_WithDuplicateGoogleId_ReturnsFailure()
    {
        // Arrange
        var existingUser = await SeedUserAsync();
        var createDto = new CreateUserDto
        {
            GoogleId = existingUser.GoogleId,
            Email = "different@test.com",
            Name = "Different User"
        };

        // Act
        var result = await _sut.CreateUserAsync(createDto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Google ID already exists", result.Error);
    }

    [Fact]
    public async Task CreateUserAsync_WithDuplicateEmail_ReturnsFailure()
    {
        // Arrange
        var existingUser = await SeedUserAsync();
        var createDto = new CreateUserDto
        {
            GoogleId = "different-google-id",
            Email = existingUser.Email,
            Name = "Different User"
        };

        // Act
        var result = await _sut.CreateUserAsync(createDto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Email is already in use", result.Error);
    }

    [Fact]
    public async Task CreateUserAsync_WithTakenUserName_ReturnsFailure()
    {
        // Arrange
        await SeedUserWithUserNameAsync(1, "TakenUsername");
        var createDto = new CreateUserDto
        {
            GoogleId = "google-new",
            Email = "newuser@test.com",
            Name = "New User",
            UserName = "TakenUsername"
        };

        // Act
        var result = await _sut.CreateUserAsync(createDto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Username is already taken", result.Error);
    }

    [Fact]
    public async Task CreateUserAsync_WithTakenUserName_CaseInsensitive_ReturnsFailure()
    {
        // Arrange
        await SeedUserWithUserNameAsync(1, "TakenUsername");
        var createDto = new CreateUserDto
        {
            GoogleId = "google-new",
            Email = "newuser@test.com",
            Name = "New User",
            UserName = "takenusername" // lowercase version
        };

        // Act
        var result = await _sut.CreateUserAsync(createDto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Username is already taken", result.Error);
    }

    #endregion

    #region UpdateUserAsync Username Validation Tests

    [Fact]
    public async Task UpdateUserAsync_WithNewUserName_UpdatesUserName()
    {
        // Arrange
        var user = await SeedUserAsync();
        var updateDto = new UpdateUserDto { UserName = "NewUserName123" };

        // Act
        var result = await _sut.UpdateUserAsync(user.Id, updateDto);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("NewUserName123", result.Data!.UserName);
    }

    [Fact]
    public async Task UpdateUserAsync_WithTakenUserName_ReturnsFailure()
    {
        // Arrange
        var user1 = await SeedUserWithUserNameAsync(1, "ExistingUser");
        var user2 = await SeedUserAsync(2);
        var updateDto = new UpdateUserDto { UserName = "ExistingUser" };

        // Act
        var result = await _sut.UpdateUserAsync(user2.Id, updateDto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Username is already taken", result.Error);
    }

    [Fact]
    public async Task UpdateUserAsync_WithOwnUserName_Succeeds()
    {
        // Arrange - user keeping their own username should succeed
        var user = await SeedUserWithUserNameAsync(1, "MyUserName");
        var updateDto = new UpdateUserDto { UserName = "MyUserName" };

        // Act
        var result = await _sut.UpdateUserAsync(user.Id, updateDto);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("MyUserName", result.Data!.UserName);
    }

    [Fact]
    public async Task UpdateUserAsync_WithTakenUserName_CaseInsensitive_ReturnsFailure()
    {
        // Arrange
        await SeedUserWithUserNameAsync(1, "CoolReader");
        var user2 = await SeedUserAsync(2);
        var updateDto = new UpdateUserDto { UserName = "coolreader" }; // lowercase

        // Act
        var result = await _sut.UpdateUserAsync(user2.Id, updateDto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Username is already taken", result.Error);
    }

    [Fact]
    public async Task UpdateUserAsync_WithTakenUserName_MixedCase_ReturnsFailure()
    {
        // Arrange
        await SeedUserWithUserNameAsync(1, "BookWorm");
        var user2 = await SeedUserAsync(2);
        var updateDto = new UpdateUserDto { UserName = "BOOKWORM" }; // all caps

        // Act
        var result = await _sut.UpdateUserAsync(user2.Id, updateDto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Username is already taken", result.Error);
    }

    #endregion

    #region UpdateUserAsync Username Clearing Tests

    [Fact]
    public async Task UpdateUserAsync_WithEmptyUserName_ClearsUserName()
    {
        // Arrange
        var user = await SeedUserWithUserNameAsync(1, "ExistingUserName");
        var updateDto = new UpdateUserDto { UserName = "" };

        // Act
        var result = await _sut.UpdateUserAsync(user.Id, updateDto);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(result.Data!.UserName);
    }

    [Fact]
    public async Task UpdateUserAsync_WithWhitespaceUserName_ClearsUserName()
    {
        // Arrange
        var user = await SeedUserWithUserNameAsync(1, "ExistingUserName");
        var updateDto = new UpdateUserDto { UserName = "   " };

        // Act
        var result = await _sut.UpdateUserAsync(user.Id, updateDto);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(result.Data!.UserName);
    }

    [Fact]
    public async Task UpdateUserAsync_WithNullUserName_DoesNotChangeUserName()
    {
        // Arrange - null means "don't update this field"
        var user = await SeedUserWithUserNameAsync(1, "KeepThisUserName");
        var updateDto = new UpdateUserDto { UserName = null, Name = "Updated Name" };

        // Act
        var result = await _sut.UpdateUserAsync(user.Id, updateDto);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("KeepThisUserName", result.Data!.UserName);
        Assert.Equal("Updated Name", result.Data.Name);
    }

    [Fact]
    public async Task UpdateUserAsync_TrimsWhitespaceFromUserName()
    {
        // Arrange
        var user = await SeedUserAsync();
        var updateDto = new UpdateUserDto { UserName = "  TrimmedName  " };

        // Act
        var result = await _sut.UpdateUserAsync(user.Id, updateDto);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("TrimmedName", result.Data!.UserName);
    }

    #endregion

    #region UpdateUserAsync NonExistent User Tests

    [Fact]
    public async Task UpdateUserAsync_WithNonExistentUser_ReturnsFailure()
    {
        // Arrange
        var updateDto = new UpdateUserDto { UserName = "SomeUserName" };

        // Act
        var result = await _sut.UpdateUserAsync(999, updateDto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("User not found", result.Error);
    }

    #endregion

    #region IsUserNameAvailableAsync Tests

    [Fact]
    public async Task IsUserNameAvailableAsync_WithAvailableUserName_ReturnsTrue()
    {
        // Act
        var result = await _sut.IsUserNameAvailableAsync("AvailableUserName");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsUserNameAvailableAsync_WithTakenUserName_ReturnsFalse()
    {
        // Arrange
        await SeedUserWithUserNameAsync(1, "TakenUserName");

        // Act
        var result = await _sut.IsUserNameAvailableAsync("TakenUserName");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsUserNameAvailableAsync_CaseInsensitive_Lowercase_ReturnsFalse()
    {
        // Arrange
        await SeedUserWithUserNameAsync(1, "CamelCaseUser");

        // Act
        var result = await _sut.IsUserNameAvailableAsync("camelcaseuser");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsUserNameAvailableAsync_CaseInsensitive_Uppercase_ReturnsFalse()
    {
        // Arrange
        await SeedUserWithUserNameAsync(1, "CamelCaseUser");

        // Act
        var result = await _sut.IsUserNameAvailableAsync("CAMELCASEUSER");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsUserNameAvailableAsync_CaseInsensitive_MixedCase_ReturnsFalse()
    {
        // Arrange
        await SeedUserWithUserNameAsync(1, "BookLover");

        // Act
        var result = await _sut.IsUserNameAvailableAsync("bOoKlOvEr");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsUserNameAvailableAsync_WithExcludedUserId_ReturnsTrue()
    {
        // Arrange - user checking their own username
        var user = await SeedUserWithUserNameAsync(1, "MyUserName");

        // Act
        var result = await _sut.IsUserNameAvailableAsync("MyUserName", user.Id);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsUserNameAvailableAsync_WithExcludedUserId_OtherUserHasName_ReturnsFalse()
    {
        // Arrange
        await SeedUserWithUserNameAsync(1, "TakenByOther");
        var user2 = await SeedUserAsync(2);

        // Act
        var result = await _sut.IsUserNameAvailableAsync("TakenByOther", user2.Id);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsUserNameAvailableAsync_TrimsInput()
    {
        // Arrange
        await SeedUserWithUserNameAsync(1, "NoSpaces");

        // Act
        var result = await _sut.IsUserNameAvailableAsync("  NoSpaces  ");

        // Assert
        Assert.False(result);
    }

    #endregion

    #region UsernameGenerator Tests

    [Fact]
    public void UsernameGenerator_Generate_ReturnsNonEmptyString()
    {
        // Act
        var username = UsernameGenerator.Generate();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(username));
    }

    [Fact]
    public void UsernameGenerator_Generate_ReturnsStringWithNumber()
    {
        // Act
        var username = UsernameGenerator.Generate();

        // Assert
        Assert.Matches(@"\d+$", username); // ends with digits
    }

    [Fact]
    public async Task UsernameGenerator_GenerateUniqueAsync_ReturnsUniqueUsername()
    {
        // Arrange
        var takenUsernames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Act
        var username = await UsernameGenerator.GenerateUniqueAsync(
            name => Task.FromResult(takenUsernames.Contains(name)));

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(username));
        Assert.DoesNotContain(username, takenUsernames);
    }

    [Fact]
    public async Task UsernameGenerator_GenerateUniqueAsync_RetriesOnCollision()
    {
        // Arrange
        var attemptCount = 0;
        var collisionCount = 3;
        
        Task<bool> IsUserNameTaken(string name)
        {
            attemptCount++;
            // Simulate collisions for first few attempts
            return Task.FromResult(attemptCount <= collisionCount);
        }

        // Act
        var username = await UsernameGenerator.GenerateUniqueAsync(IsUserNameTaken);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(username));
        Assert.True(attemptCount > collisionCount); // Should have retried
    }

    [Fact]
    public async Task UsernameGenerator_GenerateUniqueAsync_AddsExtraRandomnessAfterMaxAttempts()
    {
        // Arrange - always return taken to force fallback
        var attemptCount = 0;
        
        Task<bool> AlwaysTaken(string name)
        {
            attemptCount++;
            return Task.FromResult(true);
        }

        // Act
        var username = await UsernameGenerator.GenerateUniqueAsync(AlwaysTaken, maxAttempts: 5);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(username));
        Assert.Equal(5, attemptCount); // Should have tried maxAttempts times
        // The fallback username should have extra digits (1000-9999 range appended)
        Assert.Matches(@"\d{4,}$", username);
    }

    [Fact]
    public void UsernameGenerator_Generate_ProducesVariedResults()
    {
        // Act - generate multiple usernames
        var usernames = Enumerable.Range(0, 100)
            .Select(_ => UsernameGenerator.Generate())
            .ToHashSet();

        // Assert - should have significant variety (not all the same)
        Assert.True(usernames.Count > 50, "Expected variety in generated usernames");
    }

    #endregion

    #region Helper Methods

    private async Task<User> SeedUserWithUserNameAsync(int id, string userName)
    {
        var user = new User
        {
            Id = id,
            GoogleId = $"google-{id}",
            Email = $"user{id}@test.com",
            Name = $"Test User {id}",
            UserName = userName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        Context.Users.Add(user);
        await Context.SaveChangesAsync();
        return user;
    }

    #endregion
}
