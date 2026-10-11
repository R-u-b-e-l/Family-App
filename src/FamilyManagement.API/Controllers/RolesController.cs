using FamilyManagement.API.Authorization;
using FamilyManagement.Application.Common.Interfaces;
using FamilyManagement.Shared.Authorization;
using FamilyManagement.Shared.Common;
using FamilyManagement.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace FamilyManagement.API.Controllers;

public class RolesController(IRoleService roleService) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionCodes.RoleView)]
    public async Task<ActionResult<ApiResponse<List<RoleDto>>>> GetRoles(CancellationToken ct) =>
        Ok(Success(await roleService.GetRolesAsync(ct)));

    [HttpGet("{id:guid}")]
    [HasPermission(PermissionCodes.RoleView)]
    public async Task<ActionResult<ApiResponse<RoleDto>>> GetRole(Guid id, CancellationToken ct) =>
        Ok(Success(await roleService.GetRoleAsync(id, ct)));

    [HttpPost]
    [HasPermission(PermissionCodes.RoleManage)]
    public async Task<ActionResult<ApiResponse<RoleDto>>> CreateRole(CreateRoleRequest request, CancellationToken ct) =>
        Ok(Success(await roleService.CreateRoleAsync(request, ct), "Role created"));

    [HttpPut("{id:guid}")]
    [HasPermission(PermissionCodes.RoleManage)]
    public async Task<ActionResult<ApiResponse<RoleDto>>> UpdateRole(Guid id, UpdateRoleRequest request, CancellationToken ct) =>
        Ok(Success(await roleService.UpdateRoleAsync(id, request, ct), "Role updated"));

    [HttpDelete("{id:guid}")]
    [HasPermission(PermissionCodes.RoleManage)]
    public async Task<ActionResult<ApiResponse>> DeleteRole(Guid id, CancellationToken ct)
    {
        await roleService.DeleteRoleAsync(id, ct);
        return Ok(Success("Role deleted"));
    }

    [HttpPut("{id:guid}/permissions")]
    [HasPermission(PermissionCodes.RoleManage)]
    public async Task<ActionResult<ApiResponse>> UpdatePermissions(Guid id, UpdateRolePermissionsRequest request, CancellationToken ct)
    {
        await roleService.UpdateRolePermissionsAsync(id, request, ct);
        return Ok(Success("Permissions updated"));
    }
}

public class PermissionsController(IRoleService roleService, IPermissionService permissionService, ICurrentUser currentUser) : ApiControllerBase
{
    /// <summary>The full permission catalog (for the role editor UI).</summary>
    [HttpGet]
    [HasPermission(PermissionCodes.RoleView)]
    public async Task<ActionResult<ApiResponse<List<PermissionDto>>>> GetCatalog(CancellationToken ct) =>
        Ok(Success(await roleService.GetPermissionCatalogAsync(ct)));

    /// <summary>The signed-in user's effective permissions (for showing/hiding UI).</summary>
    [HttpGet("mine")]
    public async Task<ActionResult<ApiResponse<List<string>>>> GetMine(CancellationToken ct)
    {
        var permissions = await permissionService.GetUserPermissionsAsync(currentUser.FamilyId, currentUser.UserId, ct);
        return Ok(Success(permissions.ToList()));
    }
}
