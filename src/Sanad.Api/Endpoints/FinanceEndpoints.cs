using Microsoft.EntityFrameworkCore;
using Sanad.Api.Data;
using Sanad.Api.Models;

namespace Sanad.Api.Endpoints;

public static class FinanceEndpoints
{
    public static void MapFinanceEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/finances/categories", GetCategories);
        app.MapPost("/api/finances/categories", CreateCategory);
        app.MapPut("/api/finances/categories/{id}", UpdateCategory);
        app.MapGet("/api/finances/transactions", GetTransactions);
        app.MapPost("/api/finances/transactions", CreateTransaction);
        app.MapPut("/api/finances/transactions/{id}", UpdateTransaction);
        app.MapDelete("/api/finances/transactions/{id}", DeleteTransaction);
        app.MapGet("/api/finances/summary", GetSummary);
        app.MapGet("/api/finances/budget", GetMonthlyBudget);
        app.MapPut("/api/finances/budget", SetMonthlyBudget);

        app.MapGet("/api/finances/currencies", GetCurrencies);
        app.MapPost("/api/finances/currencies", CreateCurrency);
        app.MapPut("/api/finances/currencies/{id}", UpdateCurrency);
        app.MapDelete("/api/finances/currencies/{id}", DeleteCurrency);
        app.MapPut("/api/finances/currencies/{id}/set-default", SetDefaultCurrency);
    }

    public static async Task<IResult> GetCategories(SanadDbContext db) => 
        Results.Ok(await db.TransactionCategories.ToListAsync());

    public static async Task<IResult> CreateCategory(SanadDbContext db, TransactionCategory category)
    {
        db.TransactionCategories.Add(category);
        await db.SaveChangesAsync();
        return Results.Created($"/api/finances/categories/{category.Id}", category);
    }

    public static async Task<IResult> UpdateCategory(SanadDbContext db, Guid id, TransactionCategory updated)
    {
        var category = await db.TransactionCategories.FindAsync(id);
        if (category is null) return Results.NotFound();

        category.Name = updated.Name;
        category.ColorHex = updated.ColorHex;
        category.MonthlyBudget = updated.MonthlyBudget;

        await db.SaveChangesAsync();
        return Results.Ok(category);
    }

    public static async Task<IResult> GetTransactions(
        SanadDbContext db, 
        int? month, 
        int? year, 
        int page = 1, 
        int pageSize = 15,
        string? search = null,
        Guid? categoryId = null)
    {
        var targetMonth = month ?? DateTime.UtcNow.Month;
        var targetYear = year ?? DateTime.UtcNow.Year;
        var startDate = new DateTime(targetYear, targetMonth, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = startDate.AddMonths(1);

        var query = db.Transactions
            .Include(t => t.Category)
            .Where(t => t.Date >= startDate && t.Date < endDate);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(t => t.Description != null && t.Description.ToLower().Contains(searchLower));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(t => t.CategoryId == categoryId.Value);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(t => t.Date)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
            
        return Results.Ok(new 
        {
            Items = items,
            TotalCount = totalCount,
            HasMore = totalCount > page * pageSize
        });
    }

    public static async Task<IResult> CreateTransaction(SanadDbContext db, Transaction transaction)
    {
        var categoryExists = await db.TransactionCategories.AnyAsync(c => c.Id == transaction.CategoryId);
        if (!categoryExists)
        {
            return Results.BadRequest("Category not found");
        }

        db.Transactions.Add(transaction);
        
        await db.SaveChangesAsync();
        return Results.Created($"/api/finances/transactions/{transaction.Id}", transaction);
    }

    public static async Task<IResult> UpdateTransaction(SanadDbContext db, Guid id, UpdateTransactionRequest updated)
    {
        var transaction = await db.Transactions.Include(t => t.Category).FirstOrDefaultAsync(t => t.Id == id);
        if (transaction is null) return Results.NotFound();

        if (updated.Amount.HasValue)
        {
            transaction.Amount = updated.Amount.Value;
        }

        if (updated.Description != null)
        {
            transaction.Description = updated.Description;
        }

        if (updated.CategoryId.HasValue && updated.CategoryId.Value != Guid.Empty)
        {
            var category = await db.TransactionCategories.FindAsync(updated.CategoryId.Value);
            if (category == null)
            {
                return Results.BadRequest("Category not found");
            }
            transaction.CategoryId = updated.CategoryId.Value;
            transaction.Category = category;
        }

        if (updated.Date.HasValue)
        {
            transaction.Date = updated.Date.Value;
        }

        if (!string.IsNullOrEmpty(updated.Type))
        {
            transaction.Type = updated.Type;
        }

        await db.SaveChangesAsync();
        return Results.Ok(transaction);
    }

    public static async Task<IResult> DeleteTransaction(SanadDbContext db, Guid id)
    {
        var transaction = await db.Transactions.FindAsync(id);
        if (transaction is null) return Results.NotFound();

        db.Transactions.Remove(transaction);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    public static async Task<IResult> GetSummary(SanadDbContext db, int? month, int? year)
    {
        var targetMonth = month ?? DateTime.UtcNow.Month;
        var targetYear = year ?? DateTime.UtcNow.Year;
        var startDate = new DateTime(targetYear, targetMonth, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = startDate.AddMonths(1);
        
        var categories = await db.TransactionCategories.ToListAsync();
        var categorySpends = await db.Transactions
            .Where(t => t.Date >= startDate && t.Date < endDate && t.Type == "Expense")
            .GroupBy(t => t.CategoryId)
            .Select(g => new { CategoryId = g.Key, TotalAmount = g.Sum(t => t.Amount) })
            .ToDictionaryAsync(g => g.CategoryId, g => g.TotalAmount);

        var monthlyBudget = await db.MonthlyBudgets
            .FirstOrDefaultAsync(b => b.Year == targetYear && b.Month == targetMonth);

        var categorySummary = categories.Select(c => 
        {
            var spent = categorySpends.GetValueOrDefault(c.Id, 0);
            return new
            {
                Category = c,
                Spent = spent,
                Remaining = c.MonthlyBudget - spent
            };
        });

        return Results.Ok(new
        {
            Categories = categorySummary,
            MonthlyBudget = monthlyBudget?.Amount ?? 0m,
            TotalSpent = categorySpends.Values.Sum()
        });
    }

    public static async Task<IResult> GetMonthlyBudget(SanadDbContext db, int? month, int? year)
    {
        var targetMonth = month ?? DateTime.UtcNow.Month;
        var targetYear = year ?? DateTime.UtcNow.Year;

        var budget = await db.MonthlyBudgets
            .FirstOrDefaultAsync(b => b.Year == targetYear && b.Month == targetMonth);

        return Results.Ok(new { Amount = budget?.Amount ?? 0m, Year = targetYear, Month = targetMonth });
    }

    public static async Task<IResult> SetMonthlyBudget(SanadDbContext db, MonthlyBudgetRequest request)
    {
        var targetMonth = request.Month ?? DateTime.UtcNow.Month;
        var targetYear = request.Year ?? DateTime.UtcNow.Year;

        var budget = await db.MonthlyBudgets
            .FirstOrDefaultAsync(b => b.Year == targetYear && b.Month == targetMonth);

        if (budget is null)
        {
            budget = new MonthlyBudget
            {
                Year = targetYear,
                Month = targetMonth,
                Amount = request.Amount
            };
            db.MonthlyBudgets.Add(budget);
        }
        else
        {
            budget.Amount = request.Amount;
        }

        await db.SaveChangesAsync();
        return Results.Ok(budget);
    }

    public static async Task<IResult> GetCurrencies(SanadDbContext db) =>
        Results.Ok(await db.Currencies.ToListAsync());

    public static async Task<IResult> CreateCurrency(SanadDbContext db, Currency currency)
    {
        // If it's the first currency, make it default regardless of input.
        // If it's set to default, we must unset other defaults and flip exchange rates.
        // To simplify, CreateCurrency will NOT flip exchange rates. If they want to set it as default, they should use SetDefaultCurrency.
        var hasCurrencies = await db.Currencies.AnyAsync();
        currency.IsDefault = !hasCurrencies;
        
        currency.CreatedAt = DateTime.UtcNow;
        currency.UpdatedAt = DateTime.UtcNow;
        
        db.Currencies.Add(currency);
        await db.SaveChangesAsync();
        return Results.Created($"/api/finances/currencies/{currency.Id}", currency);
    }

    public static async Task<IResult> UpdateCurrency(SanadDbContext db, Guid id, Currency updated)
    {
        var currency = await db.Currencies.FindAsync(id);
        if (currency is null) return Results.NotFound();

        currency.Name = updated.Name;
        currency.Code = updated.Code;
        currency.Symbol = updated.Symbol;
        
        // We only allow updating ExchangeRate if it's not the default currency
        if (!currency.IsDefault)
        {
            currency.ExchangeRateToDefault = updated.ExchangeRateToDefault;
        }

        currency.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Results.Ok(currency);
    }

    public static async Task<IResult> DeleteCurrency(SanadDbContext db, Guid id)
    {
        var currency = await db.Currencies.FindAsync(id);
        if (currency is null) return Results.NotFound();

        if (currency.IsDefault)
            return Results.BadRequest("Cannot delete the default currency.");

        var hasAssets = await db.Assets.AnyAsync(a => a.CurrencyId == id);
        var hasDebts = await db.Debts.AnyAsync(d => d.CurrencyId == id);
        if (hasAssets || hasDebts)
            return Results.BadRequest("Cannot delete currency because it is used by assets or debts.");

        db.Currencies.Remove(currency);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    public static async Task<IResult> SetDefaultCurrency(SanadDbContext db, Guid id)
    {
        var newDefault = await db.Currencies.FindAsync(id);
        if (newDefault is null) return Results.NotFound();
        
        if (newDefault.IsDefault) return Results.Ok(newDefault);

        var currentDefault = await db.Currencies.FirstOrDefaultAsync(c => c.IsDefault);
        
        var allCurrencies = await db.Currencies.ToListAsync();
        var newDefaultRate = newDefault.ExchangeRateToDefault;

        if (newDefaultRate <= 0) 
            return Results.BadRequest("Invalid exchange rate for the new default currency.");

        foreach (var currency in allCurrencies)
        {
            if (currency.Id == newDefault.Id)
            {
                currency.IsDefault = true;
                currency.ExchangeRateToDefault = 1.0m;
            }
            else
            {
                currency.IsDefault = false;
                // If JOD was 1.41 USD, and JOD becomes default:
                // USD rate becomes 1 / 1.41
                // EUR rate (which was say 1.10 USD) becomes 1.10 / 1.41
                currency.ExchangeRateToDefault = Math.Round(currency.ExchangeRateToDefault / newDefaultRate, 6);
            }
            currency.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
        return Results.Ok(newDefault);
    }
}

public record MonthlyBudgetRequest(decimal Amount, int? Month, int? Year);
public record UpdateTransactionRequest(decimal? Amount, string? Description, Guid? CategoryId, DateTime? Date, string? Type);
