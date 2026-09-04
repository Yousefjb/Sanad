using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sanad.Api.Data;
using Sanad.Api.Models;

namespace Sanad.Api.Services;

public class NoteService : INoteService
{
    private readonly SanadDbContext _db;
    private readonly ITenantProvider _tenantProvider;

    public NoteService(SanadDbContext db, ITenantProvider tenantProvider)
    {
        _db = db;
        _tenantProvider = tenantProvider;
    }

    public async Task<object> GetNotebooksAsync()
    {
        return await _db.Notebooks
            .Include(n => n.Notes)
            .OrderBy(n => n.SortOrder).ThenBy(n => n.Name)
            .Select(n => new
            {
                n.Id,
                n.Name,
                n.SortOrder,
                n.CreatedAt,
                Notes = n.Notes.OrderByDescending(note => note.UpdatedAt).Select(note => new
                {
                    note.Id,
                    note.Title,
                    note.NotebookId,
                    note.CreatedAt,
                    note.UpdatedAt
                })
            })
            .ToListAsync();
    }

    public async Task<Notebook> CreateNotebookAsync(string name, int sortOrder = 0)
    {
        var notebook = new Notebook
        {
            Id = Guid.NewGuid(),
            Name = name,
            SortOrder = sortOrder,
            CreatedAt = DateTime.UtcNow
        };
        _db.Notebooks.Add(notebook);
        await _db.SaveChangesAsync();
        return notebook;
    }

    public async Task<Notebook?> UpdateNotebookAsync(Guid id, string name, int sortOrder)
    {
        var notebook = await _db.Notebooks.FindAsync(id);
        if (notebook == null) return null;

        notebook.Name = name;
        notebook.SortOrder = sortOrder;
        await _db.SaveChangesAsync();
        return notebook;
    }

    public async Task<bool> DeleteNotebookAsync(Guid id)
    {
        var notebook = await _db.Notebooks.Include(n => n.Notes).FirstOrDefaultAsync(n => n.Id == id);
        if (notebook == null) return false;

        var filesToDelete = new List<string>();
        try
        {
            var username = _tenantProvider.GetUsername();
            foreach (var note in notebook.Notes)
            {
                filesToDelete.AddRange(Utils.UploadHelper.GetAttachmentPathsFromHtml(note.Content, username));
            }
        }
        catch
        {
            // Ignore in test contexts without HTTP user
        }

        _db.Notes.RemoveRange(notebook.Notes);
        _db.Notebooks.Remove(notebook);
        await _db.SaveChangesAsync();

        if (filesToDelete.Count > 0)
        {
            Utils.UploadHelper.DeleteFiles(filesToDelete);
        }

        return true;
    }

    public async Task<object?> GetNotesByNotebookAsync(Guid notebookId)
    {
        var exists = await _db.Notebooks.AnyAsync(n => n.Id == notebookId);
        if (!exists) return null;

        return await _db.Notes
            .Where(n => n.NotebookId == notebookId)
            .OrderByDescending(n => n.UpdatedAt)
            .Select(n => new { n.Id, n.Title, n.NotebookId, n.CreatedAt, n.UpdatedAt })
            .ToListAsync();
    }

    public async Task<Note?> GetNoteByIdAsync(Guid id)
    {
        return await _db.Notes.FindAsync(id);
    }

    public async Task<List<Note>> GetRecentNotesAsync(int limit = 20)
    {
        return await _db.Notes
            .OrderByDescending(n => n.UpdatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<Note?> CreateNoteAsync(Guid notebookId, string title, string content)
    {
        var exists = await _db.Notebooks.AnyAsync(n => n.Id == notebookId);
        if (!exists) return null;

        var note = new Note
        {
            Id = Guid.NewGuid(),
            NotebookId = notebookId,
            Title = title,
            Content = content,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Notes.Add(note);
        await _db.SaveChangesAsync();
        return note;
    }

    public async Task<Note?> UpdateNoteAsync(Guid id, string title, string content, Guid? notebookId = null)
    {
        var note = await _db.Notes.FindAsync(id);
        if (note == null) return null;

        note.Title = title;
        note.Content = content;

        if (notebookId.HasValue && notebookId.Value != Guid.Empty && notebookId.Value != note.NotebookId)
        {
            var newNotebookExists = await _db.Notebooks.AnyAsync(n => n.Id == notebookId.Value);
            if (newNotebookExists)
            {
                note.NotebookId = notebookId.Value;
            }
        }

        note.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return note;
    }

    public async Task<bool> DeleteNoteAsync(Guid id)
    {
        var note = await _db.Notes.FindAsync(id);
        if (note == null) return false;

        var filesToDelete = new List<string>();
        try
        {
            var username = _tenantProvider.GetUsername();
            filesToDelete.AddRange(Utils.UploadHelper.GetAttachmentPathsFromHtml(note.Content, username));
        }
        catch
        {
            // Ignore in test contexts without HTTP user
        }

        _db.Notes.Remove(note);
        await _db.SaveChangesAsync();

        if (filesToDelete.Count > 0)
        {
            Utils.UploadHelper.DeleteFiles(filesToDelete);
        }

        return true;
    }

    public async Task<object?> GetLatestNoteAsync()
    {
        return await _db.Notes
            .OrderByDescending(n => n.UpdatedAt)
            .Select(n => new { n.Id, n.Title, n.NotebookId, n.CreatedAt, n.UpdatedAt })
            .FirstOrDefaultAsync();
    }

    public async Task<object> SearchNotesAsync(string? query)
    {
        if (string.IsNullOrWhiteSpace(query)) return Array.Empty<object>();
        var lower = query.ToLower();
        return await _db.Notes
            .Where(n => n.Title.ToLower().Contains(lower) || (n.Content != null && n.Content.ToLower().Contains(lower)))
            .OrderByDescending(n => n.UpdatedAt)
            .Select(n => new { n.Id, n.Title, n.NotebookId, n.CreatedAt, n.UpdatedAt })
            .Take(20)
            .ToListAsync();
    }

    public async Task<List<Guid>> SyncNotesAsync(DateTime? since)
    {
        var query = _db.Notes.AsQueryable();
        if (since.HasValue)
        {
            query = query.Where(n => n.UpdatedAt >= since.Value);
        }
        return await query.Select(n => n.Id).ToListAsync();
    }
}
