using Alexandria.Api.Models;

namespace Alexandria.Api.Repositories;

public enum LoanRole
{
    Any,
    Lender,
    Borrower,
}

/// <summary>Loans are always returned with LibraryBook (and its Book and Library), Lender and Borrower loaded.</summary>
public interface ILoanRepository
{
    Task<Loan?> GetByIdAsync(Guid loanId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Loan>> GetForUserAsync(Guid userId, LoanRole role, LoanStatus? status, CancellationToken cancellationToken = default);

    /// <summary>True when the borrower already has a pending or active loan for the copy.</summary>
    Task<bool> HasOpenLoanAsync(Guid libraryBookId, Guid borrowerId, CancellationToken cancellationToken = default);

    Task<bool> HasActiveLoanAsync(Guid libraryBookId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Loan>> GetPendingForCopyAsync(Guid libraryBookId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Active loans due between <paramref name="from"/> and <paramref name="to"/> that still have a due reminder to send.
    /// </summary>
    Task<IReadOnlyList<Loan>> GetActiveDueBetweenNeedingReminderAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);

    void Add(Loan loan);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
