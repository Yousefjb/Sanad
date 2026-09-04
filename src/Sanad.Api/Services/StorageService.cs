using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sanad.Api.Data;
using Sanad.Api.Models;

namespace Sanad.Api.Services;

public class StorageService : IStorageService
{
    private readonly AdminDbContext _adminDb;
    private readonly DiskQuotaService _quotaService;

    public StorageService(AdminDbContext adminDb, DiskQuotaService quotaService)
    {
        _adminDb = adminDb;
        _quotaService = quotaService;
    }

    public async Task<List<StorageTier>> GetTiersAsync()
    {
        return await _adminDb.Tiers.ToListAsync();
    }

    public async Task<StorageStatusDto> GetStorageStatusAsync(string username)
    {
        var user = await _adminDb.Users.Include(u => u.Tier).FirstOrDefaultAsync(u => u.Username == username);
        if (user == null) throw new InvalidOperationException("User not found");

        var userPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", username);
        var diskUsed = _quotaService.GetDirectorySize(userPath);
        var diskLimitBytes = user.Tier?.DiskLimitBytes ?? (1L * Constants.GigaByte);

        return new StorageStatusDto(
            DiskUsedBytes: diskUsed,
            DiskLimitBytes: diskLimitBytes,
            TierName: user.Tier?.Name ?? "Unknown",
            IsAdmin: user.IsAdmin
        );
    }
}
