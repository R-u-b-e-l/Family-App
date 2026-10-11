using FamilyManagement.Shared.Enums;

namespace FamilyManagement.Shared.Dtos;

public class DashboardSummaryDto
{
    public decimal TotalBalance { get; set; }
    public decimal CashBalance { get; set; }
    public decimal BankBalance { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal CurrentMonthIncome { get; set; }
    public decimal CurrentMonthExpense { get; set; }
    public decimal NetProfit { get; set; }
    public decimal TotalAssets { get; set; }
    public decimal TotalLiabilities { get; set; }
    public decimal NetWorth { get; set; }
    public int TotalMembers { get; set; }
    public int ActiveMembers { get; set; }
    public string Currency { get; set; } = "BDT";
    public DateTime GeneratedAt { get; set; }
}

public class MonthlyAmountDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string Label { get; set; } = string.Empty;
    public decimal Income { get; set; }
    public decimal Expense { get; set; }
}

public class CategoryAmountDto
{
    public Guid AccountId { get; set; }
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class RecentTransactionDto
{
    public Guid VoucherId { get; set; }
    public string VoucherNumber { get; set; } = string.Empty;
    public VoucherType VoucherType { get; set; }
    public DateOnly Date { get; set; }
    public string? Narration { get; set; }
    public decimal Amount { get; set; }
}
