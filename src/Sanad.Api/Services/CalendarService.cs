using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sanad.Api.Data;
using Sanad.Api.Models;

namespace Sanad.Api.Services;

public class CalendarService : ICalendarService
{
    private readonly SanadDbContext _db;

    public CalendarService(SanadDbContext db)
    {
        _db = db;
    }

    public async Task<List<EventCategory>> GetCategoriesAsync()
    {
        return await _db.EventCategories
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<EventCategory> CreateCategoryAsync(string name, string colorCode)
    {
        var category = new EventCategory
        {
            Id = Guid.NewGuid(),
            Name = name,
            ColorCode = colorCode,
            CreatedAt = DateTime.UtcNow
        };
        _db.EventCategories.Add(category);
        await _db.SaveChangesAsync();
        return category;
    }

    public async Task<EventCategory> CreateCategoryAsync(EventCategory category)
    {
        if (category.Id == Guid.Empty)
            category.Id = Guid.NewGuid();
        if (category.CreatedAt == default)
            category.CreatedAt = DateTime.UtcNow;

        _db.EventCategories.Add(category);
        await _db.SaveChangesAsync();
        return category;
    }

    public async Task<EventCategory?> UpdateCategoryAsync(Guid id, string name, string colorCode)
    {
        var category = await _db.EventCategories.FindAsync(id);
        if (category == null) return null;

        category.Name = name;
        category.ColorCode = colorCode;
        await _db.SaveChangesAsync();
        return category;
    }

    public async Task<bool> DeleteCategoryAsync(Guid id)
    {
        var category = await _db.EventCategories.FindAsync(id);
        if (category == null) return false;

        var events = await _db.CalendarEvents.Where(e => e.CategoryId == id).ToListAsync();
        foreach (var evt in events)
        {
            evt.CategoryId = null;
        }

        _db.EventCategories.Remove(category);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<CalendarEvent>> GetEventsAsync(DateTime? start = null, DateTime? end = null)
    {
        var query = _db.CalendarEvents
            .Include(e => e.Category)
            .Include(e => e.TaskItem)
            .AsQueryable();

        if (start.HasValue)
        {
            query = query.Where(e => (e.EndDate >= start.Value) || e.RecurrenceRule != null);
        }
        if (end.HasValue)
        {
            query = query.Where(e => (e.StartDate <= end.Value) || e.RecurrenceRule != null);
        }

        return await query.ToListAsync();
    }

    public async Task<CalendarEvent> CreateEventAsync(CalendarEvent evt)
    {
        if (evt.Id == Guid.Empty)
            evt.Id = Guid.NewGuid();
        if (evt.CreatedAt == default)
            evt.CreatedAt = DateTime.UtcNow;
        evt.UpdatedAt = DateTime.UtcNow;

        _db.CalendarEvents.Add(evt);

        if (evt.TaskItemId.HasValue)
        {
            var task = await _db.TaskItems.FindAsync(evt.TaskItemId.Value);
            if (task != null)
            {
                task.StartDate = evt.StartDate;
                task.EndDate = evt.EndDate;
                task.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync();

        return await _db.CalendarEvents
            .Include(e => e.Category)
            .Include(e => e.TaskItem)
            .FirstOrDefaultAsync(e => e.Id == evt.Id) ?? evt;
    }

    public async Task<CalendarEvent> CreateEventAsync(
        string title, string? description, DateTime startDate, DateTime endDate, bool isAllDay,
        string? recurrenceRule, int? notificationPreference, Guid? categoryId, Guid? taskItemId)
    {
        var evt = new CalendarEvent
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            StartDate = startDate,
            EndDate = endDate,
            IsAllDay = isAllDay,
            RecurrenceRule = recurrenceRule,
            NotificationPreference = notificationPreference,
            CategoryId = categoryId,
            TaskItemId = taskItemId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return await CreateEventAsync(evt);
    }

    public async Task<CalendarEvent?> UpdateEventAsync(Guid id, CalendarEvent updated)
    {
        var evt = await _db.CalendarEvents.FindAsync(id);
        if (evt == null) return null;

        evt.Title = updated.Title;
        evt.Description = updated.Description;
        evt.StartDate = updated.StartDate;
        evt.EndDate = updated.EndDate;
        evt.IsAllDay = updated.IsAllDay;
        evt.RecurrenceRule = updated.RecurrenceRule;
        evt.CategoryId = updated.CategoryId;
        evt.TaskItemId = updated.TaskItemId;
        evt.NotificationPreference = updated.NotificationPreference;
        evt.UpdatedAt = DateTime.UtcNow;

        if (evt.TaskItemId.HasValue)
        {
            var task = await _db.TaskItems.FindAsync(evt.TaskItemId.Value);
            if (task != null)
            {
                task.StartDate = evt.StartDate;
                task.EndDate = evt.EndDate;
                task.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync();

        return await _db.CalendarEvents
            .Include(e => e.Category)
            .Include(e => e.TaskItem)
            .FirstOrDefaultAsync(e => e.Id == evt.Id) ?? evt;
    }

    public async Task<CalendarEvent?> UpdateEventAsync(
        Guid id, string title, string? description, DateTime startDate, DateTime endDate, bool isAllDay,
        string? recurrenceRule, int? notificationPreference, Guid? categoryId, Guid? taskItemId)
    {
        var updated = new CalendarEvent
        {
            Title = title,
            Description = description,
            StartDate = startDate,
            EndDate = endDate,
            IsAllDay = isAllDay,
            RecurrenceRule = recurrenceRule,
            CategoryId = categoryId,
            TaskItemId = taskItemId,
            NotificationPreference = notificationPreference
        };
        return await UpdateEventAsync(id, updated);
    }

    public async Task<bool> DeleteEventAsync(Guid id)
    {
        var evt = await _db.CalendarEvents.FindAsync(id);
        if (evt == null) return false;

        if (evt.TaskItemId.HasValue)
        {
            var task = await _db.TaskItems.FindAsync(evt.TaskItemId.Value);
            if (task != null)
            {
                task.StartDate = null;
                task.EndDate = null;
                task.UpdatedAt = DateTime.UtcNow;
            }
        }

        _db.CalendarEvents.Remove(evt);
        await _db.SaveChangesAsync();
        return true;
    }
}
