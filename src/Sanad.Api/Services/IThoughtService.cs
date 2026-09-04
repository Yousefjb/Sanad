using System.Collections.Generic;
using System.Threading.Tasks;
using Sanad.Api.Models;

namespace Sanad.Api.Services;

public interface IThoughtService
{
    Task<List<Thought>> GetThoughtsAsync(int page = 1, int pageSize = 20, string? search = null);
    Task<Thought> CreateThoughtAsync(string content);
    Task<Thought?> UpdateThoughtAsync(string id, string content);
    Task<bool> DeleteThoughtAsync(string id);
}
