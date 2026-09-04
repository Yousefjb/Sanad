using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sanad.Api.Data;
using Sanad.Api.Models;

namespace Sanad.Api.Services;

public class ReadingService : IReadingService
{
    private readonly SanadDbContext _db;

    public ReadingService(SanadDbContext db)
    {
        _db = db;
    }

    public async Task<List<ReadingPeriod>> GetReadingPeriodsAsync()
    {
        return await _db.ReadingPeriods
            .Include(p => p.Book)
            .Include(p => p.Plans)
            .Include(p => p.Logs)
            .OrderByDescending(p => p.StartDate)
            .ToListAsync();
    }

    public async Task<ReadingProgressDto?> GetCurrentReadingAsync()
    {
        var current = await _db.ReadingPeriods
            .Include(p => p.Book)
            .Include(p => p.Plans)
            .Include(p => p.Logs)
            .Where(p => p.Status == "Reading")
            .OrderByDescending(p => p.StartDate)
            .FirstOrDefaultAsync();

        if (current == null) return null;

        var highestPage = current.Logs.Any() ? current.Logs.Max(l => l.EndPage) : 0;
        var currentPlan = current.Plans.OrderBy(p => p.OrderIndex)
            .FirstOrDefault(p => highestPage >= p.StartPage && highestPage < p.EndPage)
            ?? current.Plans.OrderBy(p => p.OrderIndex).FirstOrDefault(p => highestPage < p.StartPage)
            ?? current.Plans.LastOrDefault();

        return new ReadingProgressDto(
            Period: current,
            CurrentPage: highestPage,
            CurrentChapter: currentPlan?.Title,
            PagesLeftInChapter: currentPlan != null ? (currentPlan.EndPage - highestPage) : 0
        );
    }

    public async Task<ReadingPeriod> StartReadingPeriodAsync(int bookId, List<PlanDto>? plans = null)
    {
        var period = new ReadingPeriod
        {
            BookId = bookId,
            Status = "Reading",
            StartDate = DateTime.UtcNow,
            Plans = plans != null
                ? plans.Select((p, i) => new ReadingPlan
                {
                    Title = p.Title,
                    StartPage = p.StartPage,
                    EndPage = p.EndPage,
                    OrderIndex = i
                }).ToList()
                : new List<ReadingPlan>()
        };

        _db.ReadingPeriods.Add(period);
        await _db.SaveChangesAsync();
        return period;
    }

    public async Task<ReadingLog?> LogReadingAsync(int readingPeriodId, int startPage, int endPage)
    {
        var period = await _db.ReadingPeriods
            .Include(p => p.Book)
            .FirstOrDefaultAsync(p => p.Id == readingPeriodId);

        if (period == null) return null;

        var log = new ReadingLog
        {
            ReadingPeriodId = readingPeriodId,
            Date = DateTime.UtcNow,
            StartPage = startPage,
            EndPage = endPage
        };
        _db.ReadingLogs.Add(log);

        if (period.Book != null && endPage >= period.Book.TotalPages)
        {
            period.Status = "Completed";
            period.EndDate = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return log;
    }

    public async Task<ReadingPeriod?> UpdateStatusAsync(int id, string status)
    {
        var period = await _db.ReadingPeriods.FindAsync(id);
        if (period == null) return null;

        if (status == "Reading")
        {
            var otherActive = await _db.ReadingPeriods.Where(p => p.Status == "Reading" && p.Id != id).ToListAsync();
            foreach (var other in otherActive)
            {
                other.Status = "Paused";
            }
        }

        period.Status = status;
        await _db.SaveChangesAsync();
        return period;
    }

    public async Task<List<ReadingPlan>?> UpdatePlansAsync(int id, List<PlanDto> plans)
    {
        var period = await _db.ReadingPeriods.Include(p => p.Plans).FirstOrDefaultAsync(p => p.Id == id);
        if (period == null) return null;

        _db.ReadingPlans.RemoveRange(period.Plans);
        period.Plans = plans.Select((p, i) => new ReadingPlan
        {
            Title = p.Title,
            StartPage = p.StartPage,
            EndPage = p.EndPage,
            OrderIndex = i
        }).ToList();

        await _db.SaveChangesAsync();
        return period.Plans;
    }

    public async Task<bool> DeleteReadingPeriodAsync(int id)
    {
        var period = await _db.ReadingPeriods.FindAsync(id);
        if (period == null) return false;

        _db.ReadingPeriods.Remove(period);
        await _db.SaveChangesAsync();
        return true;
    }
}
