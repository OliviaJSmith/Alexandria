using Alexandria.API.DTOs;
using Alexandria.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alexandria.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LoansController(ILoanService loanService, ILogger<LoansController> logger) : BaseController
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<LoanDto>>> GetLoans([FromQuery] string? filter = null)
    {
        var userId = GetCurrentUserId();
        var loans = await loanService.GetLoansAsync(userId, filter);
        return Ok(loans);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<LoanDto>> GetLoan(int id)
    {
        var userId = GetCurrentUserId();
        var loan = await loanService.GetLoanByIdAsync(id, userId);

        if (loan is null)
            return NotFound();

        return Ok(loan);
    }

    [HttpPost]
    public async Task<ActionResult<LoanDto>> CreateLoan(CreateLoanRequest request)
    {
        var userId = GetCurrentUserId();
        var result = await loanService.CreateLoanAsync(userId, request);

        if (!result.IsSuccess)
        {
            return result.Error switch
            {
                "Library book not found" => NotFound(result.Error),
                "Borrower not found" => NotFound(result.Error),
                "Forbidden" => Forbid(),
                _ => BadRequest(result.Error)
            };
        }

        return CreatedAtAction(nameof(GetLoan), new { id = result.Data!.Id }, result.Data);
    }

    [HttpPatch("{id}/status")]
    public async Task<ActionResult<LoanDto>> UpdateLoanStatus(int id, UpdateLoanStatusRequest request)
    {
        var userId = GetCurrentUserId();
        var result = await loanService.UpdateLoanStatusAsync(id, userId, request);

        if (!result.IsSuccess)
        {
            return result.Error switch
            {
                "Not found" => NotFound(),
                "Forbidden" => Forbid(),
                _ => BadRequest(result.Error)
            };
        }

        return Ok(result.Data);
    }
}
