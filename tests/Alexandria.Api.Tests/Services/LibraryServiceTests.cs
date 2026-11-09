using Alexandria.Api.Dtos;
using Alexandria.Api.Models;
using Alexandria.Api.Repositories;
using Alexandria.Api.Services;
using Moq;

namespace Alexandria.Api.Tests.Services;

public class LibraryServiceTests
{
    private readonly Mock<ILibraryRepository> _libraries = new();
    private readonly Mock<IBookRepository> _books = new();
    private readonly Mock<IFriendshipRepository> _friendships = new();
    private readonly Mock<ILoanRepository> _loans = new();
    private readonly LibraryService _service;

    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly Guid _friendId = Guid.NewGuid();
    private readonly Guid _strangerId = Guid.NewGuid();

    public LibraryServiceTests()
    {
        _friendships.Setup(f => f.AreFriendsAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _friendships.Setup(f => f.AreFriendsAsync(_ownerId, _friendId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _service = new LibraryService(_libraries.Object, _books.Object, _friendships.Object, _loans.Object, TestData.TimeProviderAt(TestData.Now).Object);
    }

    // ---------- Visibility ----------

    [Theory]
    [InlineData(false, "owner", true)]
    [InlineData(true, "owner", true)]
    [InlineData(true, "friend", true)]
    [InlineData(false, "friend", false)]
    [InlineData(true, "stranger", false)]
    [InlineData(false, "stranger", false)]
    public async Task GetLibrary_AppliesVisibilityRules(bool isPublic, string viewer, bool visible)
    {
        var library = SetupLibrary(isPublic);

        var result = await _service.GetLibraryAsync(library.Id, Viewer(viewer));

        Assert.Equal(visible, result.IsSuccess);
        if (visible)
        {
            Assert.Equal(library.Id, result.Value!.Id);
            Assert.Equal(library.Name, result.Value.Name);
        }
        else
        {
            Assert.Equal(ServiceErrorType.NotFound, result.Error!.Type);
        }
    }

    [Fact]
    public async Task GetLibrary_Missing_ReturnsNotFound()
    {
        var result = await _service.GetLibraryAsync(Guid.NewGuid(), _ownerId);

        Assert.Equal(ServiceErrorType.NotFound, result.Error!.Type);
    }

    [Fact]
    public async Task GetMyLibraries_ReturnsOwnedLibraries()
    {
        var owned = new[] { TestData.Library(_ownerId, name: "Attic"), TestData.Library(_ownerId, name: "Den") };
        _libraries.Setup(l => l.GetByOwnerAsync(_ownerId, It.IsAny<CancellationToken>())).ReturnsAsync(owned);

        var result = await _service.GetMyLibrariesAsync(_ownerId);

        Assert.Equal(["Attic", "Den"], result.Value!.Select(l => l.Name));
    }

    // ---------- Create / update ----------

    [Fact]
    public async Task CreateLibrary_TrimsNameAndSaves()
    {
        Library? added = null;
        _libraries.Setup(l => l.Add(It.IsAny<Library>())).Callback<Library>(l => added = l);

        var result = await _service.CreateLibraryAsync(_ownerId, new CreateLibraryRequest { Name = "  Study  ", IsPublic = true });

        Assert.True(result.IsSuccess);
        Assert.NotNull(added);
        Assert.Equal("Study", added.Name);
        Assert.True(added.IsPublic);
        Assert.Equal(_ownerId, added.OwnerId);
        Assert.Equal(TestData.Now, added.CreatedAt);
        Assert.Equal(added.Id, result.Value!.Id);
        _libraries.Verify(l => l.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateLibrary_BlankName_ReturnsValidationError()
    {
        var result = await _service.CreateLibraryAsync(_ownerId, new CreateLibraryRequest { Name = "   " });

        Assert.Equal(ServiceErrorType.Validation, result.Error!.Type);
        _libraries.Verify(l => l.Add(It.IsAny<Library>()), Times.Never);
    }

    [Fact]
    public async Task UpdateLibrary_Owner_ChangesNameAndVisibility()
    {
        var library = SetupLibrary(isPublic: false);

        var result = await _service.UpdateLibraryAsync(library.Id, _ownerId, new UpdateLibraryRequest { Name = " Attic ", IsPublic = true });

        Assert.True(result.IsSuccess);
        Assert.Equal("Attic", library.Name);
        Assert.True(library.IsPublic);
        Assert.Equal(TestData.Now, library.UpdatedAt);
        _libraries.Verify(l => l.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateLibrary_NullFields_LeavesThemUnchanged()
    {
        var library = SetupLibrary(isPublic: true);

        var result = await _service.UpdateLibraryAsync(library.Id, _ownerId, new UpdateLibraryRequest());

        Assert.True(result.IsSuccess);
        Assert.Equal("Living Room", library.Name);
        Assert.True(library.IsPublic);
    }

    [Fact]
    public async Task UpdateLibrary_BlankName_ReturnsValidationError()
    {
        var library = SetupLibrary();

        var result = await _service.UpdateLibraryAsync(library.Id, _ownerId, new UpdateLibraryRequest { Name = " " });

        Assert.Equal(ServiceErrorType.Validation, result.Error!.Type);
        _libraries.Verify(l => l.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateLibrary_FriendOfPublicLibrary_IsForbidden()
    {
        var library = SetupLibrary(isPublic: true);

        var result = await _service.UpdateLibraryAsync(library.Id, _friendId, new UpdateLibraryRequest { IsPublic = false });

        Assert.Equal(ServiceErrorType.Forbidden, result.Error!.Type);
        Assert.True(library.IsPublic);
    }

    [Fact]
    public async Task UpdateLibrary_StrangerOfPrivateLibrary_IsNotFound()
    {
        var library = SetupLibrary(isPublic: false);

        var result = await _service.UpdateLibraryAsync(library.Id, _strangerId, new UpdateLibraryRequest { Name = "Mine now" });

        Assert.Equal(ServiceErrorType.NotFound, result.Error!.Type);
    }

    // ---------- Books in a library ----------

    [Fact]
    public async Task GetLibraryBooks_Visible_ReturnsCopies()
    {
        var library = SetupLibrary(isPublic: true);
        var copy = TestData.Copy(library, TestData.Book());
        _libraries.Setup(l => l.GetLibraryBooksAsync(library.Id, It.IsAny<CancellationToken>())).ReturnsAsync([copy]);

        var result = await _service.GetLibraryBooksAsync(library.Id, _friendId);

        var dto = Assert.Single(result.Value!);
        Assert.Equal(copy.Id, dto.Id);
        Assert.Equal("The Hobbit", dto.Book.Title);
    }

    [Fact]
    public async Task GetLibraryBooks_NotVisible_ReturnsNotFoundWithoutLoadingBooks()
    {
        var library = SetupLibrary(isPublic: false);

        var result = await _service.GetLibraryBooksAsync(library.Id, _friendId);

        Assert.Equal(ServiceErrorType.NotFound, result.Error!.Type);
        _libraries.Verify(l => l.GetLibraryBooksAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetLentOutBooks_ReturnsCheckedOutCopies()
    {
        var copy = TestData.Copy(TestData.Library(_ownerId), TestData.Book(), LibraryBookStatus.CheckedOut);
        _libraries.Setup(l => l.GetOwnerBooksByStatusAsync(_ownerId, LibraryBookStatus.CheckedOut, It.IsAny<CancellationToken>()))
            .ReturnsAsync([copy]);

        var result = await _service.GetLentOutBooksAsync(_ownerId);

        Assert.Equal(LibraryBookStatus.CheckedOut, Assert.Single(result.Value!).Status);
    }

    [Fact]
    public async Task AddBook_NewBook_AddsCopy()
    {
        var library = SetupLibrary();
        var book = SetupBook();
        LibraryBook? added = null;
        _libraries.Setup(l => l.AddLibraryBook(It.IsAny<LibraryBook>())).Callback<LibraryBook>(c => added = c);

        var result = await _service.AddBookAsync(library.Id, _ownerId, new AddLibraryBookRequest { BookId = book.Id, Status = LibraryBookStatus.WaitingToBeLoanedOut });

        Assert.True(result.IsSuccess);
        Assert.NotNull(added);
        Assert.Equal(library.Id, added.LibraryId);
        Assert.Equal(book.Id, added.BookId);
        Assert.Equal(LibraryBookStatus.WaitingToBeLoanedOut, added.Status);
        Assert.Equal(TestData.Now, added.AddedAt);
        Assert.Equal(book.Title, result.Value!.Book.Title);
        _libraries.Verify(l => l.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddBook_AlreadyInLibrary_ReturnsConflict()
    {
        var library = SetupLibrary();
        var book = SetupBook();
        _libraries.Setup(l => l.ContainsBookAsync(library.Id, book.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _service.AddBookAsync(library.Id, _ownerId, new AddLibraryBookRequest { BookId = book.Id });

        Assert.Equal(ServiceErrorType.Conflict, result.Error!.Type);
        _libraries.Verify(l => l.AddLibraryBook(It.IsAny<LibraryBook>()), Times.Never);
    }

    [Fact]
    public async Task AddBook_AlreadyInLibraryWithForceAdd_AddsAnotherCopy()
    {
        var library = SetupLibrary();
        var book = SetupBook();
        _libraries.Setup(l => l.ContainsBookAsync(library.Id, book.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _service.AddBookAsync(library.Id, _ownerId, new AddLibraryBookRequest { BookId = book.Id, ForceAdd = true });

        Assert.True(result.IsSuccess);
        _libraries.Verify(l => l.AddLibraryBook(It.IsAny<LibraryBook>()), Times.Once);
    }

    [Fact]
    public async Task AddBook_UnknownBook_ReturnsNotFound()
    {
        var library = SetupLibrary();

        var result = await _service.AddBookAsync(library.Id, _ownerId, new AddLibraryBookRequest { BookId = Guid.NewGuid() });

        Assert.Equal(ServiceErrorType.NotFound, result.Error!.Type);
        Assert.Equal(LibraryService.BookNotFoundMessage, result.Error.Message);
    }

    [Fact]
    public async Task AddBook_NotOwner_IsForbidden()
    {
        var library = SetupLibrary(isPublic: true);

        var result = await _service.AddBookAsync(library.Id, _friendId, new AddLibraryBookRequest { BookId = Guid.NewGuid() });

        Assert.Equal(ServiceErrorType.Forbidden, result.Error!.Type);
    }

    [Fact]
    public async Task UpdateLibraryBook_SetsStatusNoteAndGenre()
    {
        var (library, copy) = SetupCopy();

        var result = await _service.UpdateLibraryBookAsync(library.Id, copy.Id, _ownerId, new UpdateLibraryBookRequest
        {
            Status = LibraryBookStatus.CheckedOut,
            LoanNote = "  Lent to Sam  ",
            Genre = " Classics ",
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(LibraryBookStatus.CheckedOut, copy.Status);
        Assert.Equal("Lent to Sam", copy.LoanNote);
        Assert.Equal("Classics", copy.Book!.Genre);
        Assert.Equal(TestData.Now, copy.Book.UpdatedAt);
        _libraries.Verify(l => l.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateLibraryBook_StatusChangeDuringActiveLoan_ReturnsConflict()
    {
        var (library, copy) = SetupCopy(status: LibraryBookStatus.CheckedOut);
        _loans.Setup(l => l.HasActiveLoanAsync(copy.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _service.UpdateLibraryBookAsync(library.Id, copy.Id, _ownerId, new UpdateLibraryBookRequest { Status = LibraryBookStatus.Available });

        Assert.Equal(ServiceErrorType.Conflict, result.Error!.Type);
        Assert.Equal(LibraryService.OnActiveLoanMessage, result.Error.Message);
        Assert.Equal(LibraryBookStatus.CheckedOut, copy.Status);
    }

    [Fact]
    public async Task UpdateLibraryBook_SameStatus_DoesNotCheckLoans()
    {
        var (library, copy) = SetupCopy();

        var result = await _service.UpdateLibraryBookAsync(library.Id, copy.Id, _ownerId, new UpdateLibraryBookRequest { Status = LibraryBookStatus.Available });

        Assert.True(result.IsSuccess);
        _loans.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateLibraryBook_EmptyStrings_ClearNoteAndGenre_NullsLeaveFieldsAlone()
    {
        var (library, copy) = SetupCopy();
        copy.LoanNote = "Old note";

        await _service.UpdateLibraryBookAsync(library.Id, copy.Id, _ownerId, new UpdateLibraryBookRequest { LoanNote = "", Genre = "" });

        Assert.Null(copy.LoanNote);
        Assert.Null(copy.Book!.Genre);
        Assert.Equal(LibraryBookStatus.Available, copy.Status);
    }

    [Fact]
    public async Task UpdateLibraryBook_UnknownCopy_ReturnsNotFound()
    {
        var library = SetupLibrary();

        var result = await _service.UpdateLibraryBookAsync(library.Id, Guid.NewGuid(), _ownerId, new UpdateLibraryBookRequest());

        Assert.Equal(ServiceErrorType.NotFound, result.Error!.Type);
        Assert.Equal(LibraryService.LibraryBookNotFoundMessage, result.Error.Message);
    }

    [Fact]
    public async Task UpdateLibraryBook_NotOwner_IsForbidden()
    {
        var (library, copy) = SetupCopy(isPublic: true);

        var result = await _service.UpdateLibraryBookAsync(library.Id, copy.Id, _friendId, new UpdateLibraryBookRequest { LoanNote = "x" });

        Assert.Equal(ServiceErrorType.Forbidden, result.Error!.Type);
        Assert.Null(copy.LoanNote);
    }

    [Fact]
    public async Task RemoveLibraryBook_Available_RemovesCopy()
    {
        var (library, copy) = SetupCopy();

        var result = await _service.RemoveLibraryBookAsync(library.Id, copy.Id, _ownerId);

        Assert.True(result.IsSuccess);
        _libraries.Verify(l => l.RemoveLibraryBook(copy), Times.Once);
        _libraries.Verify(l => l.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveLibraryBook_LentOut_ReturnsConflict()
    {
        var (library, copy) = SetupCopy(status: LibraryBookStatus.CheckedOut);

        var result = await _service.RemoveLibraryBookAsync(library.Id, copy.Id, _ownerId);

        Assert.Equal(ServiceErrorType.Conflict, result.Error!.Type);
        _libraries.Verify(l => l.RemoveLibraryBook(It.IsAny<LibraryBook>()), Times.Never);
    }

    [Fact]
    public async Task RemoveLibraryBook_StrangerOfPrivateLibrary_IsNotFound()
    {
        var (library, copy) = SetupCopy();

        var result = await _service.RemoveLibraryBookAsync(library.Id, copy.Id, _strangerId);

        Assert.Equal(ServiceErrorType.NotFound, result.Error!.Type);
    }

    [Fact]
    public async Task MoveLibraryBook_ToOwnedLibrary_MovesCopy()
    {
        var (library, copy) = SetupCopy();
        var target = SetupLibrary(name: "Attic");

        var result = await _service.MoveLibraryBookAsync(library.Id, copy.Id, _ownerId, new MoveLibraryBookRequest { TargetLibraryId = target.Id });

        Assert.True(result.IsSuccess);
        Assert.Equal(target.Id, copy.LibraryId);
        Assert.Equal(target.Id, result.Value!.LibraryId);
        _libraries.Verify(l => l.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MoveLibraryBook_ToFriendsLibrary_IsForbidden()
    {
        var (library, copy) = SetupCopy();
        var friendsLibrary = TestData.Library(_friendId, isPublic: true);
        _libraries.Setup(l => l.GetByIdAsync(friendsLibrary.Id, It.IsAny<CancellationToken>())).ReturnsAsync(friendsLibrary);
        _friendships.Setup(f => f.AreFriendsAsync(_friendId, _ownerId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _service.MoveLibraryBookAsync(library.Id, copy.Id, _ownerId, new MoveLibraryBookRequest { TargetLibraryId = friendsLibrary.Id });

        Assert.Equal(ServiceErrorType.Forbidden, result.Error!.Type);
        Assert.Equal(library.Id, copy.LibraryId);
    }

    [Fact]
    public async Task MoveLibraryBook_UnknownTarget_IsNotFound()
    {
        var (library, copy) = SetupCopy();

        var result = await _service.MoveLibraryBookAsync(library.Id, copy.Id, _ownerId, new MoveLibraryBookRequest { TargetLibraryId = Guid.NewGuid() });

        Assert.Equal(ServiceErrorType.NotFound, result.Error!.Type);
        Assert.Equal(library.Id, copy.LibraryId);
    }

    [Fact]
    public async Task MoveLibraryBook_LentOut_ReturnsConflict()
    {
        var (library, copy) = SetupCopy(status: LibraryBookStatus.CheckedOut);
        var target = SetupLibrary(name: "Attic");

        var result = await _service.MoveLibraryBookAsync(library.Id, copy.Id, _ownerId, new MoveLibraryBookRequest { TargetLibraryId = target.Id });

        Assert.Equal(ServiceErrorType.Conflict, result.Error!.Type);
        Assert.Equal(library.Id, copy.LibraryId);
    }

    [Fact]
    public async Task MoveLibraryBook_UnknownCopy_IsNotFound()
    {
        var library = SetupLibrary();

        var result = await _service.MoveLibraryBookAsync(library.Id, Guid.NewGuid(), _ownerId, new MoveLibraryBookRequest { TargetLibraryId = Guid.NewGuid() });

        Assert.Equal(LibraryService.LibraryBookNotFoundMessage, result.Error!.Message);
    }

    private Guid Viewer(string viewer) => viewer switch
    {
        "owner" => _ownerId,
        "friend" => _friendId,
        _ => _strangerId,
    };

    private Library SetupLibrary(bool isPublic = false, string name = "Living Room")
    {
        var library = TestData.Library(_ownerId, isPublic, name);
        _libraries.Setup(l => l.GetByIdAsync(library.Id, It.IsAny<CancellationToken>())).ReturnsAsync(library);
        return library;
    }

    private Book SetupBook()
    {
        var book = TestData.Book();
        _books.Setup(b => b.GetByIdAsync(book.Id, It.IsAny<CancellationToken>())).ReturnsAsync(book);
        return book;
    }

    private (Library Library, LibraryBook Copy) SetupCopy(bool isPublic = false, LibraryBookStatus status = LibraryBookStatus.Available)
    {
        var library = SetupLibrary(isPublic);
        var copy = TestData.Copy(library, TestData.Book(), status);
        _libraries.Setup(l => l.GetLibraryBookAsync(library.Id, copy.Id, It.IsAny<CancellationToken>())).ReturnsAsync(copy);
        return (library, copy);
    }
}
