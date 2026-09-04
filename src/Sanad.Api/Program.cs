using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Sanad.Api.Data;
using Sanad.Api.Endpoints;
using Sanad.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials());
});

// Configure Data Protection to persist keys across restarts
builder.Services.AddDataProtection()
    .PersistKeysToDbContext<AdminDbContext>()
    .SetApplicationName("Sanad");

// Add Authentication and Authorization
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "Sanad.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    })
    .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
        ApiKeyAuthenticationOptions.DefaultScheme, null);

builder.Services.AddAuthorization(options =>
{
    var defaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder(
        CookieAuthenticationDefaults.AuthenticationScheme,
        ApiKeyAuthenticationOptions.DefaultScheme)
        .RequireAuthenticatedUser()
        .Build();
    options.DefaultPolicy = defaultPolicy;
});

// Add HttpContextAccessor and TenantProvider
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantProvider, TenantProvider>();
builder.Services.AddScoped<DiskQuotaService>();

// Configure Admin DB
var adminDbPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "admin.db");
Directory.CreateDirectory(Path.GetDirectoryName(adminDbPath)!);
builder.Services.AddDbContext<AdminDbContext>(options =>
    options.UseSqlite($"Data Source={adminDbPath}"));

// Configure Tenant DB
builder.Services.AddDbContext<SanadDbContext>((sp, options) => 
{
    var tenantProvider = sp.GetRequiredService<ITenantProvider>();
    try 
    {
        options.UseSqlite(tenantProvider.GetConnectionString());
    }
    catch (UnauthorizedAccessException)
    {
        // Fallback for design-time tools (e.g. EF migrations)
        options.UseSqlite("Data Source=Data/migrations.db");
    }
});

// Ignore JSON reference cycles
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);

builder.Services.AddHttpClient<IBookSearchService, BookSearchService>();

builder.Services.AddMcpServer()
    .WithHttpTransport(options => options.Stateless = true)
    .WithTools<McpEndpoints>();

// Domain Services
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IThoughtService, ThoughtService>();
builder.Services.AddScoped<IHabitService, HabitService>();
builder.Services.AddScoped<IGoalService, GoalService>();
builder.Services.AddScoped<IFinanceService, FinanceService>();
builder.Services.AddScoped<IDebtService, DebtService>();
builder.Services.AddScoped<IReadingService, ReadingService>();
builder.Services.AddScoped<IBookService, BookService>();
builder.Services.AddScoped<INoteService, NoteService>();
builder.Services.AddScoped<ICalendarService, CalendarService>();
builder.Services.AddScoped<IStorageService, StorageService>();
builder.Services.AddScoped<IAppService, AppService>();

// Change to Scoped since it needs ITenantProvider
builder.Services.AddScoped<FileStorageService>();
builder.Services.AddScoped<FileManagerService>();
builder.Services.AddScoped<PaddleService>();
builder.Services.AddSingleton<MigrationService>();
builder.Services.AddHostedService<DiskUsageSyncService>();
builder.Services.AddHostedService<SubscriptionCleanupService>();
builder.Services.AddMemoryCache();
var app = builder.Build();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<Sanad.Api.Middleware.MigrationCheckMiddleware>();
app.UseMiddleware<Sanad.Api.Middleware.TenantDbMigrationMiddleware>();

// Create Admin DB if not exists
using (var scope = app.Services.CreateScope())
{
    var adminDb = scope.ServiceProvider.GetRequiredService<AdminDbContext>();
    adminDb.Database.Migrate();
    adminDb.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");

    // Resume any interrupted background migrations
    var migrationService = scope.ServiceProvider.GetRequiredService<MigrationService>();
    var stuckUsers = adminDb.Users.Where(u => u.IsMigrating && u.TargetDatastoreId != null).ToList();
    foreach (var user in stuckUsers)
    {
        Console.WriteLine($"Resuming background migration for user {user.Username} to datastore {user.TargetDatastoreId}");
        migrationService.StartMigration(user.Id, user.TargetDatastoreId.Value);
    }
}

app.MapGet("/", () => "Sanad API Running");

app.MapAuthEndpoints();

var api = app.MapGroup("").RequireAuthorization();

api.MapUploadEndpoints();
api.MapThoughtEndpoints();
api.MapGoalEndpoints();
api.MapTaskEndpoints();
api.MapCalendarEndpoints();

api.MapFinanceEndpoints();
api.MapNotebookEndpoints();
api.MapAssetEndpoints();
api.MapDebtEndpoints();
api.MapBookEndpoints();
api.MapReadingEndpoints();
api.MapHabitEndpoints();
api.MapFolderEndpoints();
api.MapFileEndpoints();
api.MapStorageEndpoints();
api.MapSubscriptionEndpoints();
api.MapSettingsEndpoints();
api.MapAppEndpoints();
api.MapWhiteboardEndpoints();
api.MapSearchEndpoints();

app.MapShareEndpoints();

// Admin Endpoints
api.MapAdminEndpoints();
api.MapAdminSettingsEndpoints();

app.MapPaddleWebhookEndpoints();
app.MapPublicSettingsEndpoints();

app.MapMcp("/mcp").RequireAuthorization();

app.Run();
