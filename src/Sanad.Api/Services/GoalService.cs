using System;
using System.Threading.Tasks;
using Sanad.Api.Data;
using Sanad.Api.Models;

namespace Sanad.Api.Services;

public class GoalService : IGoalService
{
    private readonly SanadDbContext _db;

    public GoalService(SanadDbContext db)
    {
        _db = db;
    }

    public async Task<DailyGoal?> GetGoalAsync(string dateStr)
    {
        return await _db.DailyGoals.FindAsync(dateStr);
    }

    public async Task<DailyGoal?> GetTodaysGoalAsync()
    {
        var dateStr = DateTime.Now.ToString("yyyy-MM-dd");
        return await GetGoalAsync(dateStr);
    }

    public async Task<DailyGoal> SetGoalAsync(string dateStr, string goalText)
    {
        var goal = await _db.DailyGoals.FindAsync(dateStr);
        if (goal == null)
        {
            goal = new DailyGoal { DateStr = dateStr, Goal = goalText };
            _db.DailyGoals.Add(goal);
        }
        else
        {
            goal.Goal = goalText;
        }

        await _db.SaveChangesAsync();
        return goal;
    }
}
