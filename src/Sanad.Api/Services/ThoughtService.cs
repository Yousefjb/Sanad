using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sanad.Api.Data;
using Sanad.Api.Models;

namespace Sanad.Api.Services;

public class ThoughtService : IThoughtService
{
    private readonly SanadDbContext _db;

    public ThoughtService(SanadDbContext db)
    {
        _db = db;
    }

    public async Task<List<Thought>> GetThoughtsAsync(int page = 1, int pageSize = 20, string? search = null)
    {
        var p = page < 1 ? 1 : page;
        var size = pageSize < 1 ? 20 : pageSize;
        var query = _db.Thoughts.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var lowerSearch = search.ToLower();
            query = query.Where(t => t.Content != null && t.Content.ToLower().Contains(lowerSearch));
        }

        return await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((p - 1) * size)
            .Take(size)
            .ToListAsync();
    }

    public async Task<Thought> CreateThoughtAsync(string content)
    {
        var thought = new Thought { Content = content };
        _db.Thoughts.Add(thought);
        await _db.SaveChangesAsync();
        return thought;
    }

    public async Task<Thought?> UpdateThoughtAsync(string id, string content)
    {
        var thought = await _db.Thoughts.FindAsync(id);
        if (thought == null) return null;

        thought.Content = content;
        await _db.SaveChangesAsync();
        return thought;
    }

    public async Task<bool> DeleteThoughtAsync(string id)
    {
        var thought = await _db.Thoughts.FindAsync(id);
        if (thought == null) return false;

        _db.Thoughts.Remove(thought);
        await _db.SaveChangesAsync();
        return true;
    }
}
