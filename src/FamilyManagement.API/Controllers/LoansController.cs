using FamilyManagement.API.Authorization;
using FamilyManagement.Application.Services;
using FamilyManagement.Shared.Authorization;
using FamilyManagement.Shared.Common;
using FamilyManagement.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace FamilyManagement.API.Controllers;

public class LoansController(ILoanService loanService) : ApiControllerBase
{
    /// <summary>Loan.View holders get every member's summary; others get only their own.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<LoanSummaryDto>>>> GetSummaries(CancellationToken ct) =>
        Ok(Success(await loanService.GetSummariesAsync(ct)));

    /// <summary>A member's loan statement (self always; others need Loan.View).</summary>
    [HttpGet("{memberId:guid}/statement")]
    public async Task<ActionResult<ApiResponse<LoanStatementDto>>> GetStatement(
        Guid memberId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default) =>
        Ok(Success(await loanService.GetStatementAsync(memberId, page, pageSize, ct)));

    [HttpPost("give")]
    [HasPermission(PermissionCodes.LoanCreate)]
    public async Task<ActionResult<ApiResponse<LoanSummaryDto>>> GiveLoan(LoanTransactionRequest request, CancellationToken ct) =>
        Ok(Success(await loanService.GiveLoanAsync(request, ct), "Loan recorded"));

    [HttpPost("repay")]
    [HasPermission(PermissionCodes.LoanCreate)]
    public async Task<ActionResult<ApiResponse<LoanSummaryDto>>> RepayLoan(LoanTransactionRequest request, CancellationToken ct) =>
        Ok(Success(await loanService.RepayLoanAsync(request, ct), "Repayment recorded"));
}

[Route("api/v{version:apiVersion}/member-statement")]
public class MemberStatementController(IMemberStatementService statementService) : ApiControllerBase
{
    /// <summary>Member-wise dashboard numbers (self always; others need Member.View).</summary>
    [HttpGet("{memberId:guid}")]
    public async Task<ActionResult<ApiResponse<MemberStatementDto>>> GetStatement(Guid memberId, CancellationToken ct) =>
        Ok(Success(await statementService.GetStatementAsync(memberId, ct)));

    [HttpGet("{memberId:guid}/monthly")]
    public async Task<ActionResult<ApiResponse<List<MonthlyAmountDto>>>> GetMonthly(
        Guid memberId, [FromQuery] int months = 6, CancellationToken ct = default) =>
        Ok(Success(await statementService.GetMonthlyAsync(memberId, months, ct)));

    [HttpGet("{memberId:guid}/expense-categories")]
    public async Task<ActionResult<ApiResponse<List<CategoryAmountDto>>>> GetExpenseCategories(Guid memberId, CancellationToken ct) =>
        Ok(Success(await statementService.GetExpenseCategoriesAsync(memberId, ct)));

    [HttpGet("{memberId:guid}/transactions")]
    public async Task<ActionResult<ApiResponse<PagedResult<VoucherListItemDto>>>> GetTransactions(
        Guid memberId, [FromQuery] PagedRequest request, CancellationToken ct) =>
        Ok(Success(await statementService.GetTransactionsAsync(memberId, request, ct)));
}
