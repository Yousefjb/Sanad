using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sanad.Api.Models;

namespace Sanad.Api.Services;

public interface INoteService
{
    Task<object> GetNotebooksAsync();
    Task<Notebook> CreateNotebookAsync(string name, int sortOrder = 0);
    Task<Notebook?> UpdateNotebookAsync(Guid id, string name, int sortOrder);
    Task<bool> DeleteNotebookAsync(Guid id);

    Task<object?> GetNotesByNotebookAsync(Guid notebookId);
    Task<Note?> GetNoteByIdAsync(Guid id);
    Task<List<Note>> GetRecentNotesAsync(int limit = 20);
    Task<Note?> CreateNoteAsync(Guid notebookId, string title, string content);
    Task<Note?> UpdateNoteAsync(Guid id, string title, string content, Guid? notebookId = null);
    Task<bool> DeleteNoteAsync(Guid id);
    Task<object?> GetLatestNoteAsync();
    Task<object> SearchNotesAsync(string? query);
    Task<List<Guid>> SyncNotesAsync(DateTime? since);
}
