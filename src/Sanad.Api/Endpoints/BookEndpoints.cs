using System;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Sanad.Api.Models;
using Sanad.Api.Services;

namespace Sanad.Api.Endpoints;

public static class BookEndpoints
{
    public static void MapBookEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/books");

        group.MapGet("/search", async (string query, IBookSearchService searchService) =>
        {
            var results = await searchService.SearchBooksAsync(query);
            return Results.Ok(results);
        });

        group.MapGet("/cover", async (string url) =>
        {
            try 
            {
                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("User-Agent", "SanadApp/1.0");
                var response = await httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) return Results.NotFound();

                var contentType = response.Content.Headers.ContentType?.ToString() ?? "image/jpeg";
                var stream = await response.Content.ReadAsByteArrayAsync();
                
                return Results.File(stream, contentType);
            }
            catch (Exception)
            {
                return Results.NotFound();
            }
        });

        group.MapPost("/", async (IBookService svc, Book book) =>
        {
            var created = await svc.CreateBookAsync(book);
            return Results.Created($"/api/books/{created.Id}", created);
        });

        group.MapGet("/", async (IBookService svc) =>
        {
            var books = await svc.GetBooksAsync();
            return Results.Ok(books);
        });

        group.MapPut("/{id}", async (int id, IBookService svc, Book updatedBook) =>
        {
            var book = await svc.UpdateBookAsync(id, updatedBook);
            if (book == null) return Results.NotFound();
            return Results.Ok(book);
        });

        group.MapDelete("/{id}", async (int id, IBookService svc) =>
        {
            var success = await svc.DeleteBookAsync(id);
            if (!success) return Results.NotFound();
            return Results.NoContent();
        });
    }
}
