using System.Threading.Tasks;
using Sanad.Api.Models;

namespace Sanad.Api.Services;

public interface IGoalService
{
    Task<DailyGoal?> GetGoalAsync(string dateStr);
    Task<DailyGoal?> GetTodaysGoalAsync();
    Task<DailyGoal> SetGoalAsync(string dateStr, string goalText);
}
