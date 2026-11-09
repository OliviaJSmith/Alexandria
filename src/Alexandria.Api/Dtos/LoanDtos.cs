using System.ComponentModel.DataAnnotations;
using Alexandria.Api.Models;

namespace Alexandria.Api.Dtos;

public sealed record LoanDto(
    Guid Id,
    Guid LibraryBookId,
    Guid LibraryId,
    string LibraryName,
    BookDto Book,
    UserSummaryDto Lender,
    UserSummaryDto Borrower,
    LoanStatus Status,
    bool IsOverdue,
    DateTimeOffset RequestedAt,
    DateTimeOffset? LoanDate,
    DateTimeOffset? DueDate,
    DateTimeOffset? ReturnedDate)
{
    /// <summary>Maps a loan with LibraryBook (Book, Library), Lender and Borrower loaded. Overdue = active and past its due date.</summary>
    public static LoanDto FromLoan(Loan loan, DateTimeOffset now)
    {
        var copy = loan.LibraryBook ?? throw new InvalidOperationException("Loan.LibraryBook must be loaded before mapping.");
        return new LoanDto(
            loan.Id,
            loan.LibraryBookId,
            copy.LibraryId,
            copy.Library?.Name ?? string.Empty,
            BookDto.FromBook(copy.Book ?? throw new InvalidOperationException("LibraryBook.Book must be loaded before mapping.")),
            UserSummaryDto.FromUser(loan.Lender ?? throw new InvalidOperationException("Loan.Lender must be loaded before mapping.")),
            UserSummaryDto.FromUser(loan.Borrower ?? throw new InvalidOperationException("Loan.Borrower must be loaded before mapping.")),
            loan.Status,
            loan.Status == LoanStatus.Active && loan.DueDate is { } due && due < now,
            loan.CreatedAt,
            loan.LoanDate,
            loan.DueDate,
            loan.ReturnedDate);
    }
}

/// <summary>The owner lends a copy straight to a friend (the loan starts immediately).</summary>
public class LendBookRequest
{
    [Required]
    public Guid LibraryBookId { get; set; }

    [Required]
    public Guid BorrowerId { get; set; }

    public DateTimeOffset? DueDate { get; set; }
}

/// <summary>A friend asks to borrow a copy; the owner approves or declines.</summary>
public class BorrowRequest
{
    [Required]
    public Guid LibraryBookId { get; set; }

    public DateTimeOffset? DueDate { get; set; }
}
