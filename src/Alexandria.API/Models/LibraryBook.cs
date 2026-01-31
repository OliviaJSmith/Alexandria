namespace Alexandria.API.Models;

public class LibraryBook
{
    public int Id { get; set; }
    public int LibraryId { get; set; }
    public int BookId { get; set; }
    public BookStatus Status { get; set; }
    public string? LoanNote { get; set; }
    public DateTime AddedAt { get; set; }

    // Navigation properties
    public Library Library { get; set; } = null!;
    public Book Book { get; set; } = null!;
    public ICollection<Loan> Loans { get; set; } = new List<Loan>();
}

public enum BookStatus
{
    Available,
    CheckedOut,
    WaitingToBeLoanedOut
}
