using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sanad.Api.Models;

namespace Sanad.Api.Services;

public interface IDebtService
{
    Task<List<Debt>> GetDebtsAsync();
    Task<Debt> CreateDebtAsync(Debt debt);
    Task<Debt> CreateDebtAsync(string name, string type, decimal currentAmount, Guid? currencyId = null, string? icon = null);
    Task<Debt?> UpdateDebtAsync(Guid id, Debt updated);
    Task<Debt?> UpdateDebtAsync(Guid id, string name, string type, decimal currentAmount, Guid? currencyId = null, string? icon = null);
    Task<bool> DeleteDebtAsync(Guid id);
    Task<bool> ReorderDebtsAsync(List<Guid> orderedIds);
    Task<object> GetDebtsHistoryAsync();
}
