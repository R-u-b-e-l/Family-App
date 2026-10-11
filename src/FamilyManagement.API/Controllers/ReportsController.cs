using FamilyManagement.API.Authorization;
using FamilyManagement.Application.Services;
using FamilyManagement.Shared.Authorization;
using FamilyManagement.Shared.Common;
using FamilyManagement.Shared.Dtos;
using FamilyManagement.Shared.Enums;
using Microsoft.AspNetCore.Mvc;

namespace FamilyManagement.API.Controllers;

public class ReportsController(IReportService reportService) : ApiControllerBase
{
    [HttpGet("ledger")]
    [HasPermission(PermissionCodes.LedgerView)]
    public async Task<ActionResult<ApiResponse<LedgerReportDto>>> GetLedger([FromQuery] LedgerRequest request, CancellationToken ct) =>
        Ok(Success(await reportService.GetLedgerAsync(request, ct)));

    [HttpGet("cashbook")]
    [HasPermission(PermissionCodes.CashBookView)]
    public async Task<ActionResult<ApiResponse<BookReportDto>>> GetCashBook([FromQuery] BookRequest request, CancellationToken ct) =>
        Ok(Success(await reportService.GetBookAsync(CashBankKind.Cash, request, ct)));

    [HttpGet("bankbook")]
    [HasPermission(PermissionCodes.BankBookView)]
    public async Task<ActionResult<ApiResponse<BookReportDto>>> GetBankBook([FromQuery] BookRequest request, CancellationToken ct) =>
        Ok(Success(await reportService.GetBookAsync(CashBankKind.Bank, request, ct)));

    [HttpGet("trial-balance")]
    [HasPermission(PermissionCodes.TrialBalanceView)]
    public async Task<ActionResult<ApiResponse<TrialBalanceDto>>> GetTrialBalance([FromQuery] DateOnly asOf, CancellationToken ct) =>
        Ok(Success(await reportService.GetTrialBalanceAsync(asOf, ct)));

    [HttpGet("profit-loss")]
    [HasPermission(PermissionCodes.ProfitLossView)]
    public async Task<ActionResult<ApiResponse<ProfitLossDto>>> GetProfitLoss(
        [FromQuery] DateOnly dateFrom, [FromQuery] DateOnly dateTo, CancellationToken ct) =>
        Ok(Success(await reportService.GetProfitLossAsync(dateFrom, dateTo, ct)));

    [HttpGet("balance-sheet")]
    [HasPermission(PermissionCodes.BalanceSheetView)]
    public async Task<ActionResult<ApiResponse<BalanceSheetDto>>> GetBalanceSheet([FromQuery] DateOnly asOf, CancellationToken ct) =>
        Ok(Success(await reportService.GetBalanceSheetAsync(asOf, ct)));
}
