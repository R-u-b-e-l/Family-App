using FamilyManagement.Shared.Common;

namespace FamilyManagement.Shared.Dtos;

public class LoanSummaryDto
{
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public Guid? LoanAccountId { get; set; }
    public decimal TotalTaken { get; set; }
    public decimal TotalRepaid { get; set; }
    public decimal Outstanding { get; set; }
}

public class LoanStatementDto
{
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public Guid? LoanAccountId { get; set; }
    public decimal TotalTaken { get; set; }
    public decimal TotalRepaid { get; set; }
    public decimal Outstanding { get; set; }
    /// <summary>Monthly chart points: Expense = taken, Income = repaid.</summary>
    public List<MonthlyAmountDto> Monthly { get; set; } = [];
    public PagedResult<LedgerRowDto> Rows { get; set; } = new();
}

public class LoanTransactionRequest
{
    public Guid MemberId { get; set; }
    public decimal Amount { get; set; }
    /// <summary>The cash/bank account the money moves through.</summary>
    public Guid MoneyAccountId { get; set; }
    public DateOnly Date { get; set; }
    public string? Narration { get; set; }
}

public class MemberStatementDto
{
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public decimal TotalContribution { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal MonthContribution { get; set; }
    public decimal MonthExpense { get; set; }
    public decimal LoanTaken { get; set; }
    public decimal LoanRepaid { get; set; }
    public decimal LoanOutstanding { get; set; }
    public int TransactionCount { get; set; }
}

public class MemberContributionDto
{
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public decimal Contribution { get; set; }
    public decimal Expense { get; set; }
}
