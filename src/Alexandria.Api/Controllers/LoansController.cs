using Alexandria.Api.Dtos;
using Alexandria.Api.Models;
using Alexandria.Api.Repositories;
using Alexandria.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alexandria.Api.Controllers;

[Route("api/loans")]
[Authorize]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public class LoansController(ILoanService loanService) : ApiControllerBase
{
    /// <summary>Loans the signed-in user is part of. role: Any (default), Lender or Borrower; status filters optionally.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<LoanDto>>(StatusCodes.Status200OK)]
    public Task<ActionResult<IReadOnlyList<LoanDto>>> GetLoans(
        [FromQuery] LoanRole role = LoanRole.Any,
        [FromQuery] LoanStatus? status = null,
        CancellationToken cancellationToken = default) =>
        ForCurrentUserAsync(userId => loanService.GetLoansAsync(userId, role, status, cancellationToken));

    /// <summary>Lends one of your copies straight to a friend.</summary>
    [HttpPost]
    [ProducesResponseType<LoanDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<ActionResult<LoanDto>> Lend([FromBody] LendBookRequest request, CancellationToken cancellationToken) =>
        ForCurrentUserAsync(
            userId => loanService.LendAsync(userId, request, cancellationToken),
            loan => StatusCode(StatusCodes.Status201Created, loan));

    /// <summary>Asks a friend to borrow one of their books.</summary>
    [HttpPost("requests")]
    [ProducesResponseType<LoanDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<ActionResult<LoanDto>> RequestToBorrow([FromBody] BorrowRequest request, CancellationToken cancellationToken) =>
        ForCurrentUserAsync(
            userId => loanService.RequestAsync(userId, request, cancellationToken),
            loan => StatusCode(StatusCodes.Status201Created, loan));

    /// <summary>Lender approves a pending request; the book goes out.</summary>
    [HttpPost("{loanId:guid}/approve")]
    [ProducesResponseType<LoanDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<ActionResult<LoanDto>> Approve(Guid loanId, CancellationToken cancellationToken) =>
        ForCurrentUserAsync(userId => loanService.ApproveAsync(userId, loanId, cancellationToken));

    /// <summary>Lender declines a pending request.</summary>
    [HttpPost("{loanId:guid}/decline")]
    [ProducesResponseType<LoanDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<LoanDto>> Decline(Guid loanId, CancellationToken cancellationToken) =>
        ForCurrentUserAsync(userId => loanService.DeclineAsync(userId, loanId, cancellationToken));

    /// <summary>Borrower withdraws a pending request.</summary>
    [HttpPost("{loanId:guid}/cancel")]
    [ProducesResponseType<LoanDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<LoanDto>> Cancel(Guid loanId, CancellationToken cancellationToken) =>
        ForCurrentUserAsync(userId => loanService.CancelAsync(userId, loanId, cancellationToken));

    /// <summary>Lender marks an active loan returned; the book is available again.</summary>
    [HttpPost("{loanId:guid}/return")]
    [ProducesResponseType<LoanDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<LoanDto>> Return(Guid loanId, CancellationToken cancellationToken) =>
        ForCurrentUserAsync(userId => loanService.ReturnAsync(userId, loanId, cancellationToken));
}
