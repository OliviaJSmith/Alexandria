using Alexandria.Api.Dtos;
using Alexandria.Api.Models;
using Alexandria.Api.Repositories;
using Alexandria.Api.Services;
using Alexandria.Api.Services.Notifications;
using Moq;

namespace Alexandria.Api.Tests.Services;

public class LoanServiceTests
{
    private readonly Mock<ILoanRepository> _loans = new();
    private readonly Mock<ILibraryRepository> _libraries = new();
    private readonly Mock<IFriendshipRepository> _friendships = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<INotificationService> _notifications = new();
    private readonly LoanService _service;

    private readonly User _owner = TestData.User("Owner");
    private readonly User _friend = TestData.User("Friend");
    private readonly User _stranger = TestData.User("Stranger");

    public LoanServiceTests()
    {
        foreach (var user in new[] { _owner, _friend, _stranger })
        {
            _users.Setup(u => u.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        }

        _friendships.Setup(f => f.AreFriendsAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _friendships.Setup(f => f.AreFriendsAsync(_owner.Id, _friend.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _loans.Setup(l => l.GetPendingForCopyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _service = new LoanService(_loans.Object, _libraries.Object, _friendships.Object, _users.Object, _notifications.Object, TestData.TimeProviderAt(TestData.Now).Object);
    }

    // ---------- List ----------

    [Fact]
    public async Task GetLoans_MapsLoansAndFlagsOverdue()
    {
        var copy = Copy();
        var overdue = Loan(copy, _friend, LoanStatus.Active, due: TestData.Now.AddDays(-1));
        var current = Loan(copy, _friend, LoanStatus.Active, due: TestData.Now.AddDays(3));
        var pendingPastDue = Loan(copy, _friend, LoanStatus.Pending, due: TestData.Now.AddDays(-1));
        _loans.Setup(l => l.GetForUserAsync(_owner.Id, LoanRole.Lender, LoanStatus.Active, It.IsAny<CancellationToken>()))
            .ReturnsAsync([overdue, current, pendingPastDue]);

        var result = await _service.GetLoansAsync(_owner.Id, LoanRole.Lender, LoanStatus.Active);

        Assert.Equal([true, false, false], result.Value!.Select(l => l.IsOverdue));
        var dto = result.Value![0];
        Assert.Equal("Living Room", dto.LibraryName);
        Assert.Equal(_owner.Id, dto.Lender.Id);
        Assert.Equal(_friend.Id, dto.Borrower.Id);
        Assert.Equal("The Hobbit", dto.Book.Title);
    }

    // ---------- Lend ----------

    [Fact]
    public async Task Lend_ToFriend_StartsActiveLoanAndChecksOutCopy()
    {
        var copy = Copy();
        var otherRequest = Loan(copy, _stranger, LoanStatus.Pending);
        _loans.Setup(l => l.GetPendingForCopyAsync(copy.Id, It.IsAny<CancellationToken>())).ReturnsAsync([otherRequest]);
        Loan? added = null;
        _loans.Setup(l => l.Add(It.IsAny<Loan>())).Callback<Loan>(l => added = l);

        var result = await _service.LendAsync(_owner.Id, Lend(copy, _friend, TestData.Now.AddDays(14)));

        Assert.True(result.IsSuccess);
        Assert.NotNull(added);
        Assert.Equal(LoanStatus.Active, added.Status);
        Assert.Equal(TestData.Now, added.LoanDate);
        Assert.Equal(TestData.Now.AddDays(14), added.DueDate);
        Assert.Equal(LibraryBookStatus.CheckedOut, copy.Status);
        Assert.Equal(LoanStatus.Cancelled, otherRequest.Status);
        Assert.Equal(_friend.Id, result.Value!.Borrower.Id);
        _loans.Verify(l => l.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Lend_CopySetAsideToLend_IsAllowed()
    {
        var copy = Copy(LibraryBookStatus.WaitingToBeLoanedOut);

        var result = await _service.LendAsync(_owner.Id, Lend(copy, _friend));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Lend_DueDateInPast_ReturnsValidationError()
    {
        var result = await _service.LendAsync(_owner.Id, Lend(Copy(), _friend, TestData.Now.AddMinutes(-1)));

        Assert.Equal(LoanService.DueDateInPastMessage, result.Error!.Message);
    }

    [Fact]
    public async Task Lend_SomeoneElsesCopy_ReturnsNotFound()
    {
        var copy = Copy(owner: _friend);

        var result = await _service.LendAsync(_owner.Id, Lend(copy, _friend));

        Assert.Equal(ServiceErrorType.NotFound, result.Error!.Type);
    }

    [Fact]
    public async Task Lend_UnknownCopy_ReturnsNotFound()
    {
        var result = await _service.LendAsync(_owner.Id, new LendBookRequest { LibraryBookId = Guid.NewGuid(), BorrowerId = _friend.Id });

        Assert.Equal(ServiceErrorType.NotFound, result.Error!.Type);
    }

    [Fact]
    public async Task Lend_ToSelf_ReturnsValidationError()
    {
        var result = await _service.LendAsync(_owner.Id, Lend(Copy(), _owner));

        Assert.Equal(LoanService.OwnBookMessage, result.Error!.Message);
    }

    [Fact]
    public async Task Lend_ToNonFriend_ReturnsValidationError()
    {
        var result = await _service.LendAsync(_owner.Id, Lend(Copy(), _stranger));

        Assert.Equal(LoanService.NotFriendsMessage, result.Error!.Message);
    }

    [Fact]
    public async Task Lend_CopyAlreadyOut_ReturnsConflict()
    {
        var result = await _service.LendAsync(_owner.Id, Lend(Copy(LibraryBookStatus.CheckedOut), _friend));

        Assert.Equal(ServiceErrorType.Conflict, result.Error!.Type);
    }

    // ---------- Request ----------

    [Fact]
    public async Task Request_FriendsPublicCopy_CreatesPendingLoan()
    {
        var copy = Copy(isPublic: true);
        Loan? added = null;
        _loans.Setup(l => l.Add(It.IsAny<Loan>())).Callback<Loan>(l => added = l);

        var result = await _service.RequestAsync(_friend.Id, new BorrowRequest { LibraryBookId = copy.Id });

        Assert.NotNull(added);
        Assert.Equal(LoanStatus.Pending, added.Status);
        Assert.Null(added.LoanDate);
        Assert.Equal(_owner.Id, added.LenderId);
        Assert.Equal(LibraryBookStatus.Available, copy.Status);
        Assert.Equal(_owner.Id, result.Value!.Lender.Id);
    }

    [Fact]
    public async Task Request_Succeeds_NotifiesTheLender()
    {
        var copy = Copy(isPublic: true);

        await _service.RequestAsync(_friend.Id, new BorrowRequest { LibraryBookId = copy.Id });

        _notifications.Verify(n => n.NotifyBorrowRequestAsync(
            It.Is<Loan>(l => l.LenderId == _owner.Id && l.Borrower == _friend && l.LibraryBook == copy),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Request_Rejected_DoesNotNotify()
    {
        var copy = Copy(isPublic: false);

        await _service.RequestAsync(_friend.Id, new BorrowRequest { LibraryBookId = copy.Id });

        _notifications.Verify(n => n.NotifyBorrowRequestAsync(It.IsAny<Loan>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Lend_DoesNotSendBorrowRequestNotification()
    {
        var copy = Copy();

        await _service.LendAsync(_owner.Id, new LendBookRequest { LibraryBookId = copy.Id, BorrowerId = _friend.Id });

        _notifications.Verify(n => n.NotifyBorrowRequestAsync(It.IsAny<Loan>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(false, true)] // private library of a friend
    [InlineData(true, false)] // public library of a stranger
    public async Task Request_CopyNotVisibleToBorrower_ReturnsNotFound(bool isPublic, bool asFriend)
    {
        var copy = Copy(isPublic: isPublic);

        var result = await _service.RequestAsync(asFriend ? _friend.Id : _stranger.Id, new BorrowRequest { LibraryBookId = copy.Id });

        Assert.Equal(ServiceErrorType.NotFound, result.Error!.Type);
    }

    [Fact]
    public async Task Request_UnknownCopy_ReturnsNotFound()
    {
        var result = await _service.RequestAsync(_friend.Id, new BorrowRequest { LibraryBookId = Guid.NewGuid() });

        Assert.Equal(ServiceErrorType.NotFound, result.Error!.Type);
    }

    [Fact]
    public async Task Request_OwnCopy_ReturnsValidationError()
    {
        var copy = Copy(isPublic: true);

        var result = await _service.RequestAsync(_owner.Id, new BorrowRequest { LibraryBookId = copy.Id });

        Assert.Equal(LoanService.OwnBookMessage, result.Error!.Message);
    }

    [Theory]
    [InlineData(LibraryBookStatus.CheckedOut)]
    [InlineData(LibraryBookStatus.WaitingToBeLoanedOut)]
    public async Task Request_CopyNotOnShelf_ReturnsConflict(LibraryBookStatus status)
    {
        var copy = Copy(status, isPublic: true);

        var result = await _service.RequestAsync(_friend.Id, new BorrowRequest { LibraryBookId = copy.Id });

        Assert.Equal(LoanService.NotAvailableMessage, result.Error!.Message);
    }

    [Fact]
    public async Task Request_AlreadyRequested_ReturnsConflict()
    {
        var copy = Copy(isPublic: true);
        _loans.Setup(l => l.HasOpenLoanAsync(copy.Id, _friend.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _service.RequestAsync(_friend.Id, new BorrowRequest { LibraryBookId = copy.Id });

        Assert.Equal(LoanService.AlreadyRequestedMessage, result.Error!.Message);
    }

    [Fact]
    public async Task Request_DueDateInPast_ReturnsValidationError()
    {
        var result = await _service.RequestAsync(_friend.Id, new BorrowRequest { LibraryBookId = Guid.NewGuid(), DueDate = TestData.Now });

        Assert.Equal(ServiceErrorType.Validation, result.Error!.Type);
    }

    // ---------- Approve / decline / cancel / return ----------

    [Fact]
    public async Task Approve_PendingRequest_StartsLoanAndCancelsOtherRequests()
    {
        var copy = Copy(isPublic: true);
        var request = SetupLoan(Loan(copy, _friend, LoanStatus.Pending));
        var competing = Loan(copy, _stranger, LoanStatus.Pending);
        _loans.Setup(l => l.GetPendingForCopyAsync(copy.Id, It.IsAny<CancellationToken>())).ReturnsAsync([request, competing]);

        var result = await _service.ApproveAsync(_owner.Id, request.Id);

        Assert.Equal(LoanStatus.Active, request.Status);
        Assert.Equal(TestData.Now, request.LoanDate);
        Assert.Equal(LoanStatus.Cancelled, competing.Status);
        Assert.Equal(LibraryBookStatus.CheckedOut, copy.Status);
        Assert.Equal(LoanStatus.Active, result.Value!.Status);
    }

    [Fact]
    public async Task Approve_CopyAlreadyOut_ReturnsConflict()
    {
        var request = SetupLoan(Loan(Copy(LibraryBookStatus.CheckedOut), _friend, LoanStatus.Pending));

        var result = await _service.ApproveAsync(_owner.Id, request.Id);

        Assert.Equal(ServiceErrorType.Conflict, result.Error!.Type);
        Assert.Equal(LoanStatus.Pending, request.Status);
    }

    [Fact]
    public async Task Approve_ByBorrower_IsNotFound()
    {
        var request = SetupLoan(Loan(Copy(), _friend, LoanStatus.Pending));

        var result = await _service.ApproveAsync(_friend.Id, request.Id);

        Assert.Equal(ServiceErrorType.NotFound, result.Error!.Type);
    }

    [Fact]
    public async Task Decline_ByLender_CancelsRequest()
    {
        var request = SetupLoan(Loan(Copy(), _friend, LoanStatus.Pending));

        var result = await _service.DeclineAsync(_owner.Id, request.Id);

        Assert.Equal(LoanStatus.Cancelled, request.Status);
        Assert.Equal(LoanStatus.Cancelled, result.Value!.Status);
    }

    [Fact]
    public async Task Cancel_ByBorrower_CancelsRequest_ByLender_IsNotFound()
    {
        var request = SetupLoan(Loan(Copy(), _friend, LoanStatus.Pending));

        var byLender = await _service.CancelAsync(_owner.Id, request.Id);
        Assert.Equal(ServiceErrorType.NotFound, byLender.Error!.Type);

        var byBorrower = await _service.CancelAsync(_friend.Id, request.Id);
        Assert.True(byBorrower.IsSuccess);
        Assert.Equal(LoanStatus.Cancelled, request.Status);
    }

    [Fact]
    public async Task Return_ActiveLoan_ReturnsCopyToShelfAndClearsNote()
    {
        var copy = Copy(LibraryBookStatus.CheckedOut);
        copy.LoanNote = "With Friend";
        var loan = SetupLoan(Loan(copy, _friend, LoanStatus.Active));

        var result = await _service.ReturnAsync(_owner.Id, loan.Id);

        Assert.Equal(LoanStatus.Returned, loan.Status);
        Assert.Equal(TestData.Now, loan.ReturnedDate);
        Assert.Equal(LibraryBookStatus.Available, copy.Status);
        Assert.Null(copy.LoanNote);
        Assert.Equal(TestData.Now, result.Value!.ReturnedDate);
    }

    [Fact]
    public async Task Return_PendingLoan_IsNotFound()
    {
        var loan = SetupLoan(Loan(Copy(), _friend, LoanStatus.Pending));

        var result = await _service.ReturnAsync(_owner.Id, loan.Id);

        Assert.Equal(ServiceErrorType.NotFound, result.Error!.Type);
    }

    [Fact]
    public async Task Return_UnknownLoan_IsNotFound()
    {
        var result = await _service.ReturnAsync(_owner.Id, Guid.NewGuid());

        Assert.False(result.IsSuccess);
    }

    private LibraryBook Copy(LibraryBookStatus status = LibraryBookStatus.Available, bool isPublic = false, User? owner = null)
    {
        var copy = TestData.Copy(TestData.Library((owner ?? _owner).Id, isPublic), TestData.Book(), status);
        _libraries.Setup(l => l.GetLibraryBookByIdAsync(copy.Id, It.IsAny<CancellationToken>())).ReturnsAsync(copy);
        return copy;
    }

    private Loan SetupLoan(Loan loan)
    {
        _loans.Setup(l => l.GetByIdAsync(loan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(loan);
        return loan;
    }

    private Loan Loan(LibraryBook copy, User borrower, LoanStatus status, DateTimeOffset? due = null) => new()
    {
        Id = Guid.NewGuid(),
        LibraryBookId = copy.Id,
        LibraryBook = copy,
        LenderId = copy.Library!.OwnerId,
        Lender = _owner,
        BorrowerId = borrower.Id,
        Borrower = borrower,
        Status = status,
        DueDate = due,
        CreatedAt = TestData.Now.AddDays(-2),
        UpdatedAt = TestData.Now.AddDays(-2),
    };

    private static LendBookRequest Lend(LibraryBook copy, User borrower, DateTimeOffset? due = null) =>
        new() { LibraryBookId = copy.Id, BorrowerId = borrower.Id, DueDate = due };
}
