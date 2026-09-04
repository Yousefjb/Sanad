using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sanad.Api.Models;

namespace Sanad.Api.Services;

public interface IHabitService
{
    Task<List<Habit>> GetHabitsAsync();
    Task<Habit> CreateHabitAsync(string name, string icon, string frequency);
    Task<Habit> CreateHabitAsync(Habit habit);
    Task<Habit?> UpdateHabitAsync(string id, string name, string icon, string frequency);
    Task<bool> DeleteHabitAsync(string id);
    Task<HabitLog?> ToggleHabitLogAsync(string id, DateTime date);
    Task<bool> ReorderHabitsAsync(List<string> habitIds);
}
