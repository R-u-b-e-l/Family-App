using FamilyManagement.Application.Common.Interfaces;
using FamilyManagement.Shared.Common;
using FamilyManagement.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace FamilyManagement.API.Controllers;

public class ProfileController(IUserService userService) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<UserDto>>> Get(CancellationToken ct) =>
        Ok(Success(await userService.GetProfileAsync(ct)));

    [HttpPut]
    public async Task<ActionResult<ApiResponse<UserDto>>> Update(UpdateProfileRequest request, CancellationToken ct) =>
        Ok(Success(await userService.UpdateProfileAsync(request, ct), "Profile updated"));
}
