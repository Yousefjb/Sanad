using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sanad.Api.Data;
using Sanad.Api.Models;

namespace Sanad.Api.Services;

public class FinanceService : IFinanceService
{
    private readonly SanadDbContext _db;

    public FinanceService(SanadDbContext db)
    {
        _db = db;
    }

    public async Task<List<TransactionCategory>> GetCategoriesAsync() =>
        await _db.TransactionCategories.ToListAsync();

    public async Task<TransactionCategory> CreateCategoryAsync(string name, decimal monthlyBudget, string colorHex = "#cccccc")
    {
        var category = new TransactionCategory
        {
            Id = Guid.NewGuid(),
            Name = name,
            MonthlyBudget = monthlyBudget,
            ColorHex = colorHex
        };
        _db.TransactionCategories.Add(category);
        await _db.SaveChangesAsync();
        return category;
    }

    public async Task<TransactionCategory> CreateCategoryAsync(TransactionCategory category)
    {
        if (category.Id == Guid.Empty)
            category.Id = Guid.NewGuid();

        _db.TransactionCategories.Add(category);
        await _db.SaveChangesAsync();
        return category;
    }

    public async Task<TransactionCategory?> UpdateCategoryAsync(Guid id, string name, decimal monthlyBudget, string colorHex)
    {
        var category = await _db.TransactionCategories.FindAsync(id);
        if (category == null) return null;

        category.Name = name;
        category.MonthlyBudget = monthlyBudget;
        category.ColorHex = colorHex;

        await _db.SaveChangesAsync();
        return category;
    }

    public async Task<TransactionCategory?> UpdateCategoryAsync(Guid id, TransactionCategory updated) =>
        await UpdateCategoryAsync(id, updated.Name, updated.MonthlyBudget, updated.ColorHex);

    public async Task<(List<Transaction> Items, int TotalCount, bool HasMore)> GetTransactionsPaginatedAsync(
        int? month, int? year, int page = 1, int pageSize = 15, string? search = null, Guid? categoryId = null)
    {
        var targetMonth = month ?? DateTime.UtcNow.Month;
        var targetYear = year ?? DateTime.UtcNow.Year;
        var startDate = new DateTime(targetYear, targetMonth, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = startDate.AddMonths(1);

        var query = _db.Transactions
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
        var p = page < 1 ? 1 : page;
        var size = pageSize < 1 ? 15 : pageSize;

        var items = await query
            .OrderByDescending(t => t.Date)
            .Skip((p - 1) * size)
            .Take(size)
            .ToListAsync();

        return (items, totalCount, totalCount > p * size);
    }

    public async Task<List<Transaction>> GetRecentTransactionsAsync(int count = 20)
    {
        return await _db.Transactions
            .OrderByDescending(t => t.Date)
            .Take(count)
            .ToListAsync();
    }

    public async Task<Transaction?> CreateTransactionAsync(Transaction transaction)
    {
        var categoryExists = await _db.TransactionCategories.AnyAsync(c => c.Id == transaction.CategoryId);
        if (!categoryExists) return null;

        if (transaction.Id == Guid.Empty)
            transaction.Id = Guid.NewGuid();
        if (transaction.Date == default)
            transaction.Date = DateTime.UtcNow;

        _db.Transactions.Add(transaction);
        await _db.SaveChangesAsync();
        return transaction;
    }

    public async Task<Transaction?> UpdateTransactionAsync(Guid id, decimal? amount, string? description, Guid? categoryId, DateTime? date, string? type)
    {
        var transaction = await _db.Transactions.Include(t => t.Category).FirstOrDefaultAsync(t => t.Id == id);
        if (transaction == null) return null;

        if (amount.HasValue)
            transaction.Amount = amount.Value;

        if (description != null)
            transaction.Description = description;

        if (categoryId.HasValue && categoryId.Value != Guid.Empty)
        {
            var category = await _db.TransactionCategories.FindAsync(categoryId.Value);
            if (category == null) return null;
            transaction.CategoryId = categoryId.Value;
            transaction.Category = category;
        }

        if (date.HasValue)
            transaction.Date = date.Value;

        if (!string.IsNullOrEmpty(type))
            transaction.Type = type;

        await _db.SaveChangesAsync();
        return transaction;
    }

    public async Task<bool> DeleteTransactionAsync(Guid id)
    {
        var transaction = await _db.Transactions.FindAsync(id);
        if (transaction == null) return false;

        _db.Transactions.Remove(transaction);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<object> GetSummaryAsync(int? month, int? year)
    {
        var targetMonth = month ?? DateTime.UtcNow.Month;
        var targetYear = year ?? DateTime.UtcNow.Year;
        var startDate = new DateTime(targetYear, targetMonth, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = startDate.AddMonths(1);

        var categories = await _db.TransactionCategories.ToListAsync();
        var categorySpends = await _db.Transactions
            .Where(t => t.Date >= startDate && t.Date < endDate && t.Type == "Expense")
            .GroupBy(t => t.CategoryId)
            .Select(g => new { CategoryId = g.Key, TotalAmount = g.Sum(t => t.Amount) })
            .ToDictionaryAsync(g => g.CategoryId, g => g.TotalAmount);

        var monthlyBudget = await _db.MonthlyBudgets
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

        return new
        {
            Categories = categorySummary,
            MonthlyBudget = monthlyBudget?.Amount ?? 0m,
            TotalSpent = categorySpends.Values.Sum()
        };
    }

    public async Task<decimal> GetMonthlyBudgetAsync(int? month, int? year)
    {
        var targetMonth = month ?? DateTime.UtcNow.Month;
        var targetYear = year ?? DateTime.UtcNow.Year;

        var budget = await _db.MonthlyBudgets
            .FirstOrDefaultAsync(b => b.Year == targetYear && b.Month == targetMonth);

        return budget?.Amount ?? 0m;
    }

    public async Task<MonthlyBudget> SetMonthlyBudgetAsync(int? month, int? year, decimal amount)
    {
        var targetMonth = month ?? DateTime.UtcNow.Month;
        var targetYear = year ?? DateTime.UtcNow.Year;

        var budget = await _db.MonthlyBudgets
            .FirstOrDefaultAsync(b => b.Year == targetYear && b.Month == targetMonth);

        if (budget == null)
        {
            budget = new MonthlyBudget
            {
                Year = targetYear,
                Month = targetMonth,
                Amount = amount
            };
            _db.MonthlyBudgets.Add(budget);
        }
        else
        {
            budget.Amount = amount;
        }

        await _db.SaveChangesAsync();
        return budget;
    }

    public async Task<List<Currency>> GetCurrenciesAsync() =>
        await _db.Currencies.ToListAsync();

    public async Task<Currency> CreateCurrencyAsync(Currency currency)
    {
        var hasCurrencies = await _db.Currencies.AnyAsync();
        currency.IsDefault = !hasCurrencies;
        currency.CreatedAt = DateTime.UtcNow;
        currency.UpdatedAt = DateTime.UtcNow;

        _db.Currencies.Add(currency);
        await _db.SaveChangesAsync();
        return currency;
    }

    public async Task<Currency?> UpdateCurrencyAsync(Guid id, Currency updated)
    {
        var currency = await _db.Currencies.FindAsync(id);
        if (currency == null) return null;

        currency.Name = updated.Name;
        currency.Code = updated.Code;
        currency.Symbol = updated.Symbol;

        if (!currency.IsDefault)
        {
            currency.ExchangeRateToDefault = updated.ExchangeRateToDefault;
        }

        currency.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return currency;
    }

    public async Task<bool> DeleteCurrencyAsync(Guid id)
    {
        var currency = await _db.Currencies.FindAsync(id);
        if (currency == null || currency.IsDefault) return false;

        var hasAssets = await _db.Assets.AnyAsync(a => a.CurrencyId == id);
        var hasDebts = await _db.Debts.AnyAsync(d => d.CurrencyId == id);
        if (hasAssets || hasDebts) return false;

        _db.Currencies.Remove(currency);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SetDefaultCurrencyAsync(Guid id)
    {
        var newDefault = await _db.Currencies.FindAsync(id);
        if (newDefault == null) return false;

        if (newDefault.IsDefault) return true;

        var newDefaultRate = newDefault.ExchangeRateToDefault;
        if (newDefaultRate <= 0) return false;

        var allCurrencies = await _db.Currencies.ToListAsync();
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
                currency.ExchangeRateToDefault = Math.Round(currency.ExchangeRateToDefault / newDefaultRate, 6);
            }
            currency.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return true;
    }
}
