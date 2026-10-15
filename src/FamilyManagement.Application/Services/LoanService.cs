using FamilyManagement.Application.Common;
using FamilyManagement.Application.Common.Exceptions;
using FamilyManagement.Application.Common.Interfaces;
using FamilyManagement.Domain.Entities;
using FamilyManagement.Shared.Authorization;
using FamilyManagement.Shared.Dtos;
using FamilyManagement.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace FamilyManagement.Application.Services;

public interface ILoanService
{
    /// <summary>All members' loan summaries for Loan.View holders; otherwise only the caller's own.</summary>
    Task<List<LoanSummaryDto>> GetSummariesAsync(CancellationToken ct = default);
    Task<LoanStatementDto> GetStatementAsync(Guid memberId, int page = 1, int pageSize = 50, CancellationToken ct = default);
    Task<LoanSummaryDto> GiveLoanAsync(LoanTransactionRequest request, CancellationToken ct = default);
    Task<LoanSummaryDto> RepayLoanAsync(LoanTransactionRequest request, CancellationToken ct = default);
}

public class LoanService(
    IAppDbContext db,
    ICurrentUser currentUser,
    IPermissionService permissions,
    IVoucherService vouchers,
    IReportService reports,
    ICacheService cache,
    IDateTimeProvider clock) : ILoanService
{
    private const string LoanGroupCode = "1140";

    public async Task<List<LoanSummaryDto>> GetSummariesAsync(CancellationToken ct = default)
    {
        var canViewAll = await CanAsync(PermissionCodes.LoanView, ct);

        var accountsQuery = db.Accounts.AsNoTracking().Where(a => a.LinkedMemberId != null);
        if (!canViewAll)
            accountsQuery = accountsQuery.Where(a => a.LinkedMemberId == currentUser.UserId);

        var accounts = await accountsQuery
            .Select(a => new { a.Id, MemberId = a.LinkedMemberId!.Value })
            .ToListAsync(ct);

        var accountIds = accounts.Select(a => a.Id).ToList();
        var sums = await PostedDetails()
            .Where(d => accountIds.Contains(d.AccountId))
            .GroupBy(d => d.AccountId)
            .Select(g => new { AccountId = g.Key, Taken = g.Sum(x => x.Debit), Repaid = g.Sum(x => x.Credit) })
            .ToListAsync(ct);

        var memberIds = accounts.Select(a => a.MemberId).ToList();
        var names = await db.Users.AsNoTracking()
            .Where(u => memberIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FullName })
            .ToListAsync(ct);

        return accounts
            .Select(a =>
            {
                var sum = sums.FirstOrDefault(s => s.AccountId == a.Id);
                return new LoanSummaryDto
                {
                    MemberId = a.MemberId,
                    MemberName = names.FirstOrDefault(n => n.Id == a.MemberId)?.FullName ?? "Unknown",
                    LoanAccountId = a.Id,
                    TotalTaken = sum?.Taken ?? 0,
                    TotalRepaid = sum?.Repaid ?? 0,
                    Outstanding = (sum?.Taken ?? 0) - (sum?.Repaid ?? 0)
                };
            })
            .OrderByDescending(s => s.Outstanding)
            .ToList();
    }

    public async Task<LoanStatementDto> GetStatementAsync(Guid memberId, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        await EnsureCanSeeMemberAsync(memberId, ct);

        var member = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == memberId, ct)
            ?? throw new NotFoundException("Member not found.");

        var statement = new LoanStatementDto { MemberId = memberId, MemberName = member.FullName };

        var account = await db.Accounts.AsNoTracking()
            .FirstOrDefaultAsync(a => a.LinkedMemberId == memberId, ct);
        if (account is null)
            return statement; // no loan yet — everything stays zero

        statement.LoanAccountId = account.Id;

        var sums = await PostedDetails()
            .Where(d => d.AccountId == account.Id)
            .GroupBy(_ => 1)
            .Select(g => new { Taken = g.Sum(x => x.Debit), Repaid = g.Sum(x => x.Credit) })
            .FirstOrDefaultAsync(ct);
        statement.TotalTaken = sums?.Taken ?? 0;
        statement.TotalRepaid = sums?.Repaid ?? 0;
        statement.Outstanding = statement.TotalTaken - statement.TotalRepaid;

        // 12-month chart: Expense = taken, Income = repaid.
        var today = clock.Today;
        var chartStart = new DateOnly(today.Year, today.Month, 1).AddMonths(-11);
        var monthlyRaw = await PostedDetails()
            .Where(d => d.AccountId == account.Id && d.Voucher!.VoucherDate >= chartStart)
            .GroupBy(d => new { d.Voucher!.VoucherDate.Year, d.Voucher.VoucherDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Taken = g.Sum(x => x.Debit), Repaid = g.Sum(x => x.Credit) })
            .ToListAsync(ct);

        for (var i = 0; i < 12; i++)
        {
            var m = chartStart.AddMonths(i);
            var raw = monthlyRaw.FirstOrDefault(r => r.Year == m.Year && r.Month == m.Month);
            statement.Monthly.Add(new MonthlyAmountDto
            {
                Year = m.Year,
                Month = m.Month,
                Label = m.ToDateTime(TimeOnly.MinValue).ToString("MMM"),
                Income = raw?.Repaid ?? 0,
                Expense = raw?.Taken ?? 0
            });
        }

        var ledger = await reports.GetLedgerAsync(new LedgerRequest
        {
            AccountId = account.Id,
            DateFrom = new DateOnly(2000, 1, 1),
            DateTo = today,
            Page = page,
            PageSize = pageSize
        }, ct);
        statement.Rows = ledger.Rows;

        return statement;
    }

    public Task<LoanSummaryDto> GiveLoanAsync(LoanTransactionRequest request, CancellationToken ct = default) =>
        PostLoanVoucherAsync(request, isRepayment: false, ct);

    public Task<LoanSummaryDto> RepayLoanAsync(LoanTransactionRequest request, CancellationToken ct = default) =>
        PostLoanVoucherAsync(request, isRepayment: true, ct);

    // ---- internals ---------------------------------------------------------

    private async Task<LoanSummaryDto> PostLoanVoucherAsync(LoanTransactionRequest request, bool isRepayment, CancellationToken ct)
    {
        var member = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.MemberId && u.Status == EntityStatus.Active, ct)
            ?? throw new NotFoundException("Member not found or inactive.");

        var loanAccount = await EnsureLoanAccountAsync(member.Id, member.FullName, ct);

        if (isRepayment)
        {
            var outstanding = await GetOutstandingAsync(loanAccount.Id, ct);
            if (request.Amount > outstanding)
                throw new BadRequestException(
                    $"Repayment ({request.Amount:N2}) is more than the outstanding loan ({outstanding:N2}).");
        }

        var narration = string.IsNullOrWhiteSpace(request.Narration)
            ? (isRepayment ? $"Loan repayment — {member.FullName}" : $"Loan given — {member.FullName}")
            : request.Narration.Trim();

        var voucherRequest = new SaveVoucherRequest
        {
            VoucherType = isRepayment ? VoucherType.RV : VoucherType.PV,
            VoucherDate = request.Date,
            Narration = narration,
            MemberId = member.Id,
            Lines = isRepayment
                ?
                [
                    new SaveVoucherLineRequest { AccountId = request.MoneyAccountId, Debit = request.Amount, Credit = 0 },
                    new SaveVoucherLineRequest { AccountId = loanAccount.Id, Debit = 0, Credit = request.Amount }
                ]
                :
                [
                    new SaveVoucherLineRequest { AccountId = loanAccount.Id, Debit = request.Amount, Credit = 0 },
                    new SaveVoucherLineRequest { AccountId = request.MoneyAccountId, Debit = 0, Credit = request.Amount }
                ]
        };

        var draft = await vouchers.CreateDraftAsync(voucherRequest, ct);
        await vouchers.PostAsync(draft.Id, ct);

        var summaries = await GetSummariesAsync(ct);
        return summaries.First(s => s.MemberId == member.Id);
    }

    private async Task<Account> EnsureLoanAccountAsync(Guid memberId, string memberName, CancellationToken ct)
    {
        var existing = await db.Accounts.FirstOrDefaultAsync(a => a.LinkedMemberId == memberId, ct);
        if (existing is not null)
            return existing;

        var now = clock.UtcNow;
        var group = await db.Accounts.FirstOrDefaultAsync(a => a.Code == LoanGroupCode && a.IsGroup, ct);
        if (group is null)
        {
            var currentAssets = await db.Accounts.FirstOrDefaultAsync(a => a.Code == "1100" && a.IsGroup, ct)
                ?? throw new NotFoundException("The Current Assets group (1100) is missing from the chart of accounts.");
            group = new Account
            {
                FamilyId = currentUser.FamilyId,
                ParentAccountId = currentAssets.Id,
                Code = LoanGroupCode,
                Name = "Member Loans",
                AccountType = AccountType.Asset,
                NormalBalance = NormalBalance.Debit,
                IsGroup = true,
                IsSystem = true,
                Level = currentAssets.Level + 1,
                CreatedBy = currentUser.UserId,
                CreatedDate = now
            };
            db.Accounts.Add(group);
        }

        var code = int.Parse(LoanGroupCode) + 1;
        while (await db.Accounts.AnyAsync(a => a.Code == code.ToString(), ct))
            code++;

        var account = new Account
        {
            FamilyId = currentUser.FamilyId,
            ParentAccountId = group.Id,
            Code = code.ToString(),
            Name = $"Loan — {memberName}",
            AccountType = AccountType.Asset,
            NormalBalance = NormalBalance.Debit,
            IsGroup = false,
            IsSystem = true,
            LinkedMemberId = memberId,
            Level = group.Level + 1,
            CreatedBy = currentUser.UserId,
            CreatedDate = now
        };
        db.Accounts.Add(account);
        await db.SaveChangesAsync(ct);

        cache.Remove(CacheKeys.ChartOfAccounts(currentUser.FamilyId));
        return account;
    }

    private async Task<decimal> GetOutstandingAsync(Guid loanAccountId, CancellationToken ct)
    {
        var sums = await PostedDetails()
            .Where(d => d.AccountId == loanAccountId)
            .GroupBy(_ => 1)
            .Select(g => new { Taken = g.Sum(x => x.Debit), Repaid = g.Sum(x => x.Credit) })
            .FirstOrDefaultAsync(ct);
        return (sums?.Taken ?? 0) - (sums?.Repaid ?? 0);
    }

    private IQueryable<VoucherDetail> PostedDetails() => db.VoucherDetails.AsNoTracking()
        .Where(d => d.Voucher!.Status == VoucherStatus.Posted || d.Voucher.Status == VoucherStatus.Reversed);

    private async Task<bool> CanAsync(string permission, CancellationToken ct) =>
        (await permissions.GetUserPermissionsAsync(currentUser.FamilyId, currentUser.UserId, ct)).Contains(permission);

    private async Task EnsureCanSeeMemberAsync(Guid memberId, CancellationToken ct)
    {
        if (memberId == currentUser.UserId)
            return;
        if (!await CanAsync(PermissionCodes.LoanView, ct))
            throw new ForbiddenException("You can only view your own loan statement.");
    }
}
