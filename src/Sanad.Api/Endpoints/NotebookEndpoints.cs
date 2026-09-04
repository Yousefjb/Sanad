using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Sanad.Api.Data;
using Sanad.Api.Models;
using Sanad.Api.Services;

namespace Sanad.Api.Endpoints;

public static class NotebookEndpoints
{
    public static void MapNotebookEndpoints(this IEndpointRouteBuilder app)
    {
        // Notebooks CRUD
        app.MapGet("/api/notebooks", async (INoteService svc) => Results.Ok(await svc.GetNotebooksAsync()));
        app.MapPost("/api/notebooks", async (INoteService svc, Notebook input) =>
        {
            if (string.IsNullOrWhiteSpace(input.Name)) return Results.BadRequest("Name is required");
            var notebook = await svc.CreateNotebookAsync(input.Name, input.SortOrder);
            return Results.Created($"/api/notebooks/{notebook.Id}", new
            {
                notebook.Id,
                notebook.Name,
                notebook.SortOrder,
                notebook.CreatedAt,
                Notes = Array.Empty<object>()
            });
        });
        app.MapPut("/api/notebooks/{id}", async (INoteService svc, Guid id, Notebook updated) =>
        {
            if (string.IsNullOrWhiteSpace(updated.Name)) return Results.BadRequest("Name is required");
            var notebook = await svc.UpdateNotebookAsync(id, updated.Name, updated.SortOrder);
            if (notebook == null) return Results.NotFound();
            return Results.Ok(notebook);
        });
        app.MapDelete("/api/notebooks/{id}", async (INoteService svc, Guid id) =>
        {
            var success = await svc.DeleteNotebookAsync(id);
            if (!success) return Results.NotFound();
            return Results.NoContent();
        });

        // Notes CRUD
        app.MapGet("/api/notebooks/{notebookId}/notes", async (INoteService svc, Guid notebookId) =>
        {
            var notes = await svc.GetNotesByNotebookAsync(notebookId);
            if (notes == null) return Results.NotFound();
            return Results.Ok(notes);
        });
        app.MapPost("/api/notebooks/{notebookId}/notes", async (INoteService svc, Guid notebookId, Note input) =>
        {
            if (string.IsNullOrWhiteSpace(input.Title)) return Results.BadRequest("Title is required");
            var note = await svc.CreateNoteAsync(notebookId, input.Title, input.Content ?? string.Empty);
            if (note == null) return Results.NotFound();
            return Results.Created($"/api/notes/{note.Id}", note);
        });
        app.MapGet("/api/notes/{id}", async (INoteService svc, Guid id) =>
        {
            var note = await svc.GetNoteByIdAsync(id);
            if (note == null) return Results.NotFound();
            return Results.Ok(note);
        });
        app.MapPut("/api/notes/{id}", async (INoteService svc, Guid id, Note updated) =>
        {
            var note = await svc.UpdateNoteAsync(id, updated.Title, updated.Content ?? string.Empty, updated.NotebookId);
            if (note == null) return Results.NotFound();
            return Results.Ok(note);
        });
        app.MapDelete("/api/notes/{id}", async (INoteService svc, Guid id) =>
        {
            var success = await svc.DeleteNoteAsync(id);
            if (!success) return Results.NotFound();
            return Results.NoContent();
        });

        // Search, latest, sync
        app.MapGet("/api/notes/latest", async (INoteService svc) =>
        {
            var note = await svc.GetLatestNoteAsync();
            if (note == null) return Results.NoContent();
            return Results.Ok(note);
        });
        app.MapGet("/api/notes/search", async (INoteService svc, string? q) => Results.Ok(await svc.SearchNotesAsync(q)));
        app.MapGet("/api/notes/sync", async (INoteService svc, DateTime? since) => Results.Ok(await svc.SyncNotesAsync(since)));

        // Image upload
        app.MapPost("/api/notes/{id}/images", UploadNoteImage);
    }

    static async Task<IResult> UploadNoteImage(HttpRequest request, SanadDbContext db, Services.ITenantProvider tenantProvider, Services.DiskQuotaService quotaService, Guid id)
    {
        var note = await db.Notes.FindAsync(id);
        if (note == null) return Results.NotFound();

        var (errorResult, _, fileUrl) = await Utils.UploadHelper.HandleUploadAsync(request, tenantProvider, quotaService);
        if (errorResult != null) return errorResult;

        return Results.Ok(new { url = fileUrl });
    }
}
