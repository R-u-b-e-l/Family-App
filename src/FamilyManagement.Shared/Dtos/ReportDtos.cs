using FamilyManagement.Shared.Common;
using FamilyManagement.Shared.Enums;

namespace FamilyManagement.Shared.Dtos;

public class LedgerRequest : PagedRequest
{
    public Guid AccountId { get; set; }
    public DateOnly DateFrom { get; set; }
    public DateOnly DateTo { get; set; }
}

public class LedgerRowDto
{
    public DateOnly Date { get; set; }
    public string VoucherNumber { get; set; } = string.Empty;
    public Guid VoucherId { get; set; }
    public VoucherType VoucherType { get; set; }
    public string? Narration { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal RunningBalance { get; set; }
}

public class LedgerReportDto
{
    public Guid AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public DateOnly DateFrom { get; set; }
    public DateOnly DateTo { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public decimal ClosingBalance { get; set; }
    public PagedResult<LedgerRowDto> Rows { get; set; } = new();
}

public class BookRequest : PagedRequest
{
    /// <summary>Specific cash/bank account; null = all accounts of that kind.</summary>
    public Guid? AccountId { get; set; }
    public DateOnly DateFrom { get; set; }
    public DateOnly DateTo { get; set; }
}

public class BookRowDto
{
    public DateOnly Date { get; set; }
    public string VoucherNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string? Narration { get; set; }
    public decimal Receipt { get; set; }
    public decimal Payment { get; set; }
    public decimal RunningBalance { get; set; }
}

public class BookReportDto
{
    public string Title { get; set; } = string.Empty;
    public DateOnly DateFrom { get; set; }
    public DateOnly DateTo { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal TotalReceipts { get; set; }
    public decimal TotalPayments { get; set; }
    public decimal ClosingBalance { get; set; }
    public PagedResult<BookRowDto> Rows { get; set; } = new();
}

public class TrialBalanceRowDto
{
    public Guid AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public AccountType AccountType { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
}

public class TrialBalanceDto
{
    public DateOnly AsOfDate { get; set; }
    public List<TrialBalanceRowDto> Rows { get; set; } = [];
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public bool IsBalanced => TotalDebit == TotalCredit;
}

public class ReportLineDto
{
    public Guid AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int Level { get; set; }
    public bool IsGroup { get; set; }
}

public class ProfitLossDto
{
    public DateOnly DateFrom { get; set; }
    public DateOnly DateTo { get; set; }
    public List<ReportLineDto> IncomeLines { get; set; } = [];
    public List<ReportLineDto> ExpenseLines { get; set; } = [];
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal NetProfit => TotalIncome - TotalExpense;
}

public class BalanceSheetDto
{
    public DateOnly AsOfDate { get; set; }
    public List<ReportLineDto> AssetLines { get; set; } = [];
    public List<ReportLineDto> LiabilityLines { get; set; } = [];
    public List<ReportLineDto> EquityLines { get; set; } = [];
    public decimal TotalAssets { get; set; }
    public decimal TotalLiabilities { get; set; }
    public decimal TotalEquity { get; set; }
    public decimal RetainedEarnings { get; set; }
    public bool IsBalanced => TotalAssets == TotalLiabilities + TotalEquity;
}
