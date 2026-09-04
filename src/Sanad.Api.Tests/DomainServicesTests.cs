using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sanad.Api.Data;
using Sanad.Api.Models;
using Sanad.Api.Services;
using Xunit;

namespace Sanad.Api.Tests;

public class DomainServicesTests
{
    private class DummyTenantProvider : ITenantProvider
    {
        public string GetUsername() => "testuser";
        public Guid GetTenantId() => Guid.Empty;
        public string GetConnectionString() => "";
        public string GetTenantBasePath() => "/tmp/dummy_tenant";
    }

    [Fact]
    public async Task ReadingService_CalculatesProgressAndAutoCompletes()
    {
        var options = new DbContextOptionsBuilder<SanadDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var db = new SanadDbContext(options);
        var readingService = new ReadingService(db);

        var book = new Book { Title = "Atomic Habits", Author = "James Clear", TotalPages = 200 };
        db.Books.Add(book);
        await db.SaveChangesAsync();

        // 1. Start period with plans
        var plans = new List<PlanDto>
        {
            new PlanDto("Chapter 1", 1, 50),
            new PlanDto("Chapter 2", 50, 100),
            new PlanDto("Chapter 3", 100, 200)
        };
        var period = await readingService.StartReadingPeriodAsync(book.Id, plans);
        Assert.Equal("Reading", period.Status);

        // 2. Log reading: pages 1 to 40
        var log = await readingService.LogReadingAsync(period.Id, 1, 40);
        Assert.NotNull(log);
        Assert.Equal(40, log.EndPage);

        // Check progress
        var progress = await readingService.GetCurrentReadingAsync();
        Assert.NotNull(progress);
        Assert.Equal(40, progress.CurrentPage);
        Assert.Equal("Chapter 1", progress.CurrentChapter);
        Assert.Equal(10, progress.PagesLeftInChapter);

        // 3. Log reading to 200 (finishes book)
        await readingService.LogReadingAsync(period.Id, 40, 200);

        var finishedPeriod = await db.ReadingPeriods.FindAsync(period.Id);
        Assert.NotNull(finishedPeriod);
        Assert.Equal("Completed", finishedPeriod.Status);
        Assert.NotNull(finishedPeriod.EndDate);
    }

    [Fact]
    public async Task DebtService_CreatesSnapshotsOnAmountChanges()
    {
        var options = new DbContextOptionsBuilder<SanadDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var db = new SanadDbContext(options);
        var debtService = new DebtService(db);

        // Create debt -> initial snapshot
        var debt = await debtService.CreateDebtAsync("Student Loan", "Loan", 10000m);
        Assert.Equal(1, await db.DebtSnapshots.CountAsync());

        // Update with same amount -> no new snapshot
        await debtService.UpdateDebtAsync(debt.Id, "Student Loan", "Loan", 10000m);
        Assert.Equal(1, await db.DebtSnapshots.CountAsync());

        // Update with new amount -> new snapshot created
        await debtService.UpdateDebtAsync(debt.Id, "Student Loan", "Loan", 9000m);
        Assert.Equal(2, await db.DebtSnapshots.CountAsync());

        var latest = await db.DebtSnapshots.OrderByDescending(s => s.RecordedAt).FirstAsync();
        Assert.Equal(9000m, latest.Amount);

        // Delete debt -> cleans up debt and its snapshots
        var deleted = await debtService.DeleteDebtAsync(debt.Id);
        Assert.True(deleted);
        Assert.Equal(0, await db.Debts.CountAsync());
        Assert.Equal(0, await db.DebtSnapshots.CountAsync());
    }

    [Fact]
    public async Task ThoughtService_CrudOperationsWork()
    {
        var options = new DbContextOptionsBuilder<SanadDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var db = new SanadDbContext(options);
        var thoughtService = new ThoughtService(db);

        var thought = await thoughtService.CreateThoughtAsync("Deep thought about services");
        Assert.NotNull(thought);
        Assert.Equal("Deep thought about services", thought.Content);

        var list = await thoughtService.GetThoughtsAsync(1, 10, "services");
        Assert.Single(list);

        var updated = await thoughtService.UpdateThoughtAsync(thought.Id, "Updated thought");
        Assert.NotNull(updated);
        Assert.Equal("Updated thought", updated.Content);

        var deleted = await thoughtService.DeleteThoughtAsync(thought.Id);
        Assert.True(deleted);
        Assert.Empty(await thoughtService.GetThoughtsAsync());
    }

    [Fact]
    public async Task HabitService_TogglingAndReorderingWorks()
    {
        var options = new DbContextOptionsBuilder<SanadDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var db = new SanadDbContext(options);
        var habitService = new HabitService(db);

        var h1 = await habitService.CreateHabitAsync("Workout", "dumbbell", "daily");
        var h2 = await habitService.CreateHabitAsync("Read", "book", "daily");

        var today = DateTime.UtcNow.Date;
        var log = await habitService.ToggleHabitLogAsync(h1.Id, today);
        Assert.NotNull(log);
        Assert.True(log.Completed);

        // Toggle again -> completed false
        var log2 = await habitService.ToggleHabitLogAsync(h1.Id, today);
        Assert.NotNull(log2);
        Assert.False(log2.Completed);

        // Reorder
        await habitService.ReorderHabitsAsync(new List<string> { h2.Id, h1.Id });
        var habits = await habitService.GetHabitsAsync();
        Assert.Equal(h2.Id, habits[0].Id);
        Assert.Equal(h1.Id, habits[1].Id);
    }
}
