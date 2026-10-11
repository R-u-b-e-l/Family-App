using FamilyManagement.Application.Common;
using FamilyManagement.Application.Common.Interfaces;
using FamilyManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace FamilyManagement.Infrastructure.Identity;

public class PermissionService(AppDbContext ctx, IMemoryCache cache) : IPermissionService
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(15);

    public async Task<IReadOnlySet<string>> GetUserPermissionsAsync(Guid familyId, Guid userId, CancellationToken ct = default)
    {
        var key = VersionedKey(familyId, userId);
        if (cache.TryGetValue(key, out IReadOnlySet<string>? cached) && cached is not null)
            return cached;

        var codes = await (from ur in ctx.UserRoles
                           join rp in ctx.RolePermissions on ur.RoleId equals rp.RoleId
                           join p in ctx.Permissions on rp.PermissionId equals p.Id
                           where ur.UserId == userId
                           select p.Code)
            .Distinct()
            .ToListAsync(ct);

        var set = (IReadOnlySet<string>)codes.ToHashSet(StringComparer.Ordinal);
        cache.Set(key, set, Ttl);
        return set;
    }

    public void InvalidateUser(Guid familyId, Guid userId) =>
        cache.Remove(VersionedKey(familyId, userId));

    /// <summary>Bumping the family permission version makes every member's cached set stale at once.</summary>
    public void InvalidateFamily(Guid familyId)
    {
        var version = cache.GetOrCreate($"permver:{familyId}", _ => 0);
        cache.Set($"permver:{familyId}", version + 1);
    }

    private string VersionedKey(Guid familyId, Guid userId)
    {
        var version = cache.GetOrCreate($"permver:{familyId}", _ => 0);
        return $"{CacheKeys.Permissions(familyId, userId)}:v{version}";
    }
}
