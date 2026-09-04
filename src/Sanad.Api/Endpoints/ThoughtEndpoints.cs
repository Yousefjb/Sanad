using Microsoft.EntityFrameworkCore;
using Sanad.Api.Data;
using Sanad.Api.Models;
using Sanad.Api.Services;

namespace Sanad.Api.Endpoints;

public static class ThoughtEndpoints
{
    public static void MapThoughtEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/thoughts", (IThoughtService svc, Thought input) => CreateThought(svc, input));
        app.MapGet("/api/thoughts", (IThoughtService svc, int? page, int? pageSize, string? search) => GetThoughts(svc, page, pageSize, search));
        app.MapPut("/api/thoughts/{id}", (IThoughtService svc, string id, Thought updated) => UpdateThought(svc, id, updated));
        app.MapDelete("/api/thoughts/{id}", (IThoughtService svc, string id) => DeleteThought(svc, id));
    }

    public static async Task<IResult> CreateThought(IThoughtService svc, Thought input)
    {
        var thought = await svc.CreateThoughtAsync(input.Content ?? string.Empty);
        return Results.Ok(thought);
    }

    public static Task<IResult> CreateThought(SanadDbContext db, Thought input) =>
        CreateThought(new ThoughtService(db), input);

    public static async Task<IResult> GetThoughts(IThoughtService svc, int? page, int? pageSize, string? search)
    {
        var thoughts = await svc.GetThoughtsAsync(page ?? 1, pageSize ?? 20, search);
        return Results.Ok(thoughts);
    }

    public static Task<IResult> GetThoughts(SanadDbContext db, int? page, int? pageSize, string? search) =>
        GetThoughts(new ThoughtService(db), page, pageSize, search);

    public static async Task<IResult> UpdateThought(IThoughtService svc, string id, Thought updated)
    {
        var thought = await svc.UpdateThoughtAsync(id, updated.Content ?? string.Empty);
        if (thought == null) return Results.NotFound();
        return Results.Ok(thought);
    }

    public static Task<IResult> UpdateThought(SanadDbContext db, string id, Thought updated) =>
        UpdateThought(new ThoughtService(db), id, updated);

    public static async Task<IResult> DeleteThought(IThoughtService svc, string id)
    {
        var success = await svc.DeleteThoughtAsync(id);
        if (!success) return Results.NotFound();
        return Results.NoContent();
    }

    public static Task<IResult> DeleteThought(SanadDbContext db, string id) =>
        DeleteThought(new ThoughtService(db), id);
}
