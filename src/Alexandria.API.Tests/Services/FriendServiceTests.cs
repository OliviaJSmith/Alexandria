using Alexandria.API.Models;
using Alexandria.API.Services;

namespace Alexandria.API.Tests.Services;

public class FriendServiceTests : ServiceTestBase
{
    private readonly FriendService _sut;

    public FriendServiceTests()
    {
        var logger = CreateMockLogger<FriendService>();
        _sut = new FriendService(Context, logger.Object);
    }

    private async Task<Friendship> SeedFriendshipAsync(int requesterId, int addresseeId, FriendshipStatus status)
    {
        var friendship = new Friendship
        {
            RequesterId = requesterId,
            AddresseeId = addresseeId,
            Status = status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        Context.Friendships.Add(friendship);
        await Context.SaveChangesAsync();
        return friendship;
    }

    [Fact]
    public async Task GetFriendsAsync_ReturnsAcceptedFriendships()
    {
        // Arrange
        var user1 = await SeedUserAsync(1);
        var user2 = await SeedUserAsync(2);
        var user3 = await SeedUserAsync(3);
        await SeedFriendshipAsync(user1.Id, user2.Id, FriendshipStatus.Accepted);
        await SeedFriendshipAsync(user3.Id, user1.Id, FriendshipStatus.Pending);

        // Act
        var result = await _sut.GetFriendsAsync(user1.Id);

        // Assert
        var friends = result.ToList();
        Assert.Single(friends);
        Assert.Equal(user2.Email, friends[0].Friend.Email);
    }

    [Fact]
    public async Task GetFriendsAsync_ReturnsFriendsWhereUserIsRequester()
    {
        // Arrange
        var user1 = await SeedUserAsync(1);
        var user2 = await SeedUserAsync(2);
        await SeedFriendshipAsync(user1.Id, user2.Id, FriendshipStatus.Accepted);

        // Act
        var result = await _sut.GetFriendsAsync(user1.Id);

        // Assert
        var friends = result.ToList();
        Assert.Single(friends);
        Assert.Equal(user2.Id, friends[0].Friend.Id);
    }

    [Fact]
    public async Task GetFriendsAsync_ReturnsFriendsWhereUserIsAddressee()
    {
        // Arrange
        var user1 = await SeedUserAsync(1);
        var user2 = await SeedUserAsync(2);
        await SeedFriendshipAsync(user1.Id, user2.Id, FriendshipStatus.Accepted);

        // Act
        var result = await _sut.GetFriendsAsync(user2.Id);

        // Assert
        var friends = result.ToList();
        Assert.Single(friends);
        Assert.Equal(user1.Id, friends[0].Friend.Id);
    }

    [Fact]
    public async Task SendFriendRequestAsync_CreatesRequest()
    {
        // Arrange
        var user1 = await SeedUserAsync(1);
        var user2 = await SeedUserAsync(2);

        // Act
        var result = await _sut.SendFriendRequestAsync(user1.Id, user2.Id);

        // Assert
        Assert.True(result.IsSuccess);

        var friendship = Context.Friendships.FirstOrDefault(f =>
            f.RequesterId == user1.Id && f.AddresseeId == user2.Id);
        Assert.NotNull(friendship);
        Assert.Equal(FriendshipStatus.Pending, friendship.Status);
    }

    [Fact]
    public async Task SendFriendRequestAsync_ToSelf_ReturnsError()
    {
        // Arrange
        var user = await SeedUserAsync(1);

        // Act
        var result = await _sut.SendFriendRequestAsync(user.Id, user.Id);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Cannot send friend request to yourself", result.Error);
    }

    [Fact]
    public async Task SendFriendRequestAsync_ToNonExistentUser_ReturnsError()
    {
        // Arrange
        var user = await SeedUserAsync(1);

        // Act
        var result = await _sut.SendFriendRequestAsync(user.Id, 999);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("User not found", result.Error);
    }

    [Fact]
    public async Task SendFriendRequestAsync_WhenAlreadyExists_ReturnsError()
    {
        // Arrange
        var user1 = await SeedUserAsync(1);
        var user2 = await SeedUserAsync(2);
        await SeedFriendshipAsync(user1.Id, user2.Id, FriendshipStatus.Pending);

        // Act
        var result = await _sut.SendFriendRequestAsync(user1.Id, user2.Id);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Friendship request already exists", result.Error);
    }

    [Fact]
    public async Task AcceptFriendRequestAsync_AcceptsRequest()
    {
        // Arrange
        var user1 = await SeedUserAsync(1);
        var user2 = await SeedUserAsync(2);
        var friendship = await SeedFriendshipAsync(user1.Id, user2.Id, FriendshipStatus.Pending);

        // Act
        var result = await _sut.AcceptFriendRequestAsync(user2.Id, friendship.Id);

        // Assert
        Assert.True(result.IsSuccess);

        var updated = await Context.Friendships.FindAsync(friendship.Id);
        Assert.Equal(FriendshipStatus.Accepted, updated!.Status);
    }

    [Fact]
    public async Task AcceptFriendRequestAsync_ByNonAddressee_ReturnsForbidden()
    {
        // Arrange
        var user1 = await SeedUserAsync(1);
        var user2 = await SeedUserAsync(2);
        var friendship = await SeedFriendshipAsync(user1.Id, user2.Id, FriendshipStatus.Pending);

        // Act - user1 tries to accept (but they're the requester, not addressee)
        var result = await _sut.AcceptFriendRequestAsync(user1.Id, friendship.Id);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Forbidden", result.Error);
    }

    [Fact]
    public async Task AcceptFriendRequestAsync_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var user = await SeedUserAsync(1);

        // Act
        var result = await _sut.AcceptFriendRequestAsync(user.Id, 999);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Not found", result.Error);
    }

    [Fact]
    public async Task RemoveFriendAsync_RemovesFriendship()
    {
        // Arrange
        var user1 = await SeedUserAsync(1);
        var user2 = await SeedUserAsync(2);
        var friendship = await SeedFriendshipAsync(user1.Id, user2.Id, FriendshipStatus.Accepted);

        // Act
        var result = await _sut.RemoveFriendAsync(user1.Id, friendship.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(await Context.Friendships.FindAsync(friendship.Id));
    }

    [Fact]
    public async Task RemoveFriendAsync_ByAddressee_RemovesFriendship()
    {
        // Arrange
        var user1 = await SeedUserAsync(1);
        var user2 = await SeedUserAsync(2);
        var friendship = await SeedFriendshipAsync(user1.Id, user2.Id, FriendshipStatus.Accepted);

        // Act
        var result = await _sut.RemoveFriendAsync(user2.Id, friendship.Id);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task RemoveFriendAsync_ByUnrelatedUser_ReturnsForbidden()
    {
        // Arrange
        var user1 = await SeedUserAsync(1);
        var user2 = await SeedUserAsync(2);
        var user3 = await SeedUserAsync(3);
        var friendship = await SeedFriendshipAsync(user1.Id, user2.Id, FriendshipStatus.Accepted);

        // Act
        var result = await _sut.RemoveFriendAsync(user3.Id, friendship.Id);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Forbidden", result.Error);
    }

    [Fact]
    public async Task RemoveFriendAsync_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var user = await SeedUserAsync(1);

        // Act
        var result = await _sut.RemoveFriendAsync(user.Id, 999);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Not found", result.Error);
    }
}
