using Alexandria.API.DTOs;

namespace Alexandria.API.Services;

public interface IBookService
{
    Task<IEnumerable<BookDto>> SearchBooksAsync(BookSearchRequest request, int page, int pageSize);
    Task<BookDto?> GetBookByIdAsync(int id);
    Task<BookDto> CreateBookAsync(CreateBookRequest request);
    Task<IEnumerable<BookDto>> SearchBooksByImageAsync(Stream imageStream, string fileName);
}
