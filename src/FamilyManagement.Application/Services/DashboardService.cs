using FamilyManagement.Application.Common;
using FamilyManagement.Application.Common.Interfaces;
using FamilyManagement.Domain.Entities;
using FamilyManagement.Shared.Dtos;
using FamilyManagement.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace FamilyManagement.Application.Services;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken ct = default);
    Task<List<MonthlyAmountDto>> GetMonthlyIncomeExpenseAsync(int months, CancellationToken ct = default);
    Task<List<CategoryAmountDto>> GetExpenseByCategoryAsync(DateOnly? from, DateOnly? to, CancellationToken ct = default);
    Task<List<CategoryAmountDto>> GetIncomeByCategoryAsync(DateOnly? from, DateOnly? to, CancellationToken ct = default);
    Task<List<RecentTransactionDto>> GetRecentTransactionsAsync(int count, CancellationToken ct = default);
    /// <summary>Per-member contribution vs expense totals (member-tagged vouchers).</summary>
    Task<List<MemberContributionDto>> GetMemberContributionsAsync(CancellationToken ct = default);
}

public class DashboardService(
    IAppDbContext db,
    ICurrentUser currentUser,
    ICacheService cache,
    IDateTimeProvider clock) : IDashboardService
{
    private IQueryable<VoucherDetail> PostedDetails => db.VoucherDetails.AsNoTracking()
        .Where(d => d.Voucher!.Status == VoucherStatus.Posted || d.Voucher.Status == VoucherStatus.Reversed);

    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken ct = default)
    {
        return await cache.GetOrCreateAsync(
            CacheKeys.Dashboard(currentUser.FamilyId),
            TimeSpan.FromMinutes(5),
            async token =>
            {
                var today = clock.Today;
                var monthStart = new DateOnly(today.Year, today.Month, 1);

                // One aggregation pass over posted lines, grouped by account classification.
                var sums = await PostedDetails
                    .GroupBy(d => new
                    {
                        d.Account!.AccountType,
                        d.Account.CashBankKind,
                        InCurrentMonth = d.Voucher!.VoucherDate >= monthStart && d.Voucher.VoucherDate <= today
                    })
                    .Select(g => new
                    {
                        g.Key.AccountType,
                        g.Key.CashBankKind,
                        g.Key.InCurrentMonth,
                        Debit = g.Sum(x => x.Debit),
                        Credit = g.Sum(x => x.Credit)
                    })
                    .ToListAsync(token);

                decimal cash = sums.Where(s => s.CashBankKind == CashBankKind.Cash).Sum(s => s.Debit - s.Credit);
                decimal bank = sums.Where(s => s.CashBankKind == CashBankKind.Bank).Sum(s => s.Debit - s.Credit);
                decimal totalIncome = sums.Where(s => s.AccountType == AccountType.Income).Sum(s => s.Credit - s.Debit);
                decimal totalExpense = sums.Where(s => s.AccountType == AccountType.Expense).Sum(s => s.Debit - s.Credit);
                decimal monthIncome = sums.Where(s => s.AccountType == AccountType.Income && s.InCurrentMonth).Sum(s => s.Credit - s.Debit);
                decimal monthExpense = sums.Where(s => s.AccountType == AccountType.Expense && s.InCurrentMonth).Sum(s => s.Debit - s.Credit);
                decimal assets = sums.Where(s => s.AccountType == AccountType.Asset).Sum(s => s.Debit - s.Credit);
                decimal liabilities = sums.Where(s => s.AccountType == AccountType.Liability).Sum(s => s.Credit - s.Debit);

                var members = await db.Users.AsNoTracking()
                    .GroupBy(_ => 1)
                    .Select(g => new
                    {
                        Total = g.Count(u => u.Status != EntityStatus.Deleted),
                        Active = g.Count(u => u.Status == EntityStatus.Active)
                    })
                    .FirstOrDefaultAsync(token);

                var currency = await db.FamilySettings.AsNoTracking()
                    .Where(s => s.FamilyId == currentUser.FamilyId)
                    .Select(s => s.Currency)
                    .FirstOrDefaultAsync(token) ?? "BDT";

                return new DashboardSummaryDto
                {
                    TotalBalance = cash + bank,
                    CashBalance = cash,
                    BankBalance = bank,
                    TotalIncome = totalIncome,
                    TotalExpense = totalExpense,
                    CurrentMonthIncome = monthIncome,
                    CurrentMonthExpense = monthExpense,
                    NetProfit = totalIncome - totalExpense,
                    TotalAssets = assets,
                    TotalLiabilities = liabilities,
                    NetWorth = assets - liabilities,
                    TotalMembers = members?.Total ?? 0,
                    ActiveMembers = members?.Active ?? 0,
                    Currency = currency,
                    GeneratedAt = clock.UtcNow
                };
            }, ct);
    }

    public async Task<List<MonthlyAmountDto>> GetMonthlyIncomeExpenseAsync(int months, CancellationToken ct = default)
    {
        months = Math.Clamp(months, 1, 24);
        var today = clock.Today;
        var start = new DateOnly(today.Year, today.Month, 1).AddMonths(-(months - 1));

        var rows = await PostedDetails
            .Where(d => d.Voucher!.VoucherDate >= start
                        && (d.Account!.AccountType == AccountType.Income || d.Account.AccountType == AccountType.Expense))
            .GroupBy(d => new { d.Voucher!.VoucherDate.Year, d.Voucher.VoucherDate.Month, d.Account!.AccountType })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                g.Key.AccountType,
                Debit = g.Sum(x => x.Debit),
                Credit = g.Sum(x => x.Credit)
            })
            .ToListAsync(ct);

        var result = new List<MonthlyAmountDto>();
        for (var i = 0; i < months; i++)
        {
            var m = start.AddMonths(i);
            var income = rows.Where(r => r.Year == m.Year && r.Month == m.Month && r.AccountType == AccountType.Income)
                .Sum(r => r.Credit - r.Debit);
            var expense = rows.Where(r => r.Year == m.Year && r.Month == m.Month && r.AccountType == AccountType.Expense)
                .Sum(r => r.Debit - r.Credit);
            result.Add(new MonthlyAmountDto
            {
                Year = m.Year,
                Month = m.Month,
                Label = m.ToDateTime(TimeOnly.MinValue).ToString("MMM yy"),
                Income = income,
                Expense = expense
            });
        }
        return result;
    }

    public Task<List<CategoryAmountDto>> GetExpenseByCategoryAsync(DateOnly? from, DateOnly? to, CancellationToken ct = default) =>
        GetCategoryAsync(AccountType.Expense, from, to, ct);

    public Task<List<CategoryAmountDto>> GetIncomeByCategoryAsync(DateOnly? from, DateOnly? to, CancellationToken ct = default) =>
        GetCategoryAsync(AccountType.Income, from, to, ct);

    private async Task<List<CategoryAmountDto>> GetCategoryAsync(AccountType type, DateOnly? from, DateOnly? to, CancellationToken ct)
    {
        var today = clock.Today;
        var dateFrom = from ?? new DateOnly(today.Year, today.Month, 1);
        var dateTo = to ?? today;

        var rows = await PostedDetails
            .Where(d => d.Account!.AccountType == type
                        && d.Voucher!.VoucherDate >= dateFrom && d.Voucher.VoucherDate <= dateTo)
            .GroupBy(d => new { d.AccountId, d.Account!.Name })
            .Select(g => new
            {
                g.Key.AccountId,
                g.Key.Name,
                Net = type == AccountType.Expense
                    ? g.Sum(x => x.Debit) - g.Sum(x => x.Credit)
                    : g.Sum(x => x.Credit) - g.Sum(x => x.Debit)
            })
            .Where(x => x.Net != 0)
            .OrderByDescending(x => x.Net)
            .Take(12)
            .ToListAsync(ct);

        return rows.Select(r => new CategoryAmountDto
        {
            AccountId = r.AccountId,
            Category = r.Name,
            Amount = r.Net
        }).ToList();
    }

    public async Task<List<MemberContributionDto>> GetMemberContributionsAsync(CancellationToken ct = default)
    {
        var rows = await PostedDetails
            .Where(d => d.Voucher!.MemberId != null
                        && (d.Account!.AccountType == AccountType.Income || d.Account.AccountType == AccountType.Expense))
            .GroupBy(d => new { d.Voucher!.MemberId, d.Account!.AccountType })
            .Select(g => new
            {
                MemberId = g.Key.MemberId!.Value,
                g.Key.AccountType,
                Debit = g.Sum(x => x.Debit),
                Credit = g.Sum(x => x.Credit)
            })
            .ToListAsync(ct);

        var memberIds = rows.Select(r => r.MemberId).Distinct().ToList();
        var names = await db.Users.AsNoTracking()
            .Where(u => memberIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FullName })
            .ToListAsync(ct);

        return memberIds.Select(id => new MemberContributionDto
        {
            MemberId = id,
            MemberName = names.FirstOrDefault(n => n.Id == id)?.FullName ?? "Unknown",
            Contribution = rows.Where(r => r.MemberId == id && r.AccountType == AccountType.Income)
                .Sum(r => r.Credit - r.Debit),
            Expense = rows.Where(r => r.MemberId == id && r.AccountType == AccountType.Expense)
                .Sum(r => r.Debit - r.Credit)
        })
        .OrderByDescending(m => m.Contribution)
        .ToList();
    }

    public async Task<List<RecentTransactionDto>> GetRecentTransactionsAsync(int count, CancellationToken ct = default)
    {
        count = Math.Clamp(count, 1, 50);
        return await db.Vouchers.AsNoTracking()
            .Where(v => v.Status == VoucherStatus.Posted || v.Status == VoucherStatus.Reversed)
            .OrderByDescending(v => v.VoucherDate)
            .ThenByDescending(v => v.PostedDate)
            .Take(count)
            .Select(v => new RecentTransactionDto
            {
                VoucherId = v.Id,
                VoucherNumber = v.VoucherNumber,
                VoucherType = v.VoucherType,
                Date = v.VoucherDate,
                Narration = v.Narration,
                Amount = v.TotalDebit
            })
            .ToListAsync(ct);
    }
}
