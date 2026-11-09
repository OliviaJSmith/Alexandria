using Alexandria.Api.Dtos;
using Alexandria.Api.Models;
using Alexandria.Api.Repositories;
using Alexandria.Api.Services;
using Moq;

namespace Alexandria.Api.Tests.Services;

public class FriendServiceTests
{
    private readonly Mock<IFriendshipRepository> _friendships = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ILibraryRepository> _libraries = new();
    private readonly FriendService _service;

    private readonly User _me = TestData.User("Me_Reader");
    private readonly User _them = TestData.User("Them_Reader");

    public FriendServiceTests()
    {
        _users.Setup(u => u.GetByIdAsync(_me.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_me);
        _users.Setup(u => u.GetByNormalizedUserNameAsync("THEM_READER", It.IsAny<CancellationToken>())).ReturnsAsync(_them);
        _users.Setup(u => u.GetByNormalizedUserNameAsync("ME_READER", It.IsAny<CancellationToken>())).ReturnsAsync(_me);
        _friendships.Setup(f => f.GetBetweenManyAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _service = new FriendService(_friendships.Object, _users.Object, _libraries.Object, TestData.TimeProviderAt(TestData.Now).Object);
    }

    // ---------- Lists ----------

    [Fact]
    public async Task GetFriends_ReturnsOtherSideOfEachFriendship_SortedByUserName()
    {
        var zed = TestData.User("zed");
        var amy = TestData.User("Amy");
        _friendships.Setup(f => f.GetForUserAsync(_me.Id, FriendshipStatus.Accepted, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Friendship(_me, zed, FriendshipStatus.Accepted), Friendship(amy, _me, FriendshipStatus.Accepted)]);

        var result = await _service.GetFriendsAsync(_me.Id);

        Assert.Equal(["Amy", "zed"], result.Value!.Select(f => f.User.UserName));
        Assert.True(result.Value![0].IsIncoming);
        Assert.False(result.Value![1].IsIncoming);
    }

    [Theory]
    [InlineData("jane")]      // part of the real name, any case
    [InlineData("SMITH")]
    [InlineData("@book")]     // username, with a leading @
    [InlineData("  worm_9 ")] // surrounding spaces are ignored
    public async Task GetFriends_WithSearch_MatchesUserNameOrRealName(string search)
    {
        var jane = TestData.User("BookWorm_9");
        jane.DisplayName = "Jane Smith";
        var other = TestData.User("Other_Reader");
        other.DisplayName = "Pat Jones";
        _friendships.Setup(f => f.GetForUserAsync(_me.Id, FriendshipStatus.Accepted, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Friendship(_me, jane, FriendshipStatus.Accepted), Friendship(other, _me, FriendshipStatus.Accepted)]);

        var result = await _service.GetFriendsAsync(_me.Id, search);

        Assert.Equal("BookWorm_9", Assert.Single(result.Value!).User.UserName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetFriends_BlankSearch_ReturnsEveryone(string? search)
    {
        _friendships.Setup(f => f.GetForUserAsync(_me.Id, FriendshipStatus.Accepted, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Friendship(_me, TestData.User("One"), FriendshipStatus.Accepted), Friendship(TestData.User("Two"), _me, FriendshipStatus.Accepted)]);

        var result = await _service.GetFriendsAsync(_me.Id, search);

        Assert.Equal(2, result.Value!.Count);
    }

    [Fact]
    public async Task GetFriends_NoMatch_ReturnsEmpty()
    {
        _friendships.Setup(f => f.GetForUserAsync(_me.Id, FriendshipStatus.Accepted, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Friendship(_me, _them, FriendshipStatus.Accepted)]);

        var result = await _service.GetFriendsAsync(_me.Id, "nobody");

        Assert.Empty(result.Value!);
    }

    [Fact]
    public async Task GetFriends_SearchTooLong_ReturnsValidationError()
    {
        var result = await _service.GetFriendsAsync(_me.Id, new string('a', FriendService.MaxFriendSearchLength + 1));

        Assert.Equal(ServiceErrorType.Validation, result.Error!.Type);
        _friendships.Verify(f => f.GetForUserAsync(It.IsAny<Guid>(), It.IsAny<FriendshipStatus>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetRequests_SplitsIncomingAndOutgoing()
    {
        var sender = TestData.User("Sender");
        var receiver = TestData.User("Receiver");
        _friendships.Setup(f => f.GetForUserAsync(_me.Id, FriendshipStatus.Pending, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Friendship(sender, _me, FriendshipStatus.Pending), Friendship(_me, receiver, FriendshipStatus.Pending)]);

        var result = await _service.GetRequestsAsync(_me.Id);

        Assert.Equal("Sender", Assert.Single(result.Value!.Incoming).User.UserName);
        Assert.Equal("Receiver", Assert.Single(result.Value.Outgoing).User.UserName);
    }

    // ---------- Search ----------

    [Fact]
    public async Task Search_TooShort_ReturnsValidationError()
    {
        var result = await _service.SearchUsersAsync(_me.Id, " @a ");

        Assert.Equal(ServiceErrorType.Validation, result.Error!.Type);
    }

    [Fact]
    public async Task Search_ReportsRelationshipForEachMatch()
    {
        var friend = TestData.User("ThemFriend");
        var sentTo = TestData.User("ThemSent");
        var receivedFrom = TestData.User("ThemReceived");
        var stranger = TestData.User("ThemStranger");
        _users.Setup(u => u.SearchByUserNamePrefixAsync("THEM", _me.Id, FriendService.MaxSearchResults, It.IsAny<CancellationToken>()))
            .ReturnsAsync([friend, sentTo, receivedFrom, stranger]);
        var friendship = Friendship(_me, friend, FriendshipStatus.Accepted);
        _friendships.Setup(f => f.GetBetweenManyAsync(_me.Id, It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([friendship, Friendship(_me, sentTo, FriendshipStatus.Pending), Friendship(receivedFrom, _me, FriendshipStatus.Pending)]);

        var result = await _service.SearchUsersAsync(_me.Id, "@Them");

        Assert.Equal(
            [UserRelationship.Friends, UserRelationship.RequestSent, UserRelationship.RequestReceived, UserRelationship.None],
            result.Value!.Select(r => r.Relationship));
        Assert.Equal(friendship.Id, result.Value![0].FriendshipId);
        Assert.Null(result.Value![3].FriendshipId);
    }

    // ---------- Send ----------

    [Fact]
    public async Task Send_NewRequest_CreatesPendingFriendship()
    {
        Friendship? added = null;
        _friendships.Setup(f => f.Add(It.IsAny<Friendship>())).Callback<Friendship>(f => added = f);

        var result = await _service.SendRequestAsync(_me.Id, "@them_reader");

        Assert.NotNull(added);
        Assert.Equal(_me.Id, added.RequesterId);
        Assert.Equal(_them.Id, added.AddresseeId);
        Assert.Equal(FriendshipStatus.Pending, added.Status);
        Assert.Equal(_them.Id, result.Value!.User.Id);
        Assert.False(result.Value.IsIncoming);
        _friendships.Verify(f => f.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Send_UnknownUser_ReturnsNotFound()
    {
        var result = await _service.SendRequestAsync(_me.Id, "nobody");

        Assert.Equal(ServiceErrorType.NotFound, result.Error!.Type);
    }

    [Fact]
    public async Task Send_ToSelf_ReturnsValidationError()
    {
        var result = await _service.SendRequestAsync(_me.Id, "me_reader");

        Assert.Equal(FriendService.CannotFriendSelfMessage, result.Error!.Message);
    }

    [Theory]
    [InlineData(FriendshipStatus.Accepted, true, FriendService.AlreadyFriendsMessage)]
    [InlineData(FriendshipStatus.Accepted, false, FriendService.AlreadyFriendsMessage)]
    [InlineData(FriendshipStatus.Pending, true, FriendService.AlreadyRequestedMessage)]
    [InlineData(FriendshipStatus.Rejected, true, FriendService.RequestUnavailableMessage)]
    [InlineData(FriendshipStatus.Blocked, false, FriendService.RequestUnavailableMessage)]
    public async Task Send_ExistingFriendshipThatCantChange_ReturnsConflict(FriendshipStatus status, bool sentByMe, string message)
    {
        var existing = sentByMe ? Friendship(_me, _them, status) : Friendship(_them, _me, status);
        SetupBetween(existing);

        var result = await _service.SendRequestAsync(_me.Id, "them_reader");

        Assert.Equal(ServiceErrorType.Conflict, result.Error!.Type);
        Assert.Equal(message, result.Error.Message);
        Assert.Equal(status, existing.Status);
    }

    [Fact]
    public async Task Send_WhenTheyAlreadyAsked_AcceptsTheirRequest()
    {
        var existing = SetupBetween(Friendship(_them, _me, FriendshipStatus.Pending));

        var result = await _service.SendRequestAsync(_me.Id, "them_reader");

        Assert.Equal(FriendshipStatus.Accepted, existing.Status);
        Assert.Equal(TestData.Now, existing.UpdatedAt);
        Assert.True(result.Value!.IsIncoming);
        _friendships.Verify(f => f.Add(It.IsAny<Friendship>()), Times.Never);
    }

    [Fact]
    public async Task Send_AfterIDeclinedThem_ReopensAsMyRequest()
    {
        var existing = SetupBetween(Friendship(_them, _me, FriendshipStatus.Rejected));

        var result = await _service.SendRequestAsync(_me.Id, "them_reader");

        Assert.Equal(FriendshipStatus.Pending, existing.Status);
        Assert.Equal(_me.Id, existing.RequesterId);
        Assert.Equal(_them.Id, existing.AddresseeId);
        Assert.False(result.Value!.IsIncoming);
        Assert.Equal(_them.Id, result.Value.User.Id);
    }

    // ---------- Respond / remove ----------

    [Fact]
    public async Task Accept_IncomingPending_AcceptsIt()
    {
        var request = SetupById(Friendship(_them, _me, FriendshipStatus.Pending));

        var result = await _service.AcceptAsync(_me.Id, request.Id);

        Assert.Equal(FriendshipStatus.Accepted, request.Status);
        Assert.Equal(FriendshipStatus.Accepted, result.Value!.Status);
    }

    [Fact]
    public async Task Decline_IncomingPending_RejectsIt()
    {
        var request = SetupById(Friendship(_them, _me, FriendshipStatus.Pending));

        await _service.DeclineAsync(_me.Id, request.Id);

        Assert.Equal(FriendshipStatus.Rejected, request.Status);
    }

    [Fact]
    public async Task Accept_MyOwnOutgoingRequest_IsNotFound()
    {
        var request = SetupById(Friendship(_me, _them, FriendshipStatus.Pending));

        var result = await _service.AcceptAsync(_me.Id, request.Id);

        Assert.Equal(ServiceErrorType.NotFound, result.Error!.Type);
        Assert.Equal(FriendshipStatus.Pending, request.Status);
    }

    [Fact]
    public async Task Accept_Unknown_IsNotFound()
    {
        var result = await _service.AcceptAsync(_me.Id, Guid.NewGuid());

        Assert.Equal(ServiceErrorType.NotFound, result.Error!.Type);
    }

    [Theory]
    [InlineData(FriendshipStatus.Accepted, true, true)]
    [InlineData(FriendshipStatus.Accepted, false, true)]
    [InlineData(FriendshipStatus.Pending, true, true)]
    [InlineData(FriendshipStatus.Pending, false, false)]
    [InlineData(FriendshipStatus.Rejected, true, false)]
    public async Task Remove_OnlyFriendsOrOwnPendingRequests(FriendshipStatus status, bool sentByMe, bool removed)
    {
        var friendship = SetupById(sentByMe ? Friendship(_me, _them, status) : Friendship(_them, _me, status));

        var result = await _service.RemoveAsync(_me.Id, friendship.Id);

        Assert.Equal(removed, result.IsSuccess);
        _friendships.Verify(f => f.Remove(friendship), removed ? Times.Once() : Times.Never());
    }

    [Fact]
    public async Task Remove_SomeoneElsesFriendship_IsNotFound()
    {
        var friendship = SetupById(Friendship(_them, TestData.User("Other"), FriendshipStatus.Accepted));

        var result = await _service.RemoveAsync(_me.Id, friendship.Id);

        Assert.False(result.IsSuccess);
    }

    // ---------- Friend libraries ----------

    [Fact]
    public async Task GetFriendLibraries_Friends_ReturnsOnlyPublicLibraries()
    {
        _friendships.Setup(f => f.AreFriendsAsync(_me.Id, _them.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _libraries.Setup(l => l.GetByOwnerAsync(_them.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([TestData.Library(_them.Id, isPublic: true, "Shared"), TestData.Library(_them.Id, isPublic: false, "Secret")]);

        var result = await _service.GetFriendLibrariesAsync(_me.Id, _them.Id);

        Assert.Equal("Shared", Assert.Single(result.Value!).Name);
    }

    [Fact]
    public async Task GetFriendLibraries_NotFriends_ReturnsNotFound()
    {
        var result = await _service.GetFriendLibrariesAsync(_me.Id, _them.Id);

        Assert.Equal(ServiceErrorType.NotFound, result.Error!.Type);
        _libraries.VerifyNoOtherCalls();
    }

    private Friendship SetupBetween(Friendship friendship)
    {
        _friendships.Setup(f => f.GetBetweenAsync(_me.Id, _them.Id, It.IsAny<CancellationToken>())).ReturnsAsync(friendship);
        return friendship;
    }

    private Friendship SetupById(Friendship friendship)
    {
        _friendships.Setup(f => f.GetByIdAsync(friendship.Id, It.IsAny<CancellationToken>())).ReturnsAsync(friendship);
        return friendship;
    }

    private static Friendship Friendship(User requester, User addressee, FriendshipStatus status) => new()
    {
        Id = Guid.NewGuid(),
        RequesterId = requester.Id,
        Requester = requester,
        AddresseeId = addressee.Id,
        Addressee = addressee,
        Status = status,
        CreatedAt = TestData.Now.AddDays(-1),
        UpdatedAt = TestData.Now.AddDays(-1),
    };
}
