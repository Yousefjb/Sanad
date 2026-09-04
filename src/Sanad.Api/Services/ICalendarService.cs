using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sanad.Api.Models;

namespace Sanad.Api.Services;

public interface ICalendarService
{
    Task<List<EventCategory>> GetCategoriesAsync();
    Task<EventCategory> CreateCategoryAsync(string name, string colorCode);
    Task<EventCategory> CreateCategoryAsync(EventCategory category);
    Task<EventCategory?> UpdateCategoryAsync(Guid id, string name, string colorCode);
    Task<bool> DeleteCategoryAsync(Guid id);

    Task<List<CalendarEvent>> GetEventsAsync(DateTime? start = null, DateTime? end = null);
    Task<CalendarEvent> CreateEventAsync(CalendarEvent evt);
    Task<CalendarEvent> CreateEventAsync(string title, string? description, DateTime startDate, DateTime endDate, bool isAllDay, string? recurrenceRule, int? notificationPreference, Guid? categoryId, Guid? taskItemId);
    Task<CalendarEvent?> UpdateEventAsync(Guid id, CalendarEvent updated);
    Task<CalendarEvent?> UpdateEventAsync(Guid id, string title, string? description, DateTime startDate, DateTime endDate, bool isAllDay, string? recurrenceRule, int? notificationPreference, Guid? categoryId, Guid? taskItemId);
    Task<bool> DeleteEventAsync(Guid id);
}
