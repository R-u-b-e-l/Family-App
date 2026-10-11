using FamilyManagement.Application.Common.Exceptions;
using FamilyManagement.Application.Common.Interfaces;
using FamilyManagement.Domain.Entities;
using FamilyManagement.Shared.Common;
using FamilyManagement.Shared.Dtos;
using FamilyManagement.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace FamilyManagement.Application.Services;

public interface IReportService
{
    Task<LedgerReportDto> GetLedgerAsync(LedgerRequest request, CancellationToken ct = default);
    Task<BookReportDto> GetBookAsync(CashBankKind kind, BookRequest request, CancellationToken ct = default);
    Task<TrialBalanceDto> GetTrialBalanceAsync(DateOnly asOf, CancellationToken ct = default);
    Task<ProfitLossDto> GetProfitLossAsync(DateOnly dateFrom, DateOnly dateTo, CancellationToken ct = default);
    Task<BalanceSheetDto> GetBalanceSheetAsync(DateOnly asOf, CancellationToken ct = default);
}

public class ReportService(IAppDbContext db) : IReportService
{
    /// <summary>Reports read posted entries only. Reversed originals stay in (their reversal nets them out).</summary>
    private IQueryable<VoucherDetail> PostedDetails => db.VoucherDetails.AsNoTracking()
        .Where(d => d.Voucher!.Status == VoucherStatus.Posted || d.Voucher.Status == VoucherStatus.Reversed);

    public async Task<LedgerReportDto> GetLedgerAsync(LedgerRequest request, CancellationToken ct = default)
    {
        var account = await db.Accounts.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.AccountId, ct)
            ?? throw new NotFoundException("Account not found.");
        if (account.IsGroup)
            throw new BadRequestException("Ledger is available for transaction (leaf) accounts only.");

        var sign = account.NormalBalance == NormalBalance.Debit ? 1m : -1m;

        var opening = sign * await PostedDetails
            .Where(d => d.AccountId == account.Id && d.Voucher!.VoucherDate < request.DateFrom)
            .SumAsync(d => d.Debit - d.Credit, ct);

        var rangeQuery = PostedDetails
            .Where(d => d.AccountId == account.Id
                        && d.Voucher!.VoucherDate >= request.DateFrom
                        && d.Voucher.VoucherDate <= request.DateTo)
            .OrderBy(d => d.Voucher!.VoucherDate)
            .ThenBy(d => d.Voucher!.VoucherNumber)
            .ThenBy(d => d.LineNo);

        var total = await rangeQuery.CountAsync(ct);
        var totals = await rangeQuery
            .GroupBy(_ => 1)
            .Select(g => new { Debit = g.Sum(x => x.Debit), Credit = g.Sum(x => x.Credit) })
            .FirstOrDefaultAsync(ct);

        var skip = (request.Page - 1) * request.PageSize;
        var prefix = skip == 0
            ? 0m
            : sign * await rangeQuery.Take(skip).SumAsync(d => d.Debit - d.Credit, ct);

        var pageRows = await rangeQuery
            .Skip(skip)
            .Take(request.PageSize)
            .Select(d => new LedgerRowDto
            {
                Date = d.Voucher!.VoucherDate,
                VoucherNumber = d.Voucher.VoucherNumber,
                VoucherId = d.VoucherId,
                VoucherType = d.Voucher.VoucherType,
                Narration = d.Narration ?? d.Voucher.Narration,
                Debit = d.Debit,
                Credit = d.Credit
            })
            .ToListAsync(ct);

        var running = opening + prefix;
        foreach (var row in pageRows)
        {
            running += sign * (row.Debit - row.Credit);
            row.RunningBalance = running;
        }

        var totalDebit = totals?.Debit ?? 0;
        var totalCredit = totals?.Credit ?? 0;

        return new LedgerReportDto
        {
            AccountId = account.Id,
            AccountCode = account.Code,
            AccountName = account.Name,
            DateFrom = request.DateFrom,
            DateTo = request.DateTo,
            OpeningBalance = opening,
            TotalDebit = totalDebit,
            TotalCredit = totalCredit,
            ClosingBalance = opening + sign * (totalDebit - totalCredit),
            Rows = new PagedResult<LedgerRowDto>
            {
                Items = pageRows, Page = request.Page, PageSize = request.PageSize, TotalCount = total
            }
        };
    }

    public async Task<BookReportDto> GetBookAsync(CashBankKind kind, BookRequest request, CancellationToken ct = default)
    {
        if (request.AccountId is { } accountId)
        {
            var account = await db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == accountId, ct)
                ?? throw new NotFoundException("Account not found.");
            if (account.CashBankKind != kind)
                throw new BadRequestException($"The selected account is not a {kind} account.");
        }

        var baseQuery = PostedDetails.Where(d => d.Account!.CashBankKind == kind);
        if (request.AccountId is { } id)
            baseQuery = baseQuery.Where(d => d.AccountId == id);

        var opening = await baseQuery
            .Where(d => d.Voucher!.VoucherDate < request.DateFrom)
            .SumAsync(d => d.Debit - d.Credit, ct);

        var rangeQuery = baseQuery
            .Where(d => d.Voucher!.VoucherDate >= request.DateFrom && d.Voucher.VoucherDate <= request.DateTo)
            .OrderBy(d => d.Voucher!.VoucherDate)
            .ThenBy(d => d.Voucher!.VoucherNumber)
            .ThenBy(d => d.LineNo);

        var total = await rangeQuery.CountAsync(ct);
        var totals = await rangeQuery
            .GroupBy(_ => 1)
            .Select(g => new { Debit = g.Sum(x => x.Debit), Credit = g.Sum(x => x.Credit) })
            .FirstOrDefaultAsync(ct);

        var skip = (request.Page - 1) * request.PageSize;
        var prefix = skip == 0 ? 0m : await rangeQuery.Take(skip).SumAsync(d => d.Debit - d.Credit, ct);

        var pageRows = await rangeQuery
            .Skip(skip)
            .Take(request.PageSize)
            .Select(d => new BookRowDto
            {
                Date = d.Voucher!.VoucherDate,
                VoucherNumber = d.Voucher.VoucherNumber,
                AccountName = d.Account!.Name,
                Narration = d.Narration ?? d.Voucher.Narration,
                Receipt = d.Debit,
                Payment = d.Credit
            })
            .ToListAsync(ct);

        var running = opening + prefix;
        foreach (var row in pageRows)
        {
            running += row.Receipt - row.Payment;
            row.RunningBalance = running;
        }

        var receipts = totals?.Debit ?? 0;
        var payments = totals?.Credit ?? 0;

        return new BookReportDto
        {
            Title = kind == CashBankKind.Cash ? "Cash Book" : "Bank Book",
            DateFrom = request.DateFrom,
            DateTo = request.DateTo,
            OpeningBalance = opening,
            TotalReceipts = receipts,
            TotalPayments = payments,
            ClosingBalance = opening + receipts - payments,
            Rows = new PagedResult<BookRowDto>
            {
                Items = pageRows, Page = request.Page, PageSize = request.PageSize, TotalCount = total
            }
        };
    }

    public async Task<TrialBalanceDto> GetTrialBalanceAsync(DateOnly asOf, CancellationToken ct = default)
    {
        var rows = await PostedDetails
            .Where(d => d.Voucher!.VoucherDate <= asOf)
            .GroupBy(d => new { d.AccountId, d.Account!.Code, d.Account.Name, d.Account.AccountType })
            .Select(g => new
            {
                g.Key.AccountId,
                g.Key.Code,
                g.Key.Name,
                g.Key.AccountType,
                Net = g.Sum(x => x.Debit) - g.Sum(x => x.Credit)
            })
            .Where(x => x.Net != 0)
            .OrderBy(x => x.Code)
            .ToListAsync(ct);

        var dto = new TrialBalanceDto
        {
            AsOfDate = asOf,
            Rows = rows.Select(r => new TrialBalanceRowDto
            {
                AccountId = r.AccountId,
                AccountCode = r.Code,
                AccountName = r.Name,
                AccountType = r.AccountType,
                Debit = r.Net > 0 ? r.Net : 0,
                Credit = r.Net < 0 ? -r.Net : 0
            }).ToList()
        };
        dto.TotalDebit = dto.Rows.Sum(r => r.Debit);
        dto.TotalCredit = dto.Rows.Sum(r => r.Credit);
        return dto;
    }

    public async Task<ProfitLossDto> GetProfitLossAsync(DateOnly dateFrom, DateOnly dateTo, CancellationToken ct = default)
    {
        var lines = await AccountNetLinesAsync(
            d => d.Voucher!.VoucherDate >= dateFrom && d.Voucher.VoucherDate <= dateTo,
            [AccountType.Income, AccountType.Expense], ct);

        var dto = new ProfitLossDto { DateFrom = dateFrom, DateTo = dateTo };
        foreach (var line in lines)
        {
            if (line.Type == AccountType.Income)
            {
                line.Line.Amount = -line.Net; // credit-normal
                if (line.Line.Amount != 0) dto.IncomeLines.Add(line.Line);
            }
            else
            {
                line.Line.Amount = line.Net; // debit-normal
                if (line.Line.Amount != 0) dto.ExpenseLines.Add(line.Line);
            }
        }
        dto.TotalIncome = dto.IncomeLines.Sum(l => l.Amount);
        dto.TotalExpense = dto.ExpenseLines.Sum(l => l.Amount);
        return dto;
    }

    public async Task<BalanceSheetDto> GetBalanceSheetAsync(DateOnly asOf, CancellationToken ct = default)
    {
        var lines = await AccountNetLinesAsync(
            d => d.Voucher!.VoucherDate <= asOf,
            [AccountType.Asset, AccountType.Liability, AccountType.Equity, AccountType.Income, AccountType.Expense], ct);

        var dto = new BalanceSheetDto { AsOfDate = asOf };
        decimal retained = 0;

        foreach (var line in lines)
        {
            switch (line.Type)
            {
                case AccountType.Asset:
                    line.Line.Amount = line.Net;
                    if (line.Line.Amount != 0) dto.AssetLines.Add(line.Line);
                    break;
                case AccountType.Liability:
                    line.Line.Amount = -line.Net;
                    if (line.Line.Amount != 0) dto.LiabilityLines.Add(line.Line);
                    break;
                case AccountType.Equity:
                    line.Line.Amount = -line.Net;
                    if (line.Line.Amount != 0) dto.EquityLines.Add(line.Line);
                    break;
                case AccountType.Income:
                case AccountType.Expense:
                    retained += -line.Net; // income credit-normal adds, expense subtracts
                    break;
            }
        }

        dto.RetainedEarnings = retained;
        dto.TotalAssets = dto.AssetLines.Sum(l => l.Amount);
        dto.TotalLiabilities = dto.LiabilityLines.Sum(l => l.Amount);
        dto.TotalEquity = dto.EquityLines.Sum(l => l.Amount) + retained;

        if (retained != 0)
        {
            dto.EquityLines.Add(new ReportLineDto
            {
                AccountCode = "",
                AccountName = "Retained Earnings (cumulative)",
                Amount = retained,
                Level = 1
            });
        }

        return dto;
    }

    private async Task<List<(AccountType Type, decimal Net, ReportLineDto Line)>> AccountNetLinesAsync(
        System.Linq.Expressions.Expression<Func<VoucherDetail, bool>> dateFilter,
        AccountType[] types, CancellationToken ct)
    {
        var rows = await PostedDetails
            .Where(dateFilter)
            .Where(d => types.Contains(d.Account!.AccountType))
            .GroupBy(d => new { d.AccountId, d.Account!.Code, d.Account.Name, d.Account.AccountType, d.Account.Level })
            .Select(g => new
            {
                g.Key.AccountId,
                g.Key.Code,
                g.Key.Name,
                g.Key.AccountType,
                g.Key.Level,
                Net = g.Sum(x => x.Debit) - g.Sum(x => x.Credit)
            })
            .OrderBy(x => x.Code)
            .ToListAsync(ct);

        return rows.Select(r => (r.AccountType, r.Net, new ReportLineDto
        {
            AccountId = r.AccountId,
            AccountCode = r.Code,
            AccountName = r.Name,
            Level = r.Level
        })).ToList();
    }
}
