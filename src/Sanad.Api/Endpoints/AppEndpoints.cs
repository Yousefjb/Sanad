using System;
using System.Net.Http;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Sanad.Api.Models;
using Sanad.Api.Services;

namespace Sanad.Api.Endpoints;

public static class AppEndpoints
{
    public static void MapAppEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/apps")
            .RequireAuthorization();

        group.MapGet("/", async (IAppService svc) =>
        {
            var apps = await svc.GetAppsAsync();
            return Results.Ok(apps);
        });

        group.MapGet("/{id:guid}", async (Guid id, IAppService svc) =>
        {
            var app = await svc.GetAppByIdAsync(id);
            return app != null ? Results.Ok(app) : Results.NotFound();
        });

        group.MapPost("/", async (CustomApp app, IAppService svc) =>
        {
            var created = await svc.CreateAppAsync(app);
            return Results.Created($"/api/apps/{created.Id}", created);
        });

        group.MapPut("/{id:guid}", async (Guid id, CustomApp updatedApp, IAppService svc) =>
        {
            var app = await svc.UpdateAppAsync(id, updatedApp);
            if (app == null) return Results.NotFound();
            return Results.Ok(app);
        });

        group.MapDelete("/{id:guid}", async (Guid id, IAppService svc) =>
        {
            var success = await svc.DeleteAppAsync(id);
            if (!success) return Results.NotFound();
            return Results.NoContent();
        });

        group.MapPost("/proxy", async (ProxyRequest req, IHttpClientFactory httpClientFactory) =>
        {
            if (string.IsNullOrWhiteSpace(req.Url))
                return Results.BadRequest("URL is required");

            try
            {
                var client = httpClientFactory.CreateClient("AppProxy");
                var message = new HttpRequestMessage(new HttpMethod(req.Method ?? "GET"), req.Url);

                if (req.Headers != null)
                {
                    foreach (var (key, value) in req.Headers)
                    {
                        if (key.Equals("Host", StringComparison.OrdinalIgnoreCase) || 
                            key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                            continue;

                        message.Headers.TryAddWithoutValidation(key, value);
                    }
                }

                if (!string.IsNullOrEmpty(req.Body) && 
                    (message.Method == HttpMethod.Post || message.Method == HttpMethod.Put || message.Method == HttpMethod.Patch))
                {
                    message.Content = new StringContent(req.Body, Encoding.UTF8, "application/json");
                }

                var response = await client.SendAsync(message);
                var content = await response.Content.ReadAsStringAsync();
                var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/json";

                return Results.Content(content, contentType, statusCode: (int)response.StatusCode);
            }
            catch (Exception ex)
            {
                return Results.Problem($"Proxy error: {ex.Message}");
            }
        });
    }
}

public class ProxyRequest
{
    public string Url { get; set; } = string.Empty;
    public string? Method { get; set; } = "GET";
    public Dictionary<string, string>? Headers { get; set; }
    public string? Body { get; set; }
}
