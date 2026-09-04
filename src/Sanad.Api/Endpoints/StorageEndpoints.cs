using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Sanad.Api.Data;
using Sanad.Api.Services;

namespace Sanad.Api.Endpoints;

public static class StorageEndpoints
{
    public static void MapStorageEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/storage").RequireAuthorization();

        group.MapGet("/", async (IStorageService storageService, HttpContext context) =>
        {
            var username = context.User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return Results.Unauthorized();

            try
            {
                var status = await storageService.GetStorageStatusAsync(username);
                return Results.Ok(new
                {
                    diskUsed = status.DiskUsedBytes,
                    diskLimitBytes = status.DiskLimitBytes,
                    isAdmin = status.IsAdmin
                });
            }
            catch (InvalidOperationException)
            {
                return Results.NotFound();
            }
        });

        group.MapGet("/tiers", async (IStorageService storageService) =>
        {
            var tiers = await storageService.GetTiersAsync();
            return Results.Ok(tiers);
        }).AllowAnonymous();

        group.MapGet("/paddle-config", async (AdminDbContext db) =>
        {
            var settings = await db.SystemSettings.ToDictionaryAsync(s => s.Key, s => s.Value);
            var isEnabled = settings.GetValueOrDefault("IsPaddleEnabled") == "true";
            var token = settings.GetValueOrDefault("PaddleClientToken", "");
            var env = settings.GetValueOrDefault("PaddleEnvironment", "sandbox");
            
            return Results.Ok(new {
                enabled = isEnabled,
                token,
                environment = env
            });
        }).AllowAnonymous();

        group.MapGet("/history", async (AdminDbContext db, HttpContext context) =>
        {
            var username = context.User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return Results.Unauthorized();

            var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return Results.NotFound();

            var history = await db.SubscriptionHistories
                .Include(s => s.Tier)
                .Where(s => s.UserId == user.Id)
                .OrderByDescending(s => s.StartedAt)
                .Select(s => new {
                    tierName = s.Tier != null ? s.Tier.Name : "Unknown",
                    startedAt = s.StartedAt,
                    endedAt = s.EndedAt
                })
                .ToListAsync();

            return Results.Ok(history);
        });
    }
}
