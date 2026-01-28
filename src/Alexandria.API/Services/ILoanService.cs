using Alexandria.API.DTOs;

namespace Alexandria.API.Services;

public interface ILoanService
{
    Task<IEnumerable<LoanDto>> GetLoansAsync(int userId, string? filter);
    Task<LoanDto?> GetLoanByIdAsync(int id, int userId);
    Task<ServiceResult<LoanDto>> CreateLoanAsync(int userId, CreateLoanRequest request);
    Task<ServiceResult<LoanDto>> UpdateLoanStatusAsync(int id, int userId, UpdateLoanStatusRequest request);
    Task<bool> UserIsInvolvedInLoanAsync(int loanId, int userId);
}
