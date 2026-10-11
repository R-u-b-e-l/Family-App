using FamilyManagement.API.Authorization;
using FamilyManagement.Application.Services;
using FamilyManagement.Shared.Authorization;
using FamilyManagement.Shared.Common;
using FamilyManagement.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace FamilyManagement.API.Controllers;

public class AccountsController(IAccountService accountService) : ApiControllerBase
{
    [HttpGet("tree")]
    [HasPermission(PermissionCodes.AccountView)]
    public async Task<ActionResult<ApiResponse<List<AccountTreeNodeDto>>>> GetTree(CancellationToken ct) =>
        Ok(Success(await accountService.GetTreeAsync(ct)));

    [HttpGet]
    [HasPermission(PermissionCodes.AccountView)]
    public async Task<ActionResult<ApiResponse<PagedResult<AccountDto>>>> GetList([FromQuery] PagedRequest request, CancellationToken ct) =>
        Ok(Success(await accountService.GetListAsync(request, ct)));

    [HttpGet("{id:guid}")]
    [HasPermission(PermissionCodes.AccountView)]
    public async Task<ActionResult<ApiResponse<AccountDto>>> Get(Guid id, CancellationToken ct) =>
        Ok(Success(await accountService.GetAsync(id, ct)));

    [HttpGet("{id:guid}/balance")]
    [HasPermission(PermissionCodes.AccountView)]
    public async Task<ActionResult<ApiResponse<AccountBalanceDto>>> GetBalance(Guid id, [FromQuery] DateOnly? asOf, CancellationToken ct) =>
        Ok(Success(await accountService.GetBalanceAsync(id, asOf, ct)));

    [HttpPost]
    [HasPermission(PermissionCodes.AccountCreate)]
    public async Task<ActionResult<ApiResponse<AccountDto>>> Create(CreateAccountRequest request, CancellationToken ct) =>
        Ok(Success(await accountService.CreateAsync(request, ct), "Account created"));

    [HttpPut("{id:guid}")]
    [HasPermission(PermissionCodes.AccountEdit)]
    public async Task<ActionResult<ApiResponse<AccountDto>>> Update(Guid id, UpdateAccountRequest request, CancellationToken ct) =>
        Ok(Success(await accountService.UpdateAsync(id, request, ct), "Account updated"));

    [HttpPatch("{id:guid}/status")]
    [HasPermission(PermissionCodes.AccountEdit)]
    public async Task<ActionResult<ApiResponse>> SetActive(Guid id, [FromQuery] bool isActive, CancellationToken ct)
    {
        await accountService.SetActiveAsync(id, isActive, ct);
        return Ok(Success(isActive ? "Account activated" : "Account deactivated"));
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(PermissionCodes.AccountDelete)]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken ct)
    {
        await accountService.DeleteAsync(id, ct);
        return Ok(Success("Account deleted"));
    }
}

public class PeriodsController(IPeriodService periodService) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionCodes.PeriodView)]
    public async Task<ActionResult<ApiResponse<List<PeriodDto>>>> GetPeriods(CancellationToken ct) =>
        Ok(Success(await periodService.GetPeriodsAsync(ct)));

    [HttpPost]
    [HasPermission(PermissionCodes.PeriodManage)]
    public async Task<ActionResult<ApiResponse<PeriodDto>>> Create(CreatePeriodRequest request, CancellationToken ct) =>
        Ok(Success(await periodService.CreatePeriodAsync(request, ct), "Period created"));

    [HttpPost("{id:guid}/close")]
    [HasPermission(PermissionCodes.PeriodManage)]
    public async Task<ActionResult<ApiResponse<PeriodDto>>> Close(Guid id, CancellationToken ct) =>
        Ok(Success(await periodService.ClosePeriodAsync(id, ct), "Period closed"));

    [HttpPost("{id:guid}/reopen")]
    [HasPermission(PermissionCodes.PeriodManage)]
    public async Task<ActionResult<ApiResponse<PeriodDto>>> Reopen(Guid id, CancellationToken ct) =>
        Ok(Success(await periodService.ReopenPeriodAsync(id, ct), "Period reopened"));
}
