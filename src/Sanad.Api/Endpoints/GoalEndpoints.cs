using Sanad.Api.Data;
using Sanad.Api.Models;
using Sanad.Api.Services;

namespace Sanad.Api.Endpoints;

public static class GoalEndpoints
{
    public static void MapGoalEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/goals/{dateStr}", (IGoalService svc, string dateStr) => GetGoal(svc, dateStr));
        app.MapPut("/api/goals/{dateStr}", (IGoalService svc, string dateStr, DailyGoal input) => UpdateGoal(svc, dateStr, input));
    }

    public static async Task<IResult> GetGoal(IGoalService svc, string dateStr)
    {
        var goal = await svc.GetGoalAsync(dateStr);
        if (goal == null) return Results.NoContent();
        return Results.Ok(goal);
    }

    public static Task<IResult> GetGoal(SanadDbContext db, string dateStr) =>
        GetGoal(new GoalService(db), dateStr);

    public static async Task<IResult> UpdateGoal(IGoalService svc, string dateStr, DailyGoal input)
    {
        var goal = await svc.SetGoalAsync(dateStr, input.Goal);
        return Results.Ok(goal);
    }

    public static Task<IResult> UpdateGoal(SanadDbContext db, string dateStr, DailyGoal input) =>
        UpdateGoal(new GoalService(db), dateStr, input);
}
