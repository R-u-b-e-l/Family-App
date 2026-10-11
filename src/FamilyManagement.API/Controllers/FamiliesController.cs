using FamilyManagement.API.Authorization;
using FamilyManagement.Application.Services;
using FamilyManagement.Shared.Authorization;
using FamilyManagement.Shared.Common;
using FamilyManagement.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace FamilyManagement.API.Controllers;

public class FamiliesController(IFamilyService familyService) : ApiControllerBase
{
    [HttpGet("current")]
    [HasPermission(PermissionCodes.FamilyView)]
    public async Task<ActionResult<ApiResponse<FamilyDto>>> GetCurrent(CancellationToken ct) =>
        Ok(Success(await familyService.GetCurrentFamilyAsync(ct)));

    [HttpPut("current")]
    [HasPermission(PermissionCodes.FamilyEdit)]
    public async Task<ActionResult<ApiResponse<FamilyDto>>> Update(UpdateFamilyRequest request, CancellationToken ct) =>
        Ok(Success(await familyService.UpdateFamilyAsync(request, ct), "Family updated"));

    [HttpGet("settings")]
    [HasPermission(PermissionCodes.SettingsView)]
    public async Task<ActionResult<ApiResponse<FamilySettingsDto>>> GetSettings(CancellationToken ct) =>
        Ok(Success(await familyService.GetSettingsAsync(ct)));

    [HttpPut("settings")]
    [HasPermission(PermissionCodes.SettingsEdit)]
    public async Task<ActionResult<ApiResponse<FamilySettingsDto>>> UpdateSettings(UpdateFamilySettingsRequest request, CancellationToken ct) =>
        Ok(Success(await familyService.UpdateSettingsAsync(request, ct), "Settings updated"));
}
