using Alexandria.API.Models;

namespace Alexandria.API.DTOs;

public class LoanDto
{
    public int Id { get; set; }
    public int LibraryBookId { get; set; }
    public int LenderId { get; set; }
    public string LenderName { get; set; } = string.Empty;
    public int BorrowerId { get; set; }
    public string BorrowerName { get; set; } = string.Empty;
    public DateTime LoanDate { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? ReturnedDate { get; set; }
    public LoanStatus Status { get; set; }
    public BookDto Book { get; set; } = null!;
}

public class CreateLoanRequest
{
    public int LibraryBookId { get; set; }
    public int BorrowerId { get; set; }
    public DateTime? DueDate { get; set; }
}

public class UpdateLoanStatusRequest
{
    public LoanStatus Status { get; set; }
}
