using Alexandria.API.DTOs;
using Alexandria.API.Models;
using Alexandria.API.Services;

namespace Alexandria.API.Tests.Services;

public class LoanServiceTests : ServiceTestBase
{
    private readonly LoanService _sut;

    public LoanServiceTests()
    {
        var logger = CreateMockLogger<LoanService>();
        _sut = new LoanService(Context, logger.Object);
    }

    private async Task<Loan> SeedLoanAsync(int lenderId, int borrowerId, int libraryBookId, LoanStatus status = LoanStatus.Active)
    {
        var loan = new Loan
        {
            LibraryBookId = libraryBookId,
            LenderId = lenderId,
            BorrowerId = borrowerId,
            LoanDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(14),
            Status = status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        Context.Loans.Add(loan);
        await Context.SaveChangesAsync();
        return loan;
    }

    [Fact]
    public async Task GetLoansAsync_ReturnsLoansForUser()
    {
        // Arrange
        var lender = await SeedUserAsync(1);
        var borrower = await SeedUserAsync(2);
        await SeedBooksAsync();
        var library = await SeedLibraryAsync(lender.Id);
        var libraryBook = await SeedLibraryBookAsync(library.Id, 1, BookStatus.CheckedOut);
        await SeedLoanAsync(lender.Id, borrower.Id, libraryBook.Id);

        // Act
        var result = await _sut.GetLoansAsync(lender.Id, null);

        // Assert
        Assert.Single(result);
    }

    [Fact]
    public async Task GetLoansAsync_WithBorrowedFilter_ReturnsBorrowedLoans()
    {
        // Arrange
        var lender = await SeedUserAsync(1);
        var borrower = await SeedUserAsync(2);
        await SeedBooksAsync();
        var library = await SeedLibraryAsync(lender.Id);
        var libraryBook = await SeedLibraryBookAsync(library.Id, 1, BookStatus.CheckedOut);
        await SeedLoanAsync(lender.Id, borrower.Id, libraryBook.Id);

        // Act
        var result = await _sut.GetLoansAsync(borrower.Id, "borrowed");

        // Assert
        Assert.Single(result);
    }

    [Fact]
    public async Task GetLoansAsync_WithLentFilter_ReturnsLentLoans()
    {
        // Arrange
        var lender = await SeedUserAsync(1);
        var borrower = await SeedUserAsync(2);
        await SeedBooksAsync();
        var library = await SeedLibraryAsync(lender.Id);
        var libraryBook = await SeedLibraryBookAsync(library.Id, 1, BookStatus.CheckedOut);
        await SeedLoanAsync(lender.Id, borrower.Id, libraryBook.Id);

        // Act
        var result = await _sut.GetLoansAsync(lender.Id, "lent");

        // Assert
        Assert.Single(result);
    }

    [Fact]
    public async Task GetLoanByIdAsync_WithValidId_ReturnsLoan()
    {
        // Arrange
        var lender = await SeedUserAsync(1);
        var borrower = await SeedUserAsync(2);
        await SeedBooksAsync();
        var library = await SeedLibraryAsync(lender.Id);
        var libraryBook = await SeedLibraryBookAsync(library.Id, 1, BookStatus.CheckedOut);
        var loan = await SeedLoanAsync(lender.Id, borrower.Id, libraryBook.Id);

        // Act
        var result = await _sut.GetLoanByIdAsync(loan.Id, lender.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(loan.Id, result.Id);
    }

    [Fact]
    public async Task GetLoanByIdAsync_WithUnauthorizedUser_ReturnsNull()
    {
        // Arrange
        var lender = await SeedUserAsync(1);
        var borrower = await SeedUserAsync(2);
        var otherUser = await SeedUserAsync(3);
        await SeedBooksAsync();
        var library = await SeedLibraryAsync(lender.Id);
        var libraryBook = await SeedLibraryBookAsync(library.Id, 1, BookStatus.CheckedOut);
        var loan = await SeedLoanAsync(lender.Id, borrower.Id, libraryBook.Id);

        // Act
        var result = await _sut.GetLoanByIdAsync(loan.Id, otherUser.Id);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateLoanAsync_CreatesLoanSuccessfully()
    {
        // Arrange
        var lender = await SeedUserAsync(1);
        var borrower = await SeedUserAsync(2);
        await SeedBooksAsync();
        var library = await SeedLibraryAsync(lender.Id);
        var libraryBook = await SeedLibraryBookAsync(library.Id, 1, BookStatus.Available);
        var request = new CreateLoanRequest
        {
            LibraryBookId = libraryBook.Id,
            BorrowerId = borrower.Id,
            DueDate = DateTime.UtcNow.AddDays(14)
        };

        // Act
        var result = await _sut.CreateLoanAsync(lender.Id, request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(LoanStatus.Active, result.Data.Status);

        // Verify book status changed
        var updatedLibraryBook = await Context.LibraryBooks.FindAsync(libraryBook.Id);
        Assert.Equal(BookStatus.CheckedOut, updatedLibraryBook!.Status);
    }

    [Fact]
    public async Task CreateLoanAsync_WithNonExistentBook_ReturnsError()
    {
        // Arrange
        var lender = await SeedUserAsync(1);
        var borrower = await SeedUserAsync(2);
        var request = new CreateLoanRequest
        {
            LibraryBookId = 999,
            BorrowerId = borrower.Id
        };

        // Act
        var result = await _sut.CreateLoanAsync(lender.Id, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Library book not found", result.Error);
    }

    [Fact]
    public async Task CreateLoanAsync_WithNonOwner_ReturnsForbidden()
    {
        // Arrange
        var owner = await SeedUserAsync(1);
        var nonOwner = await SeedUserAsync(2);
        var borrower = await SeedUserAsync(3);
        await SeedBooksAsync();
        var library = await SeedLibraryAsync(owner.Id);
        var libraryBook = await SeedLibraryBookAsync(library.Id, 1, BookStatus.Available);
        var request = new CreateLoanRequest
        {
            LibraryBookId = libraryBook.Id,
            BorrowerId = borrower.Id
        };

        // Act
        var result = await _sut.CreateLoanAsync(nonOwner.Id, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Forbidden", result.Error);
    }

    [Fact]
    public async Task CreateLoanAsync_WithUnavailableBook_ReturnsError()
    {
        // Arrange
        var lender = await SeedUserAsync(1);
        var borrower = await SeedUserAsync(2);
        await SeedBooksAsync();
        var library = await SeedLibraryAsync(lender.Id);
        var libraryBook = await SeedLibraryBookAsync(library.Id, 1, BookStatus.CheckedOut);
        var request = new CreateLoanRequest
        {
            LibraryBookId = libraryBook.Id,
            BorrowerId = borrower.Id
        };

        // Act
        var result = await _sut.CreateLoanAsync(lender.Id, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Book is not available for loan", result.Error);
    }

    [Fact]
    public async Task UpdateLoanStatusAsync_UpdatesStatusSuccessfully()
    {
        // Arrange
        var lender = await SeedUserAsync(1);
        var borrower = await SeedUserAsync(2);
        await SeedBooksAsync();
        var library = await SeedLibraryAsync(lender.Id);
        var libraryBook = await SeedLibraryBookAsync(library.Id, 1, BookStatus.CheckedOut);
        var loan = await SeedLoanAsync(lender.Id, borrower.Id, libraryBook.Id);
        var request = new UpdateLoanStatusRequest { Status = LoanStatus.Returned };

        // Act
        var result = await _sut.UpdateLoanStatusAsync(loan.Id, lender.Id, request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(LoanStatus.Returned, result.Data.Status);
        Assert.NotNull(result.Data.ReturnedDate);

        // Verify book status changed back to available
        var updatedLibraryBook = await Context.LibraryBooks.FindAsync(libraryBook.Id);
        Assert.Equal(BookStatus.Available, updatedLibraryBook!.Status);
    }

    [Fact]
    public async Task UpdateLoanStatusAsync_WithUnauthorizedUser_ReturnsForbidden()
    {
        // Arrange
        var lender = await SeedUserAsync(1);
        var borrower = await SeedUserAsync(2);
        var otherUser = await SeedUserAsync(3);
        await SeedBooksAsync();
        var library = await SeedLibraryAsync(lender.Id);
        var libraryBook = await SeedLibraryBookAsync(library.Id, 1, BookStatus.CheckedOut);
        var loan = await SeedLoanAsync(lender.Id, borrower.Id, libraryBook.Id);
        var request = new UpdateLoanStatusRequest { Status = LoanStatus.Returned };

        // Act
        var result = await _sut.UpdateLoanStatusAsync(loan.Id, otherUser.Id, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Forbidden", result.Error);
    }

    [Fact]
    public async Task UserIsInvolvedInLoanAsync_ReturnsTrueForLender()
    {
        // Arrange
        var lender = await SeedUserAsync(1);
        var borrower = await SeedUserAsync(2);
        await SeedBooksAsync();
        var library = await SeedLibraryAsync(lender.Id);
        var libraryBook = await SeedLibraryBookAsync(library.Id, 1, BookStatus.CheckedOut);
        var loan = await SeedLoanAsync(lender.Id, borrower.Id, libraryBook.Id);

        // Act
        var result = await _sut.UserIsInvolvedInLoanAsync(loan.Id, lender.Id);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task UserIsInvolvedInLoanAsync_ReturnsTrueForBorrower()
    {
        // Arrange
        var lender = await SeedUserAsync(1);
        var borrower = await SeedUserAsync(2);
        await SeedBooksAsync();
        var library = await SeedLibraryAsync(lender.Id);
        var libraryBook = await SeedLibraryBookAsync(library.Id, 1, BookStatus.CheckedOut);
        var loan = await SeedLoanAsync(lender.Id, borrower.Id, libraryBook.Id);

        // Act
        var result = await _sut.UserIsInvolvedInLoanAsync(loan.Id, borrower.Id);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task UserIsInvolvedInLoanAsync_ReturnsFalseForUnrelatedUser()
    {
        // Arrange
        var lender = await SeedUserAsync(1);
        var borrower = await SeedUserAsync(2);
        var otherUser = await SeedUserAsync(3);
        await SeedBooksAsync();
        var library = await SeedLibraryAsync(lender.Id);
        var libraryBook = await SeedLibraryBookAsync(library.Id, 1, BookStatus.CheckedOut);
        var loan = await SeedLoanAsync(lender.Id, borrower.Id, libraryBook.Id);

        // Act
        var result = await _sut.UserIsInvolvedInLoanAsync(loan.Id, otherUser.Id);

        // Assert
        Assert.False(result);
    }
}
