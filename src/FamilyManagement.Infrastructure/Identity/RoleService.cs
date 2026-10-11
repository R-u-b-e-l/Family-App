using FamilyManagement.Application.Common.Exceptions;
using FamilyManagement.Application.Common.Interfaces;
using FamilyManagement.Application.Services;
using FamilyManagement.Domain.Entities;
using FamilyManagement.Infrastructure.Persistence;
using FamilyManagement.Shared.Dtos;
using Microsoft.EntityFrameworkCore;

namespace FamilyManagement.Infrastructure.Identity;

public class RoleService(
    AppDbContext ctx,
    ICurrentUser currentUser,
    IAuditService audit,
    IPermissionService permissions) : IRoleService
{
    private IQueryable<AppRole> FamilyRoles => ctx.Roles.Where(r => r.FamilyId == currentUser.FamilyId);

    public async Task<List<RoleDto>> GetRolesAsync(CancellationToken ct = default) =>
        await FamilyRoles.AsNoTracking()
            .OrderBy(r => r.Name)
            .Select(r => new RoleDto
            {
                Id = r.Id,
                Name = r.Name!,
                Description = r.Description,
                IsSystem = r.IsSystem,
                MemberCount = ctx.UserRoles.Count(ur => ur.RoleId == r.Id),
                Permissions = r.RolePermissions.Select(rp => rp.Permission!.Code).ToList()
            })
            .ToListAsync(ct);

    public async Task<RoleDto> GetRoleAsync(Guid roleId, CancellationToken ct = default) =>
        await FamilyRoles.AsNoTracking()
            .Where(r => r.Id == roleId)
            .Select(r => new RoleDto
            {
                Id = r.Id,
                Name = r.Name!,
                Description = r.Description,
                IsSystem = r.IsSystem,
                MemberCount = ctx.UserRoles.Count(ur => ur.RoleId == r.Id),
                Permissions = r.RolePermissions.Select(rp => rp.Permission!.Code).ToList()
            })
            .FirstOrDefaultAsync(ct)
        ?? throw new NotFoundException("Role not found.");

    public async Task<RoleDto> CreateRoleAsync(CreateRoleRequest request, CancellationToken ct = default)
    {
        var name = request.Name.Trim();
        var normalized = name.ToUpperInvariant();
        if (await FamilyRoles.AnyAsync(r => r.NormalizedName == normalized, ct))
            throw new ConflictException($"A role named '{name}' already exists.");

        var role = new AppRole
        {
            Id = Guid.NewGuid(),
            FamilyId = currentUser.FamilyId,
            Name = name,
            NormalizedName = normalized,
            Description = request.Description,
            IsSystem = false
        };
        ctx.Roles.Add(role);
        await AddPermissionsAsync(role.Id, request.PermissionCodes, ct);

        audit.Log("Role.Create", nameof(AppRole), role.Id.ToString(), null,
            new { role.Name, request.PermissionCodes });
        await ctx.SaveChangesAsync(ct);

        return await GetRoleAsync(role.Id, ct);
    }

    public async Task<RoleDto> UpdateRoleAsync(Guid roleId, UpdateRoleRequest request, CancellationToken ct = default)
    {
        var role = await FamilyRoles.FirstOrDefaultAsync(r => r.Id == roleId, ct)
            ?? throw new NotFoundException("Role not found.");
        if (role.IsSystem)
            throw new BadRequestException("System roles cannot be renamed.");

        var name = request.Name.Trim();
        var normalized = name.ToUpperInvariant();
        if (await FamilyRoles.AnyAsync(r => r.Id != roleId && r.NormalizedName == normalized, ct))
            throw new ConflictException($"A role named '{name}' already exists.");

        role.Name = name;
        role.NormalizedName = normalized;
        role.Description = request.Description;

        audit.Log("Role.Update", nameof(AppRole), role.Id.ToString());
        await ctx.SaveChangesAsync(ct);
        return await GetRoleAsync(role.Id, ct);
    }

    public async Task DeleteRoleAsync(Guid roleId, CancellationToken ct = default)
    {
        var role = await FamilyRoles.FirstOrDefaultAsync(r => r.Id == roleId, ct)
            ?? throw new NotFoundException("Role not found.");
        if (role.IsSystem)
            throw new BadRequestException("System roles cannot be deleted.");
        if (await ctx.UserRoles.AnyAsync(ur => ur.RoleId == roleId, ct))
            throw new ConflictException("The role is assigned to members and cannot be deleted.");

        ctx.Roles.Remove(role);
        audit.Log("Role.Delete", nameof(AppRole), role.Id.ToString(), new { role.Name }, null);
        await ctx.SaveChangesAsync(ct);
    }

    public async Task UpdateRolePermissionsAsync(Guid roleId, UpdateRolePermissionsRequest request, CancellationToken ct = default)
    {
        var role = await FamilyRoles.FirstOrDefaultAsync(r => r.Id == roleId, ct)
            ?? throw new NotFoundException("Role not found.");
        if (role.IsSystem && role.Name == SeedData.FamilyAdminRoleName)
            throw new BadRequestException("The FamilyAdmin role always keeps full permissions.");

        var existing = await ctx.RolePermissions.Where(rp => rp.RoleId == roleId).ToListAsync(ct);
        ctx.RolePermissions.RemoveRange(existing);
        await AddPermissionsAsync(roleId, request.PermissionCodes, ct);

        audit.Log("Role.PermissionChange", nameof(AppRole), role.Id.ToString(),
            null, new { request.PermissionCodes });
        await ctx.SaveChangesAsync(ct);

        permissions.InvalidateFamily(currentUser.FamilyId);
    }

    public async Task<List<PermissionDto>> GetPermissionCatalogAsync(CancellationToken ct = default) =>
        await ctx.Permissions.AsNoTracking()
            .OrderBy(p => p.Id)
            .Select(p => new PermissionDto { Id = p.Id, Code = p.Code, Group = p.Group, Description = p.Description })
            .ToListAsync(ct);

    private async Task AddPermissionsAsync(Guid roleId, List<string> codes, CancellationToken ct)
    {
        if (codes.Count == 0)
            return;
        var ids = await ctx.Permissions
            .Where(p => codes.Contains(p.Code))
            .Select(p => p.Id)
            .ToListAsync(ct);
        foreach (var id in ids)
            ctx.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = id });
    }
}
