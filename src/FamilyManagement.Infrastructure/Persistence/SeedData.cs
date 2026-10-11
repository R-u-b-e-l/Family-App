using FamilyManagement.Domain.Entities;
using FamilyManagement.Shared.Authorization;
using Microsoft.EntityFrameworkCore;

namespace FamilyManagement.Infrastructure.Persistence;

/// <summary>
/// Static seed data: the permission catalog and the system role templates that are
/// copied into every new family at registration. All keys are deterministic so
/// migrations stay stable.
/// </summary>
public static class SeedData
{
    public static readonly Guid AdminTemplateRoleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid MemberTemplateRoleId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public const string FamilyAdminRoleName = "FamilyAdmin";
    public const string FamilyMemberRoleName = "FamilyMember";

    /// <summary>View-level permissions granted to the FamilyMember template.</summary>
    public static readonly string[] MemberTemplatePermissions =
    [
        PermissionCodes.DashboardView,
        PermissionCodes.FamilyView,
        PermissionCodes.MemberView,
        PermissionCodes.AccountView,
        PermissionCodes.VoucherView,
        PermissionCodes.LedgerView,
        PermissionCodes.CashBookView,
        PermissionCodes.BankBookView,
        PermissionCodes.ReportsView,
        PermissionCodes.SettingsView,
    ];

    /// <summary>Stable permission ids: position in the PermissionCodes catalog (1-based).</summary>
    public static IReadOnlyDictionary<string, int> PermissionIds { get; } =
        PermissionCodes.Groups
            .SelectMany(g => g.Value.Select(code => (g.Key, code)))
            .Select((x, i) => (x.code, Id: i + 1))
            .ToDictionary(x => x.code, x => x.Id);

    public static void Apply(ModelBuilder builder)
    {
        var permissions = PermissionCodes.Groups
            .SelectMany(g => g.Value.Select(code => (Group: g.Key, Code: code)))
            .Select((x, i) => new Permission { Id = i + 1, Code = x.Code, Group = x.Group })
            .ToList();
        builder.Entity<Permission>().HasData(permissions);

        builder.Entity<AppRole>().HasData(
            new AppRole
            {
                Id = AdminTemplateRoleId,
                FamilyId = null,
                Name = FamilyAdminRoleName,
                NormalizedName = FamilyAdminRoleName.ToUpperInvariant(),
                Description = "Full control over the family (template).",
                IsSystem = true,
                ConcurrencyStamp = "seed-admin-template"
            },
            new AppRole
            {
                Id = MemberTemplateRoleId,
                FamilyId = null,
                Name = FamilyMemberRoleName,
                NormalizedName = FamilyMemberRoleName.ToUpperInvariant(),
                Description = "Read-only family member (template).",
                IsSystem = true,
                ConcurrencyStamp = "seed-member-template"
            });

        var adminPermissions = permissions
            .Select(p => new RolePermission { RoleId = AdminTemplateRoleId, PermissionId = p.Id });
        var memberPermissions = MemberTemplatePermissions
            .Select(code => new RolePermission { RoleId = MemberTemplateRoleId, PermissionId = PermissionIds[code] });

        builder.Entity<RolePermission>().HasData(adminPermissions.Concat(memberPermissions));
    }
}
