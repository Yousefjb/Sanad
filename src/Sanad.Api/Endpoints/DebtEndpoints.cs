using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Sanad.Api.Data;
using Sanad.Api.Models;
using Sanad.Api.Services;

namespace Sanad.Api.Endpoints;

public static class DebtEndpoints
{
    public static void MapDebtEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/finances/debts", (IDebtService svc) => GetDebts(svc));
        app.MapPost("/api/finances/debts", (IDebtService svc, Debt debt) => CreateDebt(svc, debt));
        app.MapPut("/api/finances/debts/{id}", (IDebtService svc, Guid id, Debt updated) => UpdateDebt(svc, id, updated));
        app.MapDelete("/api/finances/debts/{id}", (IDebtService svc, Guid id) => DeleteDebt(svc, id));
        app.MapPut("/api/finances/debts/reorder", (IDebtService svc, List<Guid> orderedIds) => ReorderDebts(svc, orderedIds));
        app.MapGet("/api/finances/debts/history", (IDebtService svc) => GetDebtsHistory(svc));
    }

    public static async Task<IResult> GetDebts(IDebtService svc) =>
        Results.Ok(await svc.GetDebtsAsync());

    public static Task<IResult> GetDebts(SanadDbContext db) =>
        GetDebts(new DebtService(db));

    public static async Task<IResult> CreateDebt(IDebtService svc, Debt debt)
    {
        var created = await svc.CreateDebtAsync(debt);
        return Results.Created($"/api/finances/debts/{created.Id}", created);
    }

    public static Task<IResult> CreateDebt(SanadDbContext db, Debt debt) =>
        CreateDebt(new DebtService(db), debt);

    public static async Task<IResult> UpdateDebt(IDebtService svc, Guid id, Debt updated)
    {
        var debt = await svc.UpdateDebtAsync(id, updated);
        if (debt is null) return Results.NotFound();
        return Results.Ok(debt);
    }

    public static Task<IResult> UpdateDebt(SanadDbContext db, Guid id, Debt updated) =>
        UpdateDebt(new DebtService(db), id, updated);

    public static async Task<IResult> DeleteDebt(IDebtService svc, Guid id)
    {
        var success = await svc.DeleteDebtAsync(id);
        if (!success) return Results.NotFound();
        return Results.NoContent();
    }

    public static Task<IResult> DeleteDebt(SanadDbContext db, Guid id) =>
        DeleteDebt(new DebtService(db), id);

    public static async Task<IResult> ReorderDebts(IDebtService svc, List<Guid> orderedIds)
    {
        await svc.ReorderDebtsAsync(orderedIds);
        return Results.Ok();
    }

    public static Task<IResult> ReorderDebts(SanadDbContext db, List<Guid> orderedIds) =>
        ReorderDebts(new DebtService(db), orderedIds);

    public static async Task<IResult> GetDebtsHistory(IDebtService svc) =>
        Results.Ok(await svc.GetDebtsHistoryAsync());

    public static Task<IResult> GetDebtsHistory(SanadDbContext db) =>
        GetDebtsHistory(new DebtService(db));
}
