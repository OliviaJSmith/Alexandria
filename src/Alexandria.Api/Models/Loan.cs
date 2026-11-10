namespace Alexandria.Api.Models;

public enum LoanStatus
{
    /// <summary>A borrower has requested the book; the lender has not approved it yet.</summary>
    Pending,
    Active,
    Returned,
    Overdue,
    Cancelled,
}

public class Loan
{
    public Guid Id { get; set; }

    public Guid LibraryBookId { get; set; }

    public LibraryBook? LibraryBook { get; set; }

    public Guid LenderId { get; set; }

    public User? Lender { get; set; }

    public Guid BorrowerId { get; set; }

    public User? Borrower { get; set; }

    public LoanStatus Status { get; set; } = LoanStatus.Pending;

    public DateTimeOffset? LoanDate { get; set; }

    public DateTimeOffset? DueDate { get; set; }

    public DateTimeOffset? ReturnedDate { get; set; }

    /// <summary>When the borrower was sent the "due tomorrow" reminder (null = not yet).</summary>
    public DateTimeOffset? DueTomorrowReminderSentAt { get; set; }

    /// <summary>When the borrower was sent the "due today" reminder (null = not yet).</summary>
    public DateTimeOffset? DueTodayReminderSentAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
