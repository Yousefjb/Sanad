using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Sanad.Api.Models;
using Sanad.Api.Services;

namespace Sanad.Api.Endpoints;

public static class CalendarEndpoints
{
    public static void MapCalendarEndpoints(this RouteGroupBuilder group)
    {
        var calendar = group.MapGroup("/api/calendar");

        // --- Categories ---
        calendar.MapGet("/categories", async (ICalendarService svc) =>
        {
            var categories = await svc.GetCategoriesAsync();
            return Results.Ok(categories);
        });

        calendar.MapPost("/categories", async (EventCategory category, ICalendarService svc) =>
        {
            var created = await svc.CreateCategoryAsync(category);
            return Results.Created($"/api/calendar/categories/{created.Id}", created);
        });

        calendar.MapPut("/categories/{id:guid}", async (Guid id, EventCategory updated, ICalendarService svc) =>
        {
            var category = await svc.UpdateCategoryAsync(id, updated.Name, updated.ColorCode);
            if (category == null) return Results.NotFound();
            return Results.Ok(category);
        });

        calendar.MapDelete("/categories/{id:guid}", async (Guid id, ICalendarService svc) =>
        {
            var success = await svc.DeleteCategoryAsync(id);
            if (!success) return Results.NotFound();
            return Results.NoContent();
        });

        // --- Events ---
        calendar.MapGet("/events", async (DateTime? start, DateTime? end, ICalendarService svc) =>
        {
            var events = await svc.GetEventsAsync(start, end);
            return Results.Ok(events);
        });

        calendar.MapPost("/events", async (CalendarEvent evt, ICalendarService svc) =>
        {
            var created = await svc.CreateEventAsync(evt);
            return Results.Created($"/api/calendar/events/{created.Id}", created);
        });

        calendar.MapPut("/events/{id:guid}", async (Guid id, CalendarEvent updated, ICalendarService svc) =>
        {
            var saved = await svc.UpdateEventAsync(id, updated);
            if (saved == null) return Results.NotFound();
            return Results.Ok(saved);
        });

        calendar.MapDelete("/events/{id:guid}", async (Guid id, ICalendarService svc) =>
        {
            var success = await svc.DeleteEventAsync(id);
            if (!success) return Results.NotFound();
            return Results.NoContent();
        });
    }
}
