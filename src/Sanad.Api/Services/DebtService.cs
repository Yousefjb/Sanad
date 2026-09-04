using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sanad.Api.Data;
using Sanad.Api.Models;

namespace Sanad.Api.Services;

public class DebtService : IDebtService
{
    private readonly SanadDbContext _db;

    public DebtService(SanadDbContext db)
    {
        _db = db;
    }

    public async Task<List<Debt>> GetDebtsAsync()
    {
        return await _db.Debts
            .Include(d => d.Currency)
            .OrderBy(d => d.Order)
            .ThenByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task<Debt> CreateDebtAsync(Debt debt)
    {
        if (debt.Id == Guid.Empty)
            debt.Id = Guid.NewGuid();
        if (debt.CreatedAt == default)
            debt.CreatedAt = DateTime.UtcNow;
        debt.UpdatedAt = DateTime.UtcNow;

        debt.Order = (await _db.Debts.MaxAsync(d => (int?)d.Order) ?? 0) + 1;

        _db.Debts.Add(debt);

        var snapshot = new DebtSnapshot
        {
            DebtId = debt.Id,
            Amount = debt.CurrentAmount,
            RecordedAt = DateTime.UtcNow
        };
        _db.DebtSnapshots.Add(snapshot);

        await _db.SaveChangesAsync();
        return debt;
    }

    public async Task<Debt> CreateDebtAsync(string name, string type, decimal currentAmount, Guid? currencyId = null, string? icon = null)
    {
        var debt = new Debt
        {
            Id = Guid.NewGuid(),
            Name = name,
            Type = type,
            CurrentAmount = currentAmount,
            CurrencyId = currencyId,
            Icon = icon,
            Order = (await _db.Debts.MaxAsync(d => (int?)d.Order) ?? 0) + 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Debts.Add(debt);

        var snapshot = new DebtSnapshot
        {
            DebtId = debt.Id,
            Amount = debt.CurrentAmount,
            RecordedAt = DateTime.UtcNow
        };
        _db.DebtSnapshots.Add(snapshot);

        await _db.SaveChangesAsync();
        return debt;
    }

    public async Task<Debt?> UpdateDebtAsync(Guid id, Debt updated)
    {
        return await UpdateDebtAsync(id, updated.Name, updated.Type, updated.CurrentAmount, updated.CurrencyId, updated.Icon);
    }

    public async Task<Debt?> UpdateDebtAsync(Guid id, string name, string type, decimal currentAmount, Guid? currencyId = null, string? icon = null)
    {
        var debt = await _db.Debts.FindAsync(id);
        if (debt == null) return null;

        debt.Name = name;
        debt.Type = type;
        debt.CurrencyId = currencyId;
        debt.Icon = icon;

        if (debt.CurrentAmount != currentAmount)
        {
            debt.CurrentAmount = currentAmount;
            var snapshot = new DebtSnapshot
            {
                DebtId = debt.Id,
                Amount = debt.CurrentAmount,
                RecordedAt = DateTime.UtcNow
            };
            _db.DebtSnapshots.Add(snapshot);
        }

        debt.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return debt;
    }

    public async Task<bool> DeleteDebtAsync(Guid id)
    {
        var debt = await _db.Debts.FindAsync(id);
        if (debt == null) return false;

        var snapshots = await _db.DebtSnapshots.Where(s => s.DebtId == id).ToListAsync();
        _db.DebtSnapshots.RemoveRange(snapshots);

        _db.Debts.Remove(debt);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ReorderDebtsAsync(List<Guid> orderedIds)
    {
        var debts = await _db.Debts.Where(d => orderedIds.Contains(d.Id)).ToListAsync();
        for (int i = 0; i < orderedIds.Count; i++)
        {
            var debt = debts.FirstOrDefault(d => d.Id == orderedIds[i]);
            if (debt != null)
            {
                debt.Order = i;
            }
        }
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<object> GetDebtsHistoryAsync()
    {
        var snapshots = await _db.DebtSnapshots
            .Include(s => s.Debt)
                .ThenInclude(d => d!.Currency)
            .OrderBy(s => s.RecordedAt)
            .ToListAsync();

        return snapshots.Select(s => new
        {
            s.Id,
            s.DebtId,
            DebtName = s.Debt?.Name,
            DebtType = s.Debt?.Type,
            s.Amount,
            ExchangeRateToDefault = s.Debt?.Currency?.ExchangeRateToDefault ?? 1m,
            s.RecordedAt
        });
    }
}
