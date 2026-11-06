namespace Alexandria.Api.Models;

public enum LibraryBookStatus
{
    Available,
    CheckedOut,
    WaitingToBeLoanedOut,
}

/// <summary>A physical copy of a <see cref="Book"/> held in a <see cref="Library"/>.</summary>
public class LibraryBook
{
    public Guid Id { get; set; }

    public Guid LibraryId { get; set; }

    public Library? Library { get; set; }

    public Guid BookId { get; set; }

    public Book? Book { get; set; }

    public LibraryBookStatus Status { get; set; } = LibraryBookStatus.Available;

    /// <summary>Free-text note about who has the book, for loans to people outside Alexandria.</summary>
    public string? LoanNote { get; set; }

    public DateTimeOffset AddedAt { get; set; }

    public ICollection<Loan> Loans { get; set; } = [];
}
