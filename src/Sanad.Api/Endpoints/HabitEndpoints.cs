using Microsoft.EntityFrameworkCore;
using Sanad.Api.Data;
using Sanad.Api.Models;
using Sanad.Api.Services;

namespace Sanad.Api.Endpoints;

public static class HabitEndpoints
{
    public static void MapHabitEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/habits");

        group.MapGet("/", async (IHabitService svc) =>
        {
            var habits = await svc.GetHabitsAsync();
            return Results.Ok(habits);
        });

        group.MapPost("/", async (IHabitService svc, Habit habit) =>
        {
            var created = await svc.CreateHabitAsync(habit);
            return Results.Created($"/api/habits/{created.Id}", created);
        });

        group.MapPut("/{id}", async (IHabitService svc, string id, Habit inputHabit) =>
        {
            var updated = await svc.UpdateHabitAsync(id, inputHabit.Name, inputHabit.Icon, inputHabit.Frequency);
            if (updated is null) return Results.NotFound();
            return Results.NoContent();
        });

        group.MapDelete("/{id}", async (IHabitService svc, string id) =>
        {
            var success = await svc.DeleteHabitAsync(id);
            if (!success) return Results.NotFound();
            return Results.NoContent();
        });

        group.MapPost("/{id}/toggle", async (IHabitService svc, string id, ToggleHabitLogRequest req) =>
        {
            var log = await svc.ToggleHabitLogAsync(id, req.Date);
            if (log is null) return Results.NotFound();
            return Results.Ok(log);
        });

        group.MapPut("/reorder", async (IHabitService svc, ReorderHabitsRequest req) =>
        {
            await svc.ReorderHabitsAsync(req.HabitIds);
            return Results.NoContent();
        });
    }
}

public class ReorderHabitsRequest
{
    public List<string> HabitIds { get; set; } = new();
}

public class ToggleHabitLogRequest
{
    public DateTime Date { get; set; }
}
