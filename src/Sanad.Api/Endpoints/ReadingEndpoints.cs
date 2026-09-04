using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Sanad.Api.Data;
using Sanad.Api.Models;
using Sanad.Api.Services;

namespace Sanad.Api.Endpoints;

public static class ReadingEndpoints
{
    public static void MapReadingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reading");

        group.MapGet("/periods", async (IReadingService svc) =>
        {
            var periods = await svc.GetReadingPeriodsAsync();
            return Results.Ok(periods);
        });

        group.MapPost("/periods", async (IReadingService svc, StartPeriodDto dto) =>
        {
            var period = await svc.StartReadingPeriodAsync(dto.BookId, dto.Plans);
            return Results.Created($"/api/reading/periods/{period.Id}", period);
        });

        group.MapPost("/logs", async (IReadingService svc, LogDto dto) =>
        {
            var log = await svc.LogReadingAsync(dto.ReadingPeriodId, dto.StartPage, dto.EndPage);
            if (log == null) return Results.NotFound();
            return Results.Ok(log);
        });

        group.MapPut("/periods/{id}/plans", async (int id, IReadingService svc, List<Sanad.Api.Services.PlanDto> plans) =>
        {
            var updatedPlans = await svc.UpdatePlansAsync(id, plans);
            if (updatedPlans == null) return Results.NotFound();
            return Results.Ok(updatedPlans);
        });

        group.MapPut("/periods/{id}/status", async (int id, IReadingService svc, StatusDto dto) =>
        {
            var period = await svc.UpdateStatusAsync(id, dto.Status);
            if (period == null) return Results.NotFound();
            return Results.Ok(period);
        });

        group.MapGet("/current", async (IReadingService svc) =>
        {
            var progress = await svc.GetCurrentReadingAsync();
            if (progress == null) return Results.NotFound();
            return Results.Ok(progress);
        });

        group.MapDelete("/periods/{id}", async (int id, IReadingService svc) =>
        {
            var success = await svc.DeleteReadingPeriodAsync(id);
            if (!success) return Results.NotFound();
            return Results.NoContent();
        });
    }

    public record StartPeriodDto(int BookId, List<Sanad.Api.Services.PlanDto> Plans);
    public record PlanDto(string Title, int StartPage, int EndPage) : Sanad.Api.Services.PlanDto(Title, StartPage, EndPage);
    public record LogDto(int ReadingPeriodId, int StartPage, int EndPage);
    public record StatusDto(string Status);
}
