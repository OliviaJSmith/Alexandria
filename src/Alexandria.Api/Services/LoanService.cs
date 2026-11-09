using Alexandria.Api.Dtos;
using Alexandria.Api.Models;
using Alexandria.Api.Repositories;

namespace Alexandria.Api.Services;

public class LoanService(
    ILoanRepository loans,
    ILibraryRepository libraries,
    IFriendshipRepository friendships,
    IUserRepository users,
    TimeProvider timeProvider) : ILoanService
{
    public const string CopyNotFoundMessage = "Book not found.";
    public const string LoanNotFoundMessage = "Loan not found.";
    public const string NotFriendsMessage = "You can only lend to and borrow from friends.";
    public const string OwnBookMessage = "You can't borrow your own book.";
    public const string NotAvailableMessage = "This book isn't available right now.";
    public const string AlreadyRequestedMessage = "You already have an open request or loan for this book.";
    public const string DueDateInPastMessage = "The due date must be in the future.";

    public async Task<ServiceResult<IReadOnlyList<LoanDto>>> GetLoansAsync(
        Guid userId,
        LoanRole role,
        LoanStatus? status,
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var found = await loans.GetForUserAsync(userId, role, status, cancellationToken);
        return ServiceResult<IReadOnlyList<LoanDto>>.Success(found.Select(l => LoanDto.FromLoan(l, now)).ToList());
    }

    public async Task<ServiceResult<LoanDto>> LendAsync(Guid lenderId, LendBookRequest request, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        if (request.DueDate is { } due && due <= now)
        {
            return Failure(ServiceErrorType.Validation, DueDateInPastMessage);
        }

        var copy = await libraries.GetLibraryBookByIdAsync(request.LibraryBookId, cancellationToken);
        if (copy?.Library is null || copy.Library.OwnerId != lenderId)
        {
            return Failure(ServiceErrorType.NotFound, CopyNotFoundMessage);
        }

        if (request.BorrowerId == lenderId)
        {
            return Failure(ServiceErrorType.Validation, OwnBookMessage);
        }

        if (!await friendships.AreFriendsAsync(lenderId, request.BorrowerId, cancellationToken))
        {
            return Failure(ServiceErrorType.Validation, NotFriendsMessage);
        }

        if (!CanGoOut(copy.Status))
        {
            return Failure(ServiceErrorType.Conflict, NotAvailableMessage);
        }

        var loan = NewLoan(copy, lenderId, request.BorrowerId, LoanStatus.Active, request.DueDate, now);
        loan.LoanDate = now;
        loan.Lender = await users.GetByIdAsync(lenderId, cancellationToken);
        loan.Borrower = await users.GetByIdAsync(request.BorrowerId, cancellationToken);
        copy.Status = LibraryBookStatus.CheckedOut;
        await CancelPendingRequestsAsync(copy.Id, now, cancellationToken);
        loans.Add(loan);
        await loans.SaveChangesAsync(cancellationToken);
        return ServiceResult<LoanDto>.Success(LoanDto.FromLoan(loan, now));
    }

    public async Task<ServiceResult<LoanDto>> RequestAsync(Guid borrowerId, BorrowRequest request, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        if (request.DueDate is { } due && due <= now)
        {
            return Failure(ServiceErrorType.Validation, DueDateInPastMessage);
        }

        var copy = await libraries.GetLibraryBookByIdAsync(request.LibraryBookId, cancellationToken);
        if (copy?.Library is null)
        {
            return Failure(ServiceErrorType.NotFound, CopyNotFoundMessage);
        }

        var lenderId = copy.Library.OwnerId;
        if (lenderId == borrowerId)
        {
            return Failure(ServiceErrorType.Validation, OwnBookMessage);
        }

        // Same visibility as browsing: only public libraries of friends.
        if (!copy.Library.IsPublic || !await friendships.AreFriendsAsync(lenderId, borrowerId, cancellationToken))
        {
            return Failure(ServiceErrorType.NotFound, CopyNotFoundMessage);
        }

        if (copy.Status != LibraryBookStatus.Available)
        {
            return Failure(ServiceErrorType.Conflict, NotAvailableMessage);
        }

        if (await loans.HasOpenLoanAsync(copy.Id, borrowerId, cancellationToken))
        {
            return Failure(ServiceErrorType.Conflict, AlreadyRequestedMessage);
        }

        var loan = NewLoan(copy, lenderId, borrowerId, LoanStatus.Pending, request.DueDate, now);
        loan.Lender = await users.GetByIdAsync(lenderId, cancellationToken);
        loan.Borrower = await users.GetByIdAsync(borrowerId, cancellationToken);
        loans.Add(loan);
        await loans.SaveChangesAsync(cancellationToken);
        return ServiceResult<LoanDto>.Success(LoanDto.FromLoan(loan, now));
    }

    public async Task<ServiceResult<LoanDto>> ApproveAsync(Guid lenderId, Guid loanId, CancellationToken cancellationToken = default)
    {
        var loan = await loans.GetByIdAsync(loanId, cancellationToken);
        if (loan is null || loan.LenderId != lenderId || loan.Status != LoanStatus.Pending)
        {
            return Failure(ServiceErrorType.NotFound, LoanNotFoundMessage);
        }

        if (!CanGoOut(loan.LibraryBook!.Status))
        {
            return Failure(ServiceErrorType.Conflict, NotAvailableMessage);
        }

        var now = timeProvider.GetUtcNow();
        await CancelPendingRequestsAsync(loan.LibraryBookId, now, cancellationToken);
        loan.Status = LoanStatus.Active;
        loan.LoanDate = now;
        loan.UpdatedAt = now;
        loan.LibraryBook.Status = LibraryBookStatus.CheckedOut;
        await loans.SaveChangesAsync(cancellationToken);
        return ServiceResult<LoanDto>.Success(LoanDto.FromLoan(loan, now));
    }

    public Task<ServiceResult<LoanDto>> DeclineAsync(Guid lenderId, Guid loanId, CancellationToken cancellationToken = default) =>
        TransitionAsync(loanId, l => l.LenderId == lenderId && l.Status == LoanStatus.Pending, LoanStatus.Cancelled, cancellationToken);

    public Task<ServiceResult<LoanDto>> CancelAsync(Guid borrowerId, Guid loanId, CancellationToken cancellationToken = default) =>
        TransitionAsync(loanId, l => l.BorrowerId == borrowerId && l.Status == LoanStatus.Pending, LoanStatus.Cancelled, cancellationToken);

    public Task<ServiceResult<LoanDto>> ReturnAsync(Guid lenderId, Guid loanId, CancellationToken cancellationToken = default) =>
        TransitionAsync(loanId, l => l.LenderId == lenderId && l.Status == LoanStatus.Active, LoanStatus.Returned, cancellationToken);

    private async Task<ServiceResult<LoanDto>> TransitionAsync(
        Guid loanId,
        Func<Loan, bool> allowed,
        LoanStatus newStatus,
        CancellationToken cancellationToken)
    {
        var loan = await loans.GetByIdAsync(loanId, cancellationToken);
        if (loan is null || !allowed(loan))
        {
            return Failure(ServiceErrorType.NotFound, LoanNotFoundMessage);
        }

        var now = timeProvider.GetUtcNow();
        loan.Status = newStatus;
        loan.UpdatedAt = now;
        if (newStatus == LoanStatus.Returned)
        {
            loan.ReturnedDate = now;
            loan.LibraryBook!.Status = LibraryBookStatus.Available;
            loan.LibraryBook.LoanNote = null;
        }

        await loans.SaveChangesAsync(cancellationToken);
        return ServiceResult<LoanDto>.Success(LoanDto.FromLoan(loan, now));
    }

    /// <summary>Once a copy goes out, other people's pending requests for it can't be fulfilled.</summary>
    private async Task CancelPendingRequestsAsync(Guid libraryBookId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        foreach (var pending in await loans.GetPendingForCopyAsync(libraryBookId, cancellationToken))
        {
            pending.Status = LoanStatus.Cancelled;
            pending.UpdatedAt = now;
        }
    }

    /// <summary>Owners may lend copies that are on the shelf or set aside to lend; borrowers can only request shelved ones.</summary>
    private static bool CanGoOut(LibraryBookStatus status) =>
        status is LibraryBookStatus.Available or LibraryBookStatus.WaitingToBeLoanedOut;

    private static Loan NewLoan(LibraryBook copy, Guid lenderId, Guid borrowerId, LoanStatus status, DateTimeOffset? dueDate, DateTimeOffset now) => new()
    {
        Id = Guid.NewGuid(),
        LibraryBookId = copy.Id,
        LibraryBook = copy,
        LenderId = lenderId,
        BorrowerId = borrowerId,
        Status = status,
        DueDate = dueDate,
        CreatedAt = now,
        UpdatedAt = now,
    };

    private static ServiceResult<LoanDto> Failure(ServiceErrorType type, string message) => ServiceResult<LoanDto>.Failure(type, message);
}
