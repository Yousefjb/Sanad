using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Sanad.Api.Data;
using Sanad.Api.Models;
using Sanad.Api.Services;

namespace Sanad.Api.Endpoints;

public static class FinanceEndpoints
{
    public static void MapFinanceEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/finances/categories", (IFinanceService svc) => GetCategories(svc));
        app.MapPost("/api/finances/categories", (IFinanceService svc, TransactionCategory category) => CreateCategory(svc, category));
        app.MapPut("/api/finances/categories/{id}", (IFinanceService svc, Guid id, TransactionCategory updated) => UpdateCategory(svc, id, updated));
        app.MapGet("/api/finances/transactions", (IFinanceService svc, int? month, int? year, int page, int pageSize, string? search, Guid? categoryId) =>
            GetTransactions(svc, month, year, page, pageSize, search, categoryId));
        app.MapPost("/api/finances/transactions", (IFinanceService svc, Transaction transaction) => CreateTransaction(svc, transaction));
        app.MapPut("/api/finances/transactions/{id}", (IFinanceService svc, Guid id, UpdateTransactionRequest updated) => UpdateTransaction(svc, id, updated));
        app.MapDelete("/api/finances/transactions/{id}", (IFinanceService svc, Guid id) => DeleteTransaction(svc, id));
        app.MapGet("/api/finances/summary", (IFinanceService svc, int? month, int? year) => GetSummary(svc, month, year));
        app.MapGet("/api/finances/budget", (IFinanceService svc, int? month, int? year) => GetMonthlyBudget(svc, month, year));
        app.MapPut("/api/finances/budget", (IFinanceService svc, MonthlyBudgetRequest request) => SetMonthlyBudget(svc, request));

        app.MapGet("/api/finances/currencies", (IFinanceService svc) => GetCurrencies(svc));
        app.MapPost("/api/finances/currencies", (IFinanceService svc, Currency currency) => CreateCurrency(svc, currency));
        app.MapPut("/api/finances/currencies/{id}", (IFinanceService svc, Guid id, Currency updated) => UpdateCurrency(svc, id, updated));
        app.MapDelete("/api/finances/currencies/{id}", (IFinanceService svc, Guid id) => DeleteCurrency(svc, id));
        app.MapPut("/api/finances/currencies/{id}/set-default", (IFinanceService svc, Guid id) => SetDefaultCurrency(svc, id));
    }

    public static async Task<IResult> GetCategories(IFinanceService svc) =>
        Results.Ok(await svc.GetCategoriesAsync());

    public static Task<IResult> GetCategories(SanadDbContext db) =>
        GetCategories(new FinanceService(db));

    public static async Task<IResult> CreateCategory(IFinanceService svc, TransactionCategory category)
    {
        var created = await svc.CreateCategoryAsync(category);
        return Results.Created($"/api/finances/categories/{created.Id}", created);
    }

    public static Task<IResult> CreateCategory(SanadDbContext db, TransactionCategory category) =>
        CreateCategory(new FinanceService(db), category);

    public static async Task<IResult> UpdateCategory(IFinanceService svc, Guid id, TransactionCategory updated)
    {
        var category = await svc.UpdateCategoryAsync(id, updated);
        if (category is null) return Results.NotFound();
        return Results.Ok(category);
    }

    public static Task<IResult> UpdateCategory(SanadDbContext db, Guid id, TransactionCategory updated) =>
        UpdateCategory(new FinanceService(db), id, updated);

    public static async Task<IResult> GetTransactions(
        IFinanceService svc,
        int? month,
        int? year,
        int page = 1,
        int pageSize = 15,
        string? search = null,
        Guid? categoryId = null)
    {
        var (items, totalCount, hasMore) = await svc.GetTransactionsPaginatedAsync(month, year, page, pageSize, search, categoryId);
        return Results.Ok(new
        {
            Items = items,
            TotalCount = totalCount,
            HasMore = hasMore
        });
    }

    public static Task<IResult> GetTransactions(
        SanadDbContext db,
        int? month,
        int? year,
        int page = 1,
        int pageSize = 15,
        string? search = null,
        Guid? categoryId = null) =>
        GetTransactions(new FinanceService(db), month, year, page, pageSize, search, categoryId);

    public static async Task<IResult> CreateTransaction(IFinanceService svc, Transaction transaction)
    {
        var created = await svc.CreateTransactionAsync(transaction);
        if (created == null) return Results.BadRequest("Category not found");
        return Results.Created($"/api/finances/transactions/{created.Id}", created);
    }

    public static Task<IResult> CreateTransaction(SanadDbContext db, Transaction transaction) =>
        CreateTransaction(new FinanceService(db), transaction);

    public static async Task<IResult> UpdateTransaction(IFinanceService svc, Guid id, UpdateTransactionRequest updated)
    {
        var transaction = await svc.UpdateTransactionAsync(id, updated.Amount, updated.Description, updated.CategoryId, updated.Date, updated.Type);
        if (transaction is null) return Results.NotFound();
        return Results.Ok(transaction);
    }

    public static Task<IResult> UpdateTransaction(SanadDbContext db, Guid id, UpdateTransactionRequest updated) =>
        UpdateTransaction(new FinanceService(db), id, updated);

    public static async Task<IResult> DeleteTransaction(IFinanceService svc, Guid id)
    {
        var success = await svc.DeleteTransactionAsync(id);
        if (!success) return Results.NotFound();
        return Results.NoContent();
    }

    public static Task<IResult> DeleteTransaction(SanadDbContext db, Guid id) =>
        DeleteTransaction(new FinanceService(db), id);

    public static async Task<IResult> GetSummary(IFinanceService svc, int? month, int? year) =>
        Results.Ok(await svc.GetSummaryAsync(month, year));

    public static Task<IResult> GetSummary(SanadDbContext db, int? month, int? year) =>
        GetSummary(new FinanceService(db), month, year);

    public static async Task<IResult> GetMonthlyBudget(IFinanceService svc, int? month, int? year)
    {
        var targetMonth = month ?? DateTime.UtcNow.Month;
        var targetYear = year ?? DateTime.UtcNow.Year;
        var amount = await svc.GetMonthlyBudgetAsync(targetMonth, targetYear);
        return Results.Ok(new { Amount = amount, Year = targetYear, Month = targetMonth });
    }

    public static Task<IResult> GetMonthlyBudget(SanadDbContext db, int? month, int? year) =>
        GetMonthlyBudget(new FinanceService(db), month, year);

    public static async Task<IResult> SetMonthlyBudget(IFinanceService svc, MonthlyBudgetRequest request)
    {
        var budget = await svc.SetMonthlyBudgetAsync(request.Month, request.Year, request.Amount);
        return Results.Ok(budget);
    }

    public static Task<IResult> SetMonthlyBudget(SanadDbContext db, MonthlyBudgetRequest request) =>
        SetMonthlyBudget(new FinanceService(db), request);

    public static async Task<IResult> GetCurrencies(IFinanceService svc) =>
        Results.Ok(await svc.GetCurrenciesAsync());

    public static Task<IResult> GetCurrencies(SanadDbContext db) =>
        GetCurrencies(new FinanceService(db));

    public static async Task<IResult> CreateCurrency(IFinanceService svc, Currency currency)
    {
        var created = await svc.CreateCurrencyAsync(currency);
        return Results.Created($"/api/finances/currencies/{created.Id}", created);
    }

    public static Task<IResult> CreateCurrency(SanadDbContext db, Currency currency) =>
        CreateCurrency(new FinanceService(db), currency);

    public static async Task<IResult> UpdateCurrency(IFinanceService svc, Guid id, Currency updated)
    {
        var currency = await svc.UpdateCurrencyAsync(id, updated);
        if (currency is null) return Results.NotFound();
        return Results.Ok(currency);
    }

    public static Task<IResult> UpdateCurrency(SanadDbContext db, Guid id, Currency updated) =>
        UpdateCurrency(new FinanceService(db), id, updated);

    public static async Task<IResult> DeleteCurrency(IFinanceService svc, Guid id)
    {
        var success = await svc.DeleteCurrencyAsync(id);
        if (!success) return Results.BadRequest("Cannot delete currency.");
        return Results.NoContent();
    }

    public static Task<IResult> DeleteCurrency(SanadDbContext db, Guid id) =>
        DeleteCurrency(new FinanceService(db), id);

    public static async Task<IResult> SetDefaultCurrency(IFinanceService svc, Guid id)
    {
        var success = await svc.SetDefaultCurrencyAsync(id);
        if (!success) return Results.BadRequest("Invalid exchange rate or currency not found.");
        return Results.Ok();
    }

    public static Task<IResult> SetDefaultCurrency(SanadDbContext db, Guid id) =>
        SetDefaultCurrency(new FinanceService(db), id);
}

public record MonthlyBudgetRequest(decimal Amount, int? Month, int? Year);
public record UpdateTransactionRequest(decimal? Amount, string? Description, Guid? CategoryId, DateTime? Date, string? Type);
