using FamilyManagement.API.Authorization;
using FamilyManagement.Application.Services;
using FamilyManagement.Shared.Authorization;
using FamilyManagement.Shared.Common;
using FamilyManagement.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace FamilyManagement.API.Controllers;

public class DashboardController(IDashboardService dashboardService) : ApiControllerBase
{
    [HttpGet("summary")]
    [HasPermission(PermissionCodes.DashboardView)]
    public async Task<ActionResult<ApiResponse<DashboardSummaryDto>>> GetSummary(CancellationToken ct) =>
        Ok(Success(await dashboardService.GetSummaryAsync(ct)));

    [HttpGet("charts/income-expense")]
    [HasPermission(PermissionCodes.DashboardView)]
    public async Task<ActionResult<ApiResponse<List<MonthlyAmountDto>>>> GetIncomeExpense([FromQuery] int months = 6, CancellationToken ct = default) =>
        Ok(Success(await dashboardService.GetMonthlyIncomeExpenseAsync(months, ct)));

    [HttpGet("charts/expense-by-category")]
    [HasPermission(PermissionCodes.DashboardView)]
    public async Task<ActionResult<ApiResponse<List<CategoryAmountDto>>>> GetExpenseByCategory(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct) =>
        Ok(Success(await dashboardService.GetExpenseByCategoryAsync(from, to, ct)));

    [HttpGet("charts/income-by-category")]
    [HasPermission(PermissionCodes.DashboardView)]
    public async Task<ActionResult<ApiResponse<List<CategoryAmountDto>>>> GetIncomeByCategory(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct) =>
        Ok(Success(await dashboardService.GetIncomeByCategoryAsync(from, to, ct)));

    [HttpGet("recent-transactions")]
    [HasPermission(PermissionCodes.DashboardView)]
    public async Task<ActionResult<ApiResponse<List<RecentTransactionDto>>>> GetRecent([FromQuery] int count = 10, CancellationToken ct = default) =>
        Ok(Success(await dashboardService.GetRecentTransactionsAsync(count, ct)));
}
