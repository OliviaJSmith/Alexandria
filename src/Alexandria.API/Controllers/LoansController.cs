using Alexandria.API.Data;
using Alexandria.API.DTOs;
using Alexandria.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Alexandria.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LoansController : BaseController
{
    private readonly AlexandriaDbContext _context;
    private readonly ILogger<LoansController> _logger;

    public LoansController(AlexandriaDbContext context, ILogger<LoansController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LoanDto>>> GetLoans([FromQuery] string? filter = null)
    {
        var userId = GetCurrentUserId();
        var query = _context.Loans
            .Include(l => l.LibraryBook)
                .ThenInclude(lb => lb.Book)
            .Include(l => l.Lender)
            .Include(l => l.Borrower)
            .AsQueryable();

        // Filter by user involvement
        query = filter switch
        {
            "borrowed" => query.Where(l => l.BorrowerId == userId),
            "lent" => query.Where(l => l.LenderId == userId),
            _ => query.Where(l => l.BorrowerId == userId || l.LenderId == userId)
        };

        var loans = await query.ToListAsync();

        return Ok(loans.Select(l => new LoanDto
        {
            Id = l.Id,
            LibraryBookId = l.LibraryBookId,
            LenderId = l.LenderId,
            LenderName = l.Lender.Name,
            BorrowerId = l.BorrowerId,
            BorrowerName = l.Borrower.Name,
            LoanDate = l.LoanDate,
            DueDate = l.DueDate,
            ReturnedDate = l.ReturnedDate,
            Status = l.Status,
            Book = new BookDto
            {
                Id = l.LibraryBook.Book.Id,
                Title = l.LibraryBook.Book.Title,
                Author = l.LibraryBook.Book.Author,
                Isbn = l.LibraryBook.Book.Isbn,
                Publisher = l.LibraryBook.Book.Publisher,
                PublishedYear = l.LibraryBook.Book.PublishedYear,
                Description = l.LibraryBook.Book.Description,
                CoverImageUrl = l.LibraryBook.Book.CoverImageUrl,
                Genre = l.LibraryBook.Book.Genre,
                PageCount = l.LibraryBook.Book.PageCount
            }
        }));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<LoanDto>> GetLoan(int id)
    {
        var userId = GetCurrentUserId();
        var loan = await _context.Loans
            .Include(l => l.LibraryBook)
                .ThenInclude(lb => lb.Book)
            .Include(l => l.Lender)
            .Include(l => l.Borrower)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (loan == null)
        {
            return NotFound();
        }

        // Check if user is involved in this loan
        if (loan.BorrowerId != userId && loan.LenderId != userId)
        {
            return Forbid();
        }

        return Ok(new LoanDto
        {
            Id = loan.Id,
            LibraryBookId = loan.LibraryBookId,
            LenderId = loan.LenderId,
            LenderName = loan.Lender.Name,
            BorrowerId = loan.BorrowerId,
            BorrowerName = loan.Borrower.Name,
            LoanDate = loan.LoanDate,
            DueDate = loan.DueDate,
            ReturnedDate = loan.ReturnedDate,
            Status = loan.Status,
            Book = new BookDto
            {
                Id = loan.LibraryBook.Book.Id,
                Title = loan.LibraryBook.Book.Title,
                Author = loan.LibraryBook.Book.Author,
                Isbn = loan.LibraryBook.Book.Isbn,
                Publisher = loan.LibraryBook.Book.Publisher,
                PublishedYear = loan.LibraryBook.Book.PublishedYear,
                Description = loan.LibraryBook.Book.Description,
                CoverImageUrl = loan.LibraryBook.Book.CoverImageUrl,
                Genre = loan.LibraryBook.Book.Genre,
                PageCount = loan.LibraryBook.Book.PageCount
            }
        });
    }

    [HttpPost]
    public async Task<ActionResult<LoanDto>> CreateLoan(CreateLoanRequest request)
    {
        var userId = GetCurrentUserId();

        var libraryBook = await _context.LibraryBooks
            .Include(lb => lb.Library)
            .Include(lb => lb.Book)
            .FirstOrDefaultAsync(lb => lb.Id == request.LibraryBookId);

        if (libraryBook == null)
        {
            return NotFound("Library book not found");
        }

        // Check if user owns the library
        if (libraryBook.Library.UserId != userId)
        {
            return Forbid();
        }

        // Check if book is available
        if (libraryBook.Status != BookStatus.Available)
        {
            return BadRequest("Book is not available for loan");
        }

        var borrower = await _context.Users.FindAsync(request.BorrowerId);
        if (borrower == null)
        {
            return NotFound("Borrower not found");
        }

        var loan = new Loan
        {
            LibraryBookId = request.LibraryBookId,
            LenderId = userId,
            BorrowerId = request.BorrowerId,
            LoanDate = DateTime.UtcNow,
            DueDate = request.DueDate,
            Status = LoanStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Update library book status
        libraryBook.Status = BookStatus.CheckedOut;

        _context.Loans.Add(loan);
        await _context.SaveChangesAsync();

        // Reload with includes
        loan = await _context.Loans
            .Include(l => l.LibraryBook)
                .ThenInclude(lb => lb.Book)
            .Include(l => l.Lender)
            .Include(l => l.Borrower)
            .FirstAsync(l => l.Id == loan.Id);

        return CreatedAtAction(nameof(GetLoan), new { id = loan.Id }, new LoanDto
        {
            Id = loan.Id,
            LibraryBookId = loan.LibraryBookId,
            LenderId = loan.LenderId,
            LenderName = loan.Lender.Name,
            BorrowerId = loan.BorrowerId,
            BorrowerName = loan.Borrower.Name,
            LoanDate = loan.LoanDate,
            DueDate = loan.DueDate,
            ReturnedDate = loan.ReturnedDate,
            Status = loan.Status,
            Book = new BookDto
            {
                Id = loan.LibraryBook.Book.Id,
                Title = loan.LibraryBook.Book.Title,
                Author = loan.LibraryBook.Book.Author,
                Isbn = loan.LibraryBook.Book.Isbn,
                Publisher = loan.LibraryBook.Book.Publisher,
                PublishedYear = loan.LibraryBook.Book.PublishedYear,
                Description = loan.LibraryBook.Book.Description,
                CoverImageUrl = loan.LibraryBook.Book.CoverImageUrl,
                Genre = loan.LibraryBook.Book.Genre,
                PageCount = loan.LibraryBook.Book.PageCount
            }
        });
    }

    [HttpPatch("{id}/status")]
    public async Task<ActionResult<LoanDto>> UpdateLoanStatus(int id, UpdateLoanStatusRequest request)
    {
        var userId = GetCurrentUserId();
        var loan = await _context.Loans
            .Include(l => l.LibraryBook)
                .ThenInclude(lb => lb.Book)
            .Include(l => l.Lender)
            .Include(l => l.Borrower)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (loan == null)
        {
            return NotFound();
        }

        // Check if user is involved in this loan
        if (loan.BorrowerId != userId && loan.LenderId != userId)
        {
            return Forbid();
        }

        loan.Status = request.Status;
        loan.UpdatedAt = DateTime.UtcNow;

        // If loan is returned, update library book status
        if (request.Status == LoanStatus.Returned)
        {
            loan.ReturnedDate = DateTime.UtcNow;
            loan.LibraryBook.Status = BookStatus.Available;
        }

        await _context.SaveChangesAsync();

        return Ok(new LoanDto
        {
            Id = loan.Id,
            LibraryBookId = loan.LibraryBookId,
            LenderId = loan.LenderId,
            LenderName = loan.Lender.Name,
            BorrowerId = loan.BorrowerId,
            BorrowerName = loan.Borrower.Name,
            LoanDate = loan.LoanDate,
            DueDate = loan.DueDate,
            ReturnedDate = loan.ReturnedDate,
            Status = loan.Status,
            Book = new BookDto
            {
                Id = loan.LibraryBook.Book.Id,
                Title = loan.LibraryBook.Book.Title,
                Author = loan.LibraryBook.Book.Author,
                Isbn = loan.LibraryBook.Book.Isbn,
                Publisher = loan.LibraryBook.Book.Publisher,
                PublishedYear = loan.LibraryBook.Book.PublishedYear,
                Description = loan.LibraryBook.Book.Description,
                CoverImageUrl = loan.LibraryBook.Book.CoverImageUrl,
                Genre = loan.LibraryBook.Book.Genre,
                PageCount = loan.LibraryBook.Book.PageCount
            }
        });
    }
}
