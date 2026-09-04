using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sanad.Api.Models;

namespace Sanad.Api.Services;

public interface IAppService
{
    Task<List<CustomApp>> GetAppsAsync();
    Task<CustomApp?> GetAppByIdAsync(Guid id);
    Task<CustomApp> CreateAppAsync(CustomApp app);
    Task<CustomApp> CreateAppAsync(string name, string htmlContent, string icon, bool showInDashboard, bool isStandalone);
    Task<CustomApp?> UpdateAppAsync(Guid id, CustomApp updatedApp);
    Task<CustomApp?> UpdateAppAsync(Guid id, string name, string htmlContent, string icon, bool showInDashboard, bool isStandalone);
    Task<bool> DeleteAppAsync(Guid id);
}
