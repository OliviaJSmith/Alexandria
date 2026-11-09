using Alexandria.Api.Dtos;
using Alexandria.Api.Models;
using Alexandria.Api.Repositories;

namespace Alexandria.Api.Services;

/// <summary>
/// Loans between friends. A borrower's request starts Pending; the lender approves (Active) or declines
/// (Cancelled); the borrower may cancel a pending request; the lender marks an active loan Returned.
/// An owner can also lend a copy straight to a friend (Active immediately). The copy's status follows:
/// Active → CheckedOut, Returned → Available.
/// </summary>
public interface ILoanService
{
    Task<ServiceResult<IReadOnlyList<LoanDto>>> GetLoansAsync(Guid userId, LoanRole role, LoanStatus? status, CancellationToken cancellationToken = default);

    Task<ServiceResult<LoanDto>> LendAsync(Guid lenderId, LendBookRequest request, CancellationToken cancellationToken = default);

    Task<ServiceResult<LoanDto>> RequestAsync(Guid borrowerId, BorrowRequest request, CancellationToken cancellationToken = default);

    Task<ServiceResult<LoanDto>> ApproveAsync(Guid lenderId, Guid loanId, CancellationToken cancellationToken = default);

    Task<ServiceResult<LoanDto>> DeclineAsync(Guid lenderId, Guid loanId, CancellationToken cancellationToken = default);

    Task<ServiceResult<LoanDto>> CancelAsync(Guid borrowerId, Guid loanId, CancellationToken cancellationToken = default);

    Task<ServiceResult<LoanDto>> ReturnAsync(Guid lenderId, Guid loanId, CancellationToken cancellationToken = default);
}
