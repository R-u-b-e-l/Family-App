using FamilyManagement.API.Authorization;
using FamilyManagement.API.Services;
using FamilyManagement.Application.Services;
using FamilyManagement.Shared.Authorization;
using FamilyManagement.Shared.Common;
using FamilyManagement.Shared.Dtos;
using FamilyManagement.Shared.Enums;
using Microsoft.AspNetCore.Mvc;

namespace FamilyManagement.API.Controllers;

public class ReportsController(
    IReportService reportService,
    IFamilyService familyService,
    PdfReportService pdfService) : ApiControllerBase
{
    private const int PdfMaxRows = 5000;
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

    // ---- PDF statements ---------------------------------------------------

    [HttpGet("ledger/pdf")]
    [HasPermission(PermissionCodes.LedgerView)]
    public async Task<IActionResult> GetLedgerPdf([FromQuery] Guid accountId,
        [FromQuery] DateOnly dateFrom, [FromQuery] DateOnly dateTo, CancellationToken ct)
    {
        var (familyName, currency) = await GetHeaderInfoAsync(ct);
        LedgerReportDto report = null!;
        var rows = new List<LedgerRowDto>();
        for (var page = 1; rows.Count < PdfMaxRows; page++)
        {
            report = await reportService.GetLedgerAsync(new LedgerRequest
            {
                AccountId = accountId, DateFrom = dateFrom, DateTo = dateTo, Page = page, PageSize = 100
            }, ct);
            rows.AddRange(report.Rows.Items);
            if (page >= report.Rows.TotalPages)
                break;
        }
        var pdf = pdfService.BuildLedger(report, rows, familyName, currency);
        return File(pdf, "application/pdf", $"Ledger_{report.AccountCode}_{dateFrom:yyyyMMdd}-{dateTo:yyyyMMdd}.pdf");
    }

    [HttpGet("cashbook/pdf")]
    [HasPermission(PermissionCodes.CashBookView)]
    public Task<IActionResult> GetCashBookPdf([FromQuery] DateOnly dateFrom, [FromQuery] DateOnly dateTo, CancellationToken ct) =>
        BuildBookPdfAsync(CashBankKind.Cash, dateFrom, dateTo, ct);

    [HttpGet("bankbook/pdf")]
    [HasPermission(PermissionCodes.BankBookView)]
    public Task<IActionResult> GetBankBookPdf([FromQuery] DateOnly dateFrom, [FromQuery] DateOnly dateTo, CancellationToken ct) =>
        BuildBookPdfAsync(CashBankKind.Bank, dateFrom, dateTo, ct);

    [HttpGet("trial-balance/pdf")]
    [HasPermission(PermissionCodes.TrialBalanceView)]
    public async Task<IActionResult> GetTrialBalancePdf([FromQuery] DateOnly asOf, CancellationToken ct)
    {
        var (familyName, currency) = await GetHeaderInfoAsync(ct);
        var report = await reportService.GetTrialBalanceAsync(asOf, ct);
        var pdf = pdfService.BuildTrialBalance(report, familyName, currency);
        return File(pdf, "application/pdf", $"TrialBalance_{asOf:yyyyMMdd}.pdf");
    }

    [HttpGet("profit-loss/pdf")]
    [HasPermission(PermissionCodes.ProfitLossView)]
    public async Task<IActionResult> GetProfitLossPdf(
        [FromQuery] DateOnly dateFrom, [FromQuery] DateOnly dateTo, CancellationToken ct)
    {
        var (familyName, currency) = await GetHeaderInfoAsync(ct);
        var report = await reportService.GetProfitLossAsync(dateFrom, dateTo, ct);
        var pdf = pdfService.BuildProfitLoss(report, familyName, currency);
        return File(pdf, "application/pdf", $"ProfitLoss_{dateFrom:yyyyMMdd}-{dateTo:yyyyMMdd}.pdf");
    }

    [HttpGet("balance-sheet/pdf")]
    [HasPermission(PermissionCodes.BalanceSheetView)]
    public async Task<IActionResult> GetBalanceSheetPdf([FromQuery] DateOnly asOf, CancellationToken ct)
    {
        var (familyName, currency) = await GetHeaderInfoAsync(ct);
        var report = await reportService.GetBalanceSheetAsync(asOf, ct);
        var pdf = pdfService.BuildBalanceSheet(report, familyName, currency);
        return File(pdf, "application/pdf", $"BalanceSheet_{asOf:yyyyMMdd}.pdf");
    }

    private async Task<IActionResult> BuildBookPdfAsync(CashBankKind kind, DateOnly dateFrom, DateOnly dateTo, CancellationToken ct)
    {
        var (familyName, currency) = await GetHeaderInfoAsync(ct);
        BookReportDto report = null!;
        var rows = new List<BookRowDto>();
        for (var page = 1; rows.Count < PdfMaxRows; page++)
        {
            report = await reportService.GetBookAsync(kind, new BookRequest
            {
                DateFrom = dateFrom, DateTo = dateTo, Page = page, PageSize = 100
            }, ct);
            rows.AddRange(report.Rows.Items);
            if (page >= report.Rows.TotalPages)
                break;
        }
        var pdf = pdfService.BuildBook(report, rows, familyName, currency);
        var name = kind == CashBankKind.Cash ? "CashBook" : "BankBook";
        return File(pdf, "application/pdf", $"{name}_{dateFrom:yyyyMMdd}-{dateTo:yyyyMMdd}.pdf");
    }

    private async Task<(string FamilyName, string Currency)> GetHeaderInfoAsync(CancellationToken ct)
    {
        var family = await familyService.GetCurrentFamilyAsync(ct);
        var settings = await familyService.GetSettingsAsync(ct);
        return (family.Name, settings.Currency);
    }
}
