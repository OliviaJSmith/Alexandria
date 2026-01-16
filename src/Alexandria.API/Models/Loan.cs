namespace Alexandria.API.Models;

public class Loan
{
    public int Id { get; set; }
    public int LibraryBookId { get; set; }
    public int LenderId { get; set; }
    public int BorrowerId { get; set; }
    public DateTime LoanDate { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? ReturnedDate { get; set; }
    public LoanStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public LibraryBook LibraryBook { get; set; } = null!;
    public User Lender { get; set; } = null!;
    public User Borrower { get; set; } = null!;
}

public enum LoanStatus
{
    Pending,
    Active,
    Returned,
    Overdue,
    Cancelled
}
