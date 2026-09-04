using System.Collections.Generic;
using System.Threading.Tasks;
using Sanad.Api.Models;

namespace Sanad.Api.Services;

public record ReadingProgressDto(
    ReadingPeriod Period,
    int CurrentPage,
    string? CurrentChapter,
    int PagesLeftInChapter
);

public interface IReadingService
{
    Task<List<ReadingPeriod>> GetReadingPeriodsAsync();
    Task<ReadingProgressDto?> GetCurrentReadingAsync();
    Task<ReadingPeriod> StartReadingPeriodAsync(int bookId, List<PlanDto>? plans = null);
    Task<ReadingLog?> LogReadingAsync(int readingPeriodId, int startPage, int endPage);
    Task<ReadingPeriod?> UpdateStatusAsync(int id, string status);
    Task<List<ReadingPlan>?> UpdatePlansAsync(int id, List<PlanDto> plans);
    Task<bool> DeleteReadingPeriodAsync(int id);
}

public record PlanDto(string Title, int StartPage, int EndPage);
