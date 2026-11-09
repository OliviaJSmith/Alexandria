using Alexandria.Api.Data;
using Alexandria.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Alexandria.Api.Repositories;

public class LoanRepository(AlexandriaDbContext context) : ILoanRepository
{
    public async Task<Loan?> GetByIdAsync(Guid loanId, CancellationToken cancellationToken = default)
    {
        return await WithDetails().SingleOrDefaultAsync(l => l.Id == loanId, cancellationToken);
    }

    public async Task<IReadOnlyList<Loan>> GetForUserAsync(
        Guid userId,
        LoanRole role,
        LoanStatus? status,
        CancellationToken cancellationToken = default)
    {
        var loans = role switch
        {
            LoanRole.Lender => WithDetails().Where(l => l.LenderId == userId),
            LoanRole.Borrower => WithDetails().Where(l => l.BorrowerId == userId),
            _ => WithDetails().Where(l => l.LenderId == userId || l.BorrowerId == userId),
        };

        if (status is not null)
        {
            loans = loans.Where(l => l.Status == status);
        }

        return await loans.OrderByDescending(l => l.UpdatedAt).ToListAsync(cancellationToken);
    }

    public async Task<bool> HasOpenLoanAsync(Guid libraryBookId, Guid borrowerId, CancellationToken cancellationToken = default)
    {
        return await context.Loans.AnyAsync(
            l => l.LibraryBookId == libraryBookId
                && l.BorrowerId == borrowerId
                && (l.Status == LoanStatus.Pending || l.Status == LoanStatus.Active),
            cancellationToken);
    }

    public async Task<bool> HasActiveLoanAsync(Guid libraryBookId, CancellationToken cancellationToken = default)
    {
        return await context.Loans.AnyAsync(l => l.LibraryBookId == libraryBookId && l.Status == LoanStatus.Active, cancellationToken);
    }

    public async Task<IReadOnlyList<Loan>> GetPendingForCopyAsync(Guid libraryBookId, CancellationToken cancellationToken = default)
    {
        return await context.Loans
            .Where(l => l.LibraryBookId == libraryBookId && l.Status == LoanStatus.Pending)
            .ToListAsync(cancellationToken);
    }

    public void Add(Loan loan)
    {
        context.Loans.Add(loan);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await context.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<Loan> WithDetails() =>
        context.Loans
            .Include(l => l.LibraryBook!).ThenInclude(lb => lb.Book)
            .Include(l => l.LibraryBook!).ThenInclude(lb => lb.Library)
            .Include(l => l.Lender)
            .Include(l => l.Borrower);
}
