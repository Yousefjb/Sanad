using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sanad.Api.Data;
using Sanad.Api.Models;

namespace Sanad.Api.Services;

public class AppService : IAppService
{
    private readonly SanadDbContext _db;

    public AppService(SanadDbContext db)
    {
        _db = db;
    }

    public async Task<List<CustomApp>> GetAppsAsync()
    {
        return await _db.CustomApps.OrderByDescending(a => a.CreatedAt).ToListAsync();
    }

    public async Task<CustomApp?> GetAppByIdAsync(Guid id)
    {
        return await _db.CustomApps.FindAsync(id);
    }

    public async Task<CustomApp> CreateAppAsync(CustomApp app)
    {
        if (app.Id == Guid.Empty)
            app.Id = Guid.NewGuid();
        if (app.CreatedAt == default)
            app.CreatedAt = DateTime.UtcNow;
        app.UpdatedAt = DateTime.UtcNow;

        _db.CustomApps.Add(app);
        await _db.SaveChangesAsync();
        return app;
    }

    public async Task<CustomApp> CreateAppAsync(string name, string htmlContent, string icon, bool showInDashboard, bool isStandalone)
    {
        var app = new CustomApp
        {
            Id = Guid.NewGuid(),
            Name = name,
            HtmlContent = htmlContent,
            Icon = icon,
            ShowInDashboard = showInDashboard,
            IsStandalone = isStandalone,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.CustomApps.Add(app);
        await _db.SaveChangesAsync();
        return app;
    }

    public async Task<CustomApp?> UpdateAppAsync(Guid id, CustomApp updatedApp)
    {
        return await UpdateAppAsync(id, updatedApp.Name, updatedApp.HtmlContent, updatedApp.Icon, updatedApp.ShowInDashboard, updatedApp.IsStandalone);
    }

    public async Task<CustomApp?> UpdateAppAsync(Guid id, string name, string htmlContent, string icon, bool showInDashboard, bool isStandalone)
    {
        var app = await _db.CustomApps.FindAsync(id);
        if (app == null) return null;

        app.Name = name;
        app.HtmlContent = htmlContent;
        app.Icon = icon;
        app.ShowInDashboard = showInDashboard;
        app.IsStandalone = isStandalone;
        app.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return app;
    }

    public async Task<bool> DeleteAppAsync(Guid id)
    {
        var app = await _db.CustomApps.FindAsync(id);
        if (app == null) return false;

        _db.CustomApps.Remove(app);
        await _db.SaveChangesAsync();
        return true;
    }
}
