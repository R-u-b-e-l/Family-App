using FamilyManagement.Domain.Entities;
using FamilyManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FamilyManagement.Infrastructure.Identity;

/// <summary>
/// Startup sync: when new system template roles or permissions ship in an update,
/// existing families get them too (additive only — custom roles are never touched).
/// </summary>
public class RoleSyncService(AppDbContext ctx, ILogger<RoleSyncService> logger)
{
    public async Task SyncAsync(CancellationToken ct = default)
    {
        var templates = await ctx.Roles.AsNoTracking()
            .Where(r => r.FamilyId == null && r.IsSystem)
            .Include(r => r.RolePermissions)
            .ToListAsync(ct);
        if (templates.Count == 0)
            return;

        var familyIds = await ctx.Families.Select(f => f.Id).ToListAsync(ct);
        var familyRoles = await ctx.Roles
            .Where(r => r.FamilyId != null)
            .Include(r => r.RolePermissions)
            .ToListAsync(ct);
        var allPermissionIds = await ctx.Permissions.Select(p => p.Id).ToListAsync(ct);

        var addedRoles = 0;
        var addedPermissions = 0;

        foreach (var familyId in familyIds)
        {
            foreach (var template in templates)
            {
                var existing = familyRoles.FirstOrDefault(r =>
                    r.FamilyId == familyId && r.NormalizedName == template.NormalizedName);

                if (existing is null)
                {
                    var role = new AppRole
                    {
                        Id = Guid.NewGuid(),
                        FamilyId = familyId,
                        Name = template.Name,
                        NormalizedName = template.NormalizedName,
                        Description = template.Description?.Replace(" (template)", ""),
                        IsSystem = true
                    };
                    ctx.Roles.Add(role);
                    foreach (var rp in template.RolePermissions)
                        ctx.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = rp.PermissionId });
                    addedRoles++;
                    continue;
                }

                if (!existing.IsSystem)
                    continue;

                // FamilyAdmin always holds every permission; other system roles follow their template (additive).
                var target = template.Name == SeedData.FamilyAdminRoleName
                    ? allPermissionIds
                    : template.RolePermissions.Select(rp => rp.PermissionId).ToList();

                var have = existing.RolePermissions.Select(rp => rp.PermissionId).ToHashSet();
                foreach (var permissionId in target.Where(p => !have.Contains(p)))
                {
                    ctx.RolePermissions.Add(new RolePermission { RoleId = existing.Id, PermissionId = permissionId });
                    addedPermissions++;
                }
            }
        }

        if (addedRoles > 0 || addedPermissions > 0)
        {
            await ctx.SaveChangesAsync(ct);
            logger.LogInformation("Role sync: added {Roles} roles and {Permissions} permissions to existing families",
                addedRoles, addedPermissions);
        }
    }
}
