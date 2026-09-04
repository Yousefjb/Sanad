using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sanad.Api.Models;

namespace Sanad.Api.Services;

public interface IFinanceService
{
    Task<List<TransactionCategory>> GetCategoriesAsync();
    Task<TransactionCategory> CreateCategoryAsync(string name, decimal monthlyBudget, string colorHex = "#cccccc");
    Task<TransactionCategory> CreateCategoryAsync(TransactionCategory category);
    Task<TransactionCategory?> UpdateCategoryAsync(Guid id, string name, decimal monthlyBudget, string colorHex);
    Task<TransactionCategory?> UpdateCategoryAsync(Guid id, TransactionCategory updated);

    Task<(List<Transaction> Items, int TotalCount, bool HasMore)> GetTransactionsPaginatedAsync(
        int? month, int? year, int page = 1, int pageSize = 15, string? search = null, Guid? categoryId = null);
    Task<List<Transaction>> GetRecentTransactionsAsync(int count = 20);
    Task<Transaction?> CreateTransactionAsync(Transaction transaction);
    Task<Transaction?> UpdateTransactionAsync(Guid id, decimal? amount, string? description, Guid? categoryId, DateTime? date, string? type);
    Task<bool> DeleteTransactionAsync(Guid id);

    Task<object> GetSummaryAsync(int? month, int? year);
    Task<decimal> GetMonthlyBudgetAsync(int? month, int? year);
    Task<MonthlyBudget> SetMonthlyBudgetAsync(int? month, int? year, decimal amount);

    Task<List<Currency>> GetCurrenciesAsync();
    Task<Currency> CreateCurrencyAsync(Currency currency);
    Task<Currency?> UpdateCurrencyAsync(Guid id, Currency updated);
    Task<bool> DeleteCurrencyAsync(Guid id);
    Task<bool> SetDefaultCurrencyAsync(Guid id);
}
