using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sanad.Api.Data;
using Sanad.Api.Models;

namespace Sanad.Api.Services;

public class BookService : IBookService
{
    private readonly SanadDbContext _db;

    public BookService(SanadDbContext db)
    {
        _db = db;
    }

    public async Task<List<Book>> GetBooksAsync()
    {
        return await _db.Books.OrderByDescending(b => b.CreatedAt).ToListAsync();
    }

    public async Task<Book> CreateBookAsync(Book book)
    {
        if (book.CreatedAt == default)
            book.CreatedAt = DateTime.UtcNow;

        _db.Books.Add(book);
        await _db.SaveChangesAsync();
        return book;
    }

    public async Task<Book> CreateBookAsync(string title, string author, string coverUrl, int totalPages)
    {
        var book = new Book
        {
            Title = title,
            Author = author,
            CoverUrl = coverUrl,
            TotalPages = totalPages,
            CreatedAt = DateTime.UtcNow
        };
        _db.Books.Add(book);
        await _db.SaveChangesAsync();
        return book;
    }

    public async Task<Book?> UpdateBookAsync(int id, Book updatedBook)
    {
        return await UpdateBookAsync(id, updatedBook.Title, updatedBook.Author, updatedBook.CoverUrl, updatedBook.TotalPages);
    }

    public async Task<Book?> UpdateBookAsync(int id, string title, string author, string coverUrl, int totalPages)
    {
        var book = await _db.Books.FindAsync(id);
        if (book == null) return null;

        book.Title = title;
        book.Author = author;
        book.CoverUrl = coverUrl;
        book.TotalPages = totalPages;

        await _db.SaveChangesAsync();
        return book;
    }

    public async Task<bool> DeleteBookAsync(int id)
    {
        var book = await _db.Books.FindAsync(id);
        if (book == null) return false;

        _db.Books.Remove(book);
        await _db.SaveChangesAsync();
        return true;
    }
}
