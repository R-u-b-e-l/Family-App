using FamilyManagement.API.Authorization;
using FamilyManagement.Application.Common.Interfaces;
using FamilyManagement.Shared.Authorization;
using FamilyManagement.Shared.Common;
using FamilyManagement.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace FamilyManagement.API.Controllers;

public class UsersController(IUserService userService) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionCodes.MemberView)]
    public async Task<ActionResult<ApiResponse<PagedResult<UserDto>>>> GetMembers([FromQuery] PagedRequest request, CancellationToken ct) =>
        Ok(Success(await userService.GetMembersAsync(request, ct)));

    [HttpGet("{id:guid}")]
    [HasPermission(PermissionCodes.MemberView)]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetMember(Guid id, CancellationToken ct) =>
        Ok(Success(await userService.GetMemberAsync(id, ct)));

    [HttpPost]
    [HasPermission(PermissionCodes.MemberCreate)]
    public async Task<ActionResult<ApiResponse<UserDto>>> CreateMember(CreateMemberRequest request, CancellationToken ct) =>
        Ok(Success(await userService.CreateMemberAsync(request, ct), "Member created"));

    [HttpPut("{id:guid}")]
    [HasPermission(PermissionCodes.MemberEdit)]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateMember(Guid id, UpdateMemberRequest request, CancellationToken ct) =>
        Ok(Success(await userService.UpdateMemberAsync(id, request, ct), "Member updated"));

    [HttpPatch("{id:guid}/status")]
    [HasPermission(PermissionCodes.MemberDelete)]
    public async Task<ActionResult<ApiResponse>> SetStatus(Guid id, UpdateMemberStatusRequest request, CancellationToken ct)
    {
        await userService.SetMemberStatusAsync(id, request, ct);
        return Ok(Success("Member status updated"));
    }

    [HttpPut("{id:guid}/roles")]
    [HasPermission(PermissionCodes.RoleManage)]
    public async Task<ActionResult<ApiResponse>> AssignRoles(Guid id, AssignRolesRequest request, CancellationToken ct)
    {
        await userService.AssignRolesAsync(id, request, ct);
        return Ok(Success("Roles updated"));
    }

    [HttpPost("{id:guid}/reset-password")]
    [HasPermission(PermissionCodes.MemberEdit)]
    public async Task<ActionResult<ApiResponse>> ResetPassword(Guid id, AdminResetPasswordRequest request, CancellationToken ct)
    {
        await userService.AdminResetPasswordAsync(id, request, ct);
        return Ok(Success("Password reset"));
    }
}
