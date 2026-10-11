using FamilyManagement.API.Authorization;
using FamilyManagement.Application.Services;
using FamilyManagement.Shared.Authorization;
using FamilyManagement.Shared.Common;
using FamilyManagement.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace FamilyManagement.API.Controllers;

public class VouchersController(IVoucherService voucherService) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(PermissionCodes.VoucherView)]
    public async Task<ActionResult<ApiResponse<PagedResult<VoucherListItemDto>>>> GetPaged([FromQuery] VoucherFilterRequest request, CancellationToken ct) =>
        Ok(Success(await voucherService.GetPagedAsync(request, ct)));

    [HttpGet("{id:guid}")]
    [HasPermission(PermissionCodes.VoucherView)]
    public async Task<ActionResult<ApiResponse<VoucherDto>>> Get(Guid id, CancellationToken ct) =>
        Ok(Success(await voucherService.GetAsync(id, ct)));

    /// <summary>Creates a draft voucher. Post it to assign the final number and hit the books.</summary>
    [HttpPost]
    [HasPermission(PermissionCodes.VoucherCreate)]
    public async Task<ActionResult<ApiResponse<VoucherDto>>> CreateDraft(SaveVoucherRequest request, CancellationToken ct) =>
        Ok(Success(await voucherService.CreateDraftAsync(request, ct), "Draft saved"));

    [HttpPut("{id:guid}")]
    [HasPermission(PermissionCodes.VoucherEdit)]
    public async Task<ActionResult<ApiResponse<VoucherDto>>> UpdateDraft(Guid id, SaveVoucherRequest request, CancellationToken ct) =>
        Ok(Success(await voucherService.UpdateDraftAsync(id, request, ct), "Draft updated"));

    [HttpDelete("{id:guid}")]
    [HasPermission(PermissionCodes.VoucherDelete)]
    public async Task<ActionResult<ApiResponse>> DeleteDraft(Guid id, CancellationToken ct)
    {
        await voucherService.DeleteDraftAsync(id, ct);
        return Ok(Success("Draft deleted"));
    }

    [HttpPost("{id:guid}/post")]
    [HasPermission(PermissionCodes.VoucherPost)]
    public async Task<ActionResult<ApiResponse<VoucherDto>>> Post(Guid id, CancellationToken ct) =>
        Ok(Success(await voucherService.PostAsync(id, ct), "Voucher posted"));

    [HttpPost("{id:guid}/cancel")]
    [HasPermission(PermissionCodes.VoucherApprove)]
    public async Task<ActionResult<ApiResponse<VoucherDto>>> Cancel(Guid id, CancellationToken ct) =>
        Ok(Success(await voucherService.CancelAsync(id, ct), "Voucher cancelled"));

    [HttpPost("{id:guid}/reverse")]
    [HasPermission(PermissionCodes.VoucherApprove)]
    public async Task<ActionResult<ApiResponse<VoucherDto>>> Reverse(Guid id, CancellationToken ct) =>
        Ok(Success(await voucherService.ReverseAsync(id, ct), "Voucher reversed"));
}
