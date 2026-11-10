using Alexandria.Api.Dtos;
using Alexandria.Api.Models;
using Alexandria.Api.Repositories;
using Alexandria.Api.Services;
using Alexandria.Api.Services.Notifications;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Alexandria.Api.Tests.Services;

public class NotificationServiceTests
{
    private const string FcmEndpoint = "https://fcm.googleapis.com/fcm/send/abc123";

    private static readonly string ValidP256dh = WebEncoders.Base64UrlEncode(new byte[65]);
    private static readonly string ValidAuth = WebEncoders.Base64UrlEncode(new byte[16]);

    private readonly Mock<IPushSubscriptionRepository> _subscriptions = new();
    private readonly Mock<IWebPushSender> _sender = new();
    private readonly NotificationService _service;

    private readonly User _lender = TestData.User("Lender");
    private readonly User _borrower = TestData.User("Borrower");

    public NotificationServiceTests()
    {
        _sender.SetupGet(s => s.IsConfigured).Returns(true);
        _sender.SetupGet(s => s.PublicKey).Returns("public-key");
        _service = new NotificationService(
            _subscriptions.Object,
            _sender.Object,
            TestData.TimeProviderAt(TestData.Now).Object,
            NullLogger<NotificationService>.Instance);
    }

    // ---------- Public key ----------

    [Fact]
    public void GetPublicKey_Configured_ReturnsKey()
    {
        Assert.Equal("public-key", _service.GetPublicKey().Value!.PublicKey);
    }

    [Fact]
    public void GetPublicKey_NotConfigured_ReturnsUnavailable()
    {
        _sender.SetupGet(s => s.IsConfigured).Returns(false);

        Assert.Equal(ServiceErrorType.Unavailable, _service.GetPublicKey().Error!.Type);
    }

    // ---------- Subscribe ----------

    [Theory]
    [InlineData(FcmEndpoint)]
    [InlineData("https://updates.push.services.mozilla.com/wpush/v2/xyz")]
    [InlineData("https://web.push.apple.com/QGx")]
    [InlineData("https://wns2-by3p.notify.windows.com/w/?token=abc")]
    public async Task Subscribe_NewEndpoint_AddsSubscription(string endpoint)
    {
        PushSubscription? added = null;
        _subscriptions.Setup(s => s.Add(It.IsAny<PushSubscription>())).Callback<PushSubscription>(s => added = s);

        var result = await _service.SubscribeAsync(_lender.Id, Request(endpoint));

        Assert.True(result.IsSuccess);
        Assert.NotNull(added);
        Assert.Equal(_lender.Id, added.UserId);
        Assert.Equal(endpoint, added.Endpoint);
        Assert.Equal(TestData.Now, added.CreatedAt);
        _subscriptions.Verify(s => s.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Subscribe_KnownEndpoint_MovesItToCallerAndUpdatesKeys()
    {
        var existing = Subscription(_borrower.Id);
        existing.Auth = "old";
        _subscriptions.Setup(s => s.GetByEndpointAsync(FcmEndpoint, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var result = await _service.SubscribeAsync(_lender.Id, Request(FcmEndpoint));

        Assert.True(result.IsSuccess);
        Assert.Equal(_lender.Id, existing.UserId);
        Assert.Equal(ValidAuth, existing.Auth);
        _subscriptions.Verify(s => s.Add(It.IsAny<PushSubscription>()), Times.Never);
        _subscriptions.Verify(s => s.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("http://fcm.googleapis.com/fcm/send/abc")]       // not https
    [InlineData("https://evil.example.com/push")]                 // not a push service
    [InlineData("https://fcm.googleapis.com.evil.com/x")]         // lookalike host
    [InlineData("https://fcm.googleapis.com:8443/x")]             // non-default port
    [InlineData("https://user:pass@fcm.googleapis.com/x")]        // credentials in URL
    [InlineData("https://localhost.push.apple.com.example/x")]
    [InlineData("not a url")]
    public async Task Subscribe_DisallowedEndpoint_ReturnsValidationError(string endpoint)
    {
        var result = await _service.SubscribeAsync(_lender.Id, Request(endpoint));

        Assert.Equal(ServiceErrorType.Validation, result.Error!.Type);
        Assert.Equal(NotificationService.InvalidEndpointMessage, result.Error.Message);
        _subscriptions.Verify(s => s.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(64, 16)]
    [InlineData(65, 15)]
    public async Task Subscribe_WrongKeyLengths_ReturnsValidationError(int p256dhBytes, int authBytes)
    {
        var request = Request(FcmEndpoint);
        request.P256dh = WebEncoders.Base64UrlEncode(new byte[p256dhBytes]);
        request.Auth = WebEncoders.Base64UrlEncode(new byte[authBytes]);

        var result = await _service.SubscribeAsync(_lender.Id, request);

        Assert.Equal(NotificationService.InvalidKeysMessage, result.Error!.Message);
    }

    [Fact]
    public async Task Subscribe_KeysNotBase64Url_ReturnsValidationError()
    {
        var request = Request(FcmEndpoint);
        request.Auth = "*not base64*";

        var result = await _service.SubscribeAsync(_lender.Id, request);

        Assert.Equal(NotificationService.InvalidKeysMessage, result.Error!.Message);
    }

    [Fact]
    public async Task Subscribe_NotConfigured_ReturnsUnavailable()
    {
        _sender.SetupGet(s => s.IsConfigured).Returns(false);

        var result = await _service.SubscribeAsync(_lender.Id, Request(FcmEndpoint));

        Assert.Equal(ServiceErrorType.Unavailable, result.Error!.Type);
    }

    // ---------- Unsubscribe ----------

    [Fact]
    public async Task Unsubscribe_OwnSubscription_RemovesIt()
    {
        var existing = Subscription(_lender.Id);
        _subscriptions.Setup(s => s.GetByEndpointAsync(FcmEndpoint, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var result = await _service.UnsubscribeAsync(_lender.Id, FcmEndpoint);

        Assert.True(result.IsSuccess);
        _subscriptions.Verify(s => s.Remove(existing), Times.Once);
        _subscriptions.Verify(s => s.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Unsubscribe_SomeoneElsesSubscription_ReturnsNotFound()
    {
        _subscriptions.Setup(s => s.GetByEndpointAsync(FcmEndpoint, It.IsAny<CancellationToken>())).ReturnsAsync(Subscription(_borrower.Id));

        var result = await _service.UnsubscribeAsync(_lender.Id, FcmEndpoint);

        Assert.Equal(ServiceErrorType.NotFound, result.Error!.Type);
        _subscriptions.Verify(s => s.Remove(It.IsAny<PushSubscription>()), Times.Never);
    }

    [Fact]
    public async Task Unsubscribe_Unknown_ReturnsNotFound()
    {
        var result = await _service.UnsubscribeAsync(_lender.Id, FcmEndpoint);

        Assert.Equal(ServiceErrorType.NotFound, result.Error!.Type);
    }

    // ---------- Notify ----------

    [Fact]
    public async Task NotifyBorrowRequest_SendsToEveryLenderSubscription()
    {
        var phone = Subscription(_lender.Id, "https://fcm.googleapis.com/fcm/send/phone");
        var laptop = Subscription(_lender.Id, "https://updates.push.services.mozilla.com/wpush/v2/laptop");
        _subscriptions.Setup(s => s.GetForUserAsync(_lender.Id, It.IsAny<CancellationToken>())).ReturnsAsync([phone, laptop]);
        var sent = new List<PushMessage>();
        _sender.Setup(s => s.SendAsync(It.IsAny<PushSubscription>(), It.IsAny<PushMessage>(), It.IsAny<CancellationToken>()))
            .Callback<PushSubscription, PushMessage, CancellationToken>((_, m, _) => sent.Add(m))
            .ReturnsAsync(PushSendResult.Sent);

        await _service.NotifyBorrowRequestAsync(BorrowRequest("Dune"));

        Assert.Equal(2, sent.Count);
        Assert.All(sent, m =>
        {
            Assert.Equal("Borrow request", m.Title);
            Assert.Equal($"{_borrower.DisplayName} wants to borrow Dune.", m.Body);
            Assert.Equal("/loans", m.Url);
        });
        _subscriptions.Verify(s => s.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task NotifyBorrowRequest_GoneSubscription_IsDeleted()
    {
        var gone = Subscription(_lender.Id, "https://fcm.googleapis.com/fcm/send/gone");
        var alive = Subscription(_lender.Id);
        _subscriptions.Setup(s => s.GetForUserAsync(_lender.Id, It.IsAny<CancellationToken>())).ReturnsAsync([gone, alive]);
        _sender.Setup(s => s.SendAsync(gone, It.IsAny<PushMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(PushSendResult.Gone);
        _sender.Setup(s => s.SendAsync(alive, It.IsAny<PushMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(PushSendResult.Failed);

        await _service.NotifyBorrowRequestAsync(BorrowRequest("Dune"));

        _subscriptions.Verify(s => s.Remove(gone), Times.Once);
        _subscriptions.Verify(s => s.Remove(alive), Times.Never);
        _subscriptions.Verify(s => s.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NotifyBorrowRequest_SenderThrows_DoesNotThrow()
    {
        _subscriptions.Setup(s => s.GetForUserAsync(_lender.Id, It.IsAny<CancellationToken>())).ReturnsAsync([Subscription(_lender.Id)]);
        _sender.Setup(s => s.SendAsync(It.IsAny<PushSubscription>(), It.IsAny<PushMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("network down"));

        var exception = await Record.ExceptionAsync(() => _service.NotifyBorrowRequestAsync(BorrowRequest("Dune")));

        Assert.Null(exception);
    }

    [Fact]
    public async Task NotifyBorrowRequest_NoSubscriptions_SendsNothing()
    {
        _subscriptions.Setup(s => s.GetForUserAsync(_lender.Id, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        await _service.NotifyBorrowRequestAsync(BorrowRequest("Dune"));

        _sender.Verify(s => s.SendAsync(It.IsAny<PushSubscription>(), It.IsAny<PushMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task NotifyBorrowRequest_NotConfigured_DoesNothing()
    {
        _sender.SetupGet(s => s.IsConfigured).Returns(false);

        await _service.NotifyBorrowRequestAsync(BorrowRequest("Dune"));

        _subscriptions.Verify(s => s.GetForUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task NotifyBorrowRequest_MissingNames_UsesFallbackText()
    {
        _subscriptions.Setup(s => s.GetForUserAsync(_lender.Id, It.IsAny<CancellationToken>())).ReturnsAsync([Subscription(_lender.Id)]);
        PushMessage? sent = null;
        _sender.Setup(s => s.SendAsync(It.IsAny<PushSubscription>(), It.IsAny<PushMessage>(), It.IsAny<CancellationToken>()))
            .Callback<PushSubscription, PushMessage, CancellationToken>((_, m, _) => sent = m)
            .ReturnsAsync(PushSendResult.Sent);

        await _service.NotifyBorrowRequestAsync(new Loan { Id = Guid.NewGuid(), LenderId = _lender.Id, BorrowerId = _borrower.Id });

        Assert.Equal("A friend wants to borrow one of your books.", sent!.Body);
    }

    [Fact]
    public async Task NotifyFriendRequest_SendsToAddressee()
    {
        _subscriptions.Setup(s => s.GetForUserAsync(_lender.Id, It.IsAny<CancellationToken>())).ReturnsAsync([Subscription(_lender.Id)]);
        var sent = CaptureSent();

        await _service.NotifyFriendRequestAsync(new Friendship
        {
            Id = Guid.NewGuid(),
            RequesterId = _borrower.Id,
            Requester = _borrower,
            AddresseeId = _lender.Id,
            Status = FriendshipStatus.Pending,
        });

        var message = Assert.Single(sent);
        Assert.Equal("Friend request", message.Title);
        Assert.Equal($"{_borrower.DisplayName} (@{_borrower.UserName}) wants to be friends.", message.Body);
        Assert.Equal("/friends", message.Url);
    }

    [Fact]
    public async Task NotifyFriendRequest_RequesterNotLoaded_UsesFallbackText()
    {
        _subscriptions.Setup(s => s.GetForUserAsync(_lender.Id, It.IsAny<CancellationToken>())).ReturnsAsync([Subscription(_lender.Id)]);
        var sent = CaptureSent();

        await _service.NotifyFriendRequestAsync(new Friendship { Id = Guid.NewGuid(), RequesterId = _borrower.Id, AddresseeId = _lender.Id });

        Assert.Equal("Someone wants to be friends.", Assert.Single(sent).Body);
    }

    [Theory]
    [InlineData(DueReminder.Tomorrow, "Due tomorrow", "Dune is due back to {0} tomorrow.")]
    [InlineData(DueReminder.Today, "Due today", "Dune is due back to {0} today.")]
    public async Task NotifyLoanDue_SendsToBorrower(DueReminder reminder, string title, string body)
    {
        _subscriptions.Setup(s => s.GetForUserAsync(_borrower.Id, It.IsAny<CancellationToken>())).ReturnsAsync([Subscription(_borrower.Id)]);
        var sent = CaptureSent();

        await _service.NotifyLoanDueAsync(BorrowRequest("Dune"), reminder);

        var message = Assert.Single(sent);
        Assert.Equal(title, message.Title);
        Assert.Equal(string.Format(body, _lender.DisplayName), message.Body);
        Assert.Equal("/loans", message.Url);
        _subscriptions.Verify(s => s.GetForUserAsync(_lender.Id, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task NotifyLoanDue_MissingNames_UsesFallbackText()
    {
        _subscriptions.Setup(s => s.GetForUserAsync(_borrower.Id, It.IsAny<CancellationToken>())).ReturnsAsync([Subscription(_borrower.Id)]);
        var sent = CaptureSent();

        await _service.NotifyLoanDueAsync(new Loan { Id = Guid.NewGuid(), LenderId = _lender.Id, BorrowerId = _borrower.Id }, DueReminder.Today);

        Assert.Equal("A book you borrowed is due back to its owner today.", Assert.Single(sent).Body);
    }

    private List<PushMessage> CaptureSent()
    {
        var sent = new List<PushMessage>();
        _sender.Setup(s => s.SendAsync(It.IsAny<PushSubscription>(), It.IsAny<PushMessage>(), It.IsAny<CancellationToken>()))
            .Callback<PushSubscription, PushMessage, CancellationToken>((_, m, _) => sent.Add(m))
            .ReturnsAsync(PushSendResult.Sent);
        return sent;
    }

    private static SubscribePushRequest Request(string endpoint) =>
        new() { Endpoint = endpoint, P256dh = ValidP256dh, Auth = ValidAuth };

    private static PushSubscription Subscription(Guid userId, string endpoint = FcmEndpoint) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        Endpoint = endpoint,
        P256dh = ValidP256dh,
        Auth = ValidAuth,
        CreatedAt = TestData.Now,
    };

    private Loan BorrowRequest(string bookTitle)
    {
        var book = TestData.Book();
        book.Title = bookTitle;
        var copy = TestData.Copy(TestData.Library(_lender.Id, isPublic: true), book, LibraryBookStatus.Available);
        return new Loan
        {
            Id = Guid.NewGuid(),
            LibraryBookId = copy.Id,
            LibraryBook = copy,
            LenderId = _lender.Id,
            Lender = _lender,
            BorrowerId = _borrower.Id,
            Borrower = _borrower,
            Status = LoanStatus.Pending,
        };
    }
}
