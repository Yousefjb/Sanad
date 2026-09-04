using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sanad.Api.Data;
using Sanad.Api.Models;

namespace Sanad.Api.Services;

public class HabitService : IHabitService
{
    private readonly SanadDbContext _db;

    public HabitService(SanadDbContext db)
    {
        _db = db;
    }

    public async Task<List<Habit>> GetHabitsAsync()
    {
        return await _db.Habits
            .Include(h => h.Logs)
            .Where(h => !h.IsDeleted)
            .OrderBy(h => h.Order)
            .ThenByDescending(h => h.CreatedAt)
            .ToListAsync();
    }

    public async Task<Habit> CreateHabitAsync(string name, string icon, string frequency)
    {
        var habit = new Habit
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Icon = icon,
            Frequency = frequency,
            CreatedAt = DateTime.UtcNow
        };
        _db.Habits.Add(habit);
        await _db.SaveChangesAsync();
        return habit;
    }

    public async Task<Habit> CreateHabitAsync(Habit habit)
    {
        if (string.IsNullOrEmpty(habit.Id))
            habit.Id = Guid.NewGuid().ToString();
        if (habit.CreatedAt == default)
            habit.CreatedAt = DateTime.UtcNow;

        _db.Habits.Add(habit);
        await _db.SaveChangesAsync();
        return habit;
    }

    public async Task<Habit?> UpdateHabitAsync(string id, string name, string icon, string frequency)
    {
        var habit = await _db.Habits.FindAsync(id);
        if (habit == null || habit.IsDeleted) return null;

        habit.Name = name;
        habit.Icon = icon;
        habit.Frequency = frequency;

        await _db.SaveChangesAsync();
        return habit;
    }

    public async Task<bool> DeleteHabitAsync(string id)
    {
        var habit = await _db.Habits.FindAsync(id);
        if (habit == null || habit.IsDeleted) return false;

        habit.IsDeleted = true;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<HabitLog?> ToggleHabitLogAsync(string id, DateTime date)
    {
        var habit = await _db.Habits.FindAsync(id);
        if (habit == null || habit.IsDeleted) return null;

        var targetDate = date.Date;
        var log = await _db.HabitLogs.FirstOrDefaultAsync(l => l.HabitId == id && l.Date.Date == targetDate);
        if (log != null)
        {
            log.Completed = !log.Completed;
        }
        else
        {
            log = new HabitLog
            {
                Id = Guid.NewGuid().ToString(),
                HabitId = id,
                Date = targetDate,
                Completed = true
            };
            _db.HabitLogs.Add(log);
        }

        await _db.SaveChangesAsync();
        return log;
    }

    public async Task<bool> ReorderHabitsAsync(List<string> habitIds)
    {
        var habits = await _db.Habits.Where(h => habitIds.Contains(h.Id)).ToListAsync();

        for (int i = 0; i < habitIds.Count; i++)
        {
            var habit = habits.FirstOrDefault(h => h.Id == habitIds[i]);
            if (habit != null)
            {
                habit.Order = i;
            }
        }

        await _db.SaveChangesAsync();
        return true;
    }
}
