using System.Collections.Generic;
using System.Threading.Tasks;
using Sanad.Api.Models;

namespace Sanad.Api.Services;

public interface IBookService
{
    Task<List<Book>> GetBooksAsync();
    Task<Book> CreateBookAsync(Book book);
    Task<Book> CreateBookAsync(string title, string author, string coverUrl, int totalPages);
    Task<Book?> UpdateBookAsync(int id, Book updatedBook);
    Task<Book?> UpdateBookAsync(int id, string title, string author, string coverUrl, int totalPages);
    Task<bool> DeleteBookAsync(int id);
}
