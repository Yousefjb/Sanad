using System.Collections.Generic;
using System.Threading.Tasks;
using Sanad.Api.Models;

namespace Sanad.Api.Services;

public record StorageStatusDto(
    long DiskUsedBytes,
    long DiskLimitBytes,
    string TierName,
    bool IsAdmin
);

public interface IStorageService
{
    Task<List<StorageTier>> GetTiersAsync();
    Task<StorageStatusDto> GetStorageStatusAsync(string username);
}
