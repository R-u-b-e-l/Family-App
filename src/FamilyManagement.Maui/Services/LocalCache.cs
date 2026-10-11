using System.Text.Json;
using SQLite;

namespace FamilyManagement.Maui.Services;

public class CacheEntry
{
    [PrimaryKey]
    public string Key { get; set; } = string.Empty;
    public string Json { get; set; } = string.Empty;
    public DateTime UpdatedAtUtc { get; set; }
}

/// <summary>
/// SQLite-backed read cache for non-sensitive display data (dashboard, accounts,
/// recent vouchers). Never used as a source for posting or calculations.
/// </summary>
public class LocalCache
{
    private SQLiteAsyncConnection? _db;

    private async Task<SQLiteAsyncConnection> GetDbAsync()
    {
        if (_db is null)
        {
            var path = Path.Combine(FileSystem.AppDataDirectory, "fm_cache.db3");
            _db = new SQLiteAsyncConnection(path);
            await _db.CreateTableAsync<CacheEntry>();
        }
        return _db;
    }

    public async Task SetAsync<T>(string key, T value)
    {
        try
        {
            var db = await GetDbAsync();
            await db.InsertOrReplaceAsync(new CacheEntry
            {
                Key = key,
                Json = JsonSerializer.Serialize(value),
                UpdatedAtUtc = DateTime.UtcNow
            });
        }
        catch
        {
            // Cache writes must never break the app.
        }
    }

    public async Task<(T? Value, DateTime? CachedAt)> GetAsync<T>(string key)
    {
        try
        {
            var db = await GetDbAsync();
            var entry = await db.FindAsync<CacheEntry>(key);
            if (entry is null)
                return (default, null);
            return (JsonSerializer.Deserialize<T>(entry.Json), entry.UpdatedAtUtc);
        }
        catch
        {
            return (default, null);
        }
    }

    public async Task ClearAsync()
    {
        try
        {
            var db = await GetDbAsync();
            await db.DeleteAllAsync<CacheEntry>();
        }
        catch
        {
            // ignore
        }
    }
}
