using Alexandria.API.Data;
using Alexandria.API.DTOs;
using Alexandria.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Alexandria.API.Services;

public class LoanService(AlexandriaDbContext context, ILogger<LoanService> logger) : ILoanService
{
    public async Task<IEnumerable<LoanDto>> GetLoansAsync(int userId, string? filter)
    {
        var query = context.Loans
            .Include(l => l.LibraryBook)
                .ThenInclude(lb => lb.Book)
            .Include(l => l.Lender)
            .Include(l => l.Borrower)
            .AsQueryable();

        query = filter switch
        {
            "borrowed" => query.Where(l => l.BorrowerId == userId),
            "lent" => query.Where(l => l.LenderId == userId),
            _ => query.Where(l => l.BorrowerId == userId || l.LenderId == userId)
        };

        var loans = await query.ToListAsync();
        return loans.Select(MapToDto);
    }

    public async Task<LoanDto?> GetLoanByIdAsync(int id, int userId)
    {
        var loan = await context.Loans
            .Include(l => l.LibraryBook)
                .ThenInclude(lb => lb.Book)
            .Include(l => l.Lender)
            .Include(l => l.Borrower)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (loan is null)
            return null;

        if (loan.BorrowerId != userId && loan.LenderId != userId)
            return null;

        return MapToDto(loan);
    }

    public async Task<ServiceResult<LoanDto>> CreateLoanAsync(int userId, CreateLoanRequest request)
    {
        var libraryBook = await context.LibraryBooks
            .Include(lb => lb.Library)
            .Include(lb => lb.Book)
            .FirstOrDefaultAsync(lb => lb.Id == request.LibraryBookId);

        if (libraryBook is null)
            return ServiceResult<LoanDto>.Failure("Library book not found");

        if (libraryBook.Library.UserId != userId)
            return ServiceResult<LoanDto>.Failure("Forbidden");

        if (libraryBook.Status != BookStatus.Available)
            return ServiceResult<LoanDto>.Failure("Book is not available for loan");

        var borrower = await context.Users.FindAsync(request.BorrowerId);
        if (borrower is null)
            return ServiceResult<LoanDto>.Failure("Borrower not found");

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

        libraryBook.Status = BookStatus.CheckedOut;

        context.Loans.Add(loan);
        await context.SaveChangesAsync();

        loan = await context.Loans
            .Include(l => l.LibraryBook)
                .ThenInclude(lb => lb.Book)
            .Include(l => l.Lender)
            .Include(l => l.Borrower)
            .FirstAsync(l => l.Id == loan.Id);

        return ServiceResult<LoanDto>.Success(MapToDto(loan));
    }

    public async Task<ServiceResult<LoanDto>> UpdateLoanStatusAsync(int id, int userId, UpdateLoanStatusRequest request)
    {
        var loan = await context.Loans
            .Include(l => l.LibraryBook)
                .ThenInclude(lb => lb.Book)
            .Include(l => l.Lender)
            .Include(l => l.Borrower)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (loan is null)
            return ServiceResult<LoanDto>.Failure("Not found");

        if (loan.BorrowerId != userId && loan.LenderId != userId)
            return ServiceResult<LoanDto>.Failure("Forbidden");

        loan.Status = request.Status;
        loan.UpdatedAt = DateTime.UtcNow;

        if (request.Status == LoanStatus.Returned)
        {
            loan.ReturnedDate = DateTime.UtcNow;
            loan.LibraryBook.Status = BookStatus.Available;
        }

        await context.SaveChangesAsync();

        return ServiceResult<LoanDto>.Success(MapToDto(loan));
    }

    public async Task<bool> UserIsInvolvedInLoanAsync(int loanId, int userId)
    {
        var loan = await context.Loans.FindAsync(loanId);
        return loan is not null && (loan.BorrowerId == userId || loan.LenderId == userId);
    }

    private static LoanDto MapToDto(Loan loan) => new()
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
    };
}
