using FamilyManagement.Application.Common.Exceptions;
using FamilyManagement.Application.Common.Interfaces;
using FamilyManagement.Domain.Entities;
using FamilyManagement.Shared.Authorization;
using FamilyManagement.Shared.Common;
using FamilyManagement.Shared.Dtos;
using FamilyManagement.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace FamilyManagement.Application.Services;

public interface IMemberStatementService
{
    Task<MemberStatementDto> GetStatementAsync(Guid memberId, CancellationToken ct = default);
    Task<List<MonthlyAmountDto>> GetMonthlyAsync(Guid memberId, int months, CancellationToken ct = default);
    Task<List<CategoryAmountDto>> GetExpenseCategoriesAsync(Guid memberId, CancellationToken ct = default);
    Task<PagedResult<VoucherListItemDto>> GetTransactionsAsync(Guid memberId, PagedRequest request, CancellationToken ct = default);
}

/// <summary>
/// The member-wise personal dashboard: a member always sees their own numbers;
/// seeing another member's statement requires Member.View (admin/manager).
/// </summary>
public class MemberStatementService(
    IAppDbContext db,
    ICurrentUser currentUser,
    IPermissionService permissions,
    IDateTimeProvider clock) : IMemberStatementService
{
    public async Task<MemberStatementDto> GetStatementAsync(Guid memberId, CancellationToken ct = default)
    {
        await EnsureAccessAsync(memberId, ct);
        var member = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == memberId, ct)
            ?? throw new NotFoundException("Member not found.");

        var today = clock.Today;
        var monthStart = new DateOnly(today.Year, today.Month, 1);

        var sums = await MemberDetails(memberId)
            .Where(d => d.Account!.AccountType == AccountType.Income || d.Account.AccountType == AccountType.Expense)
            .GroupBy(d => new
            {
                d.Account!.AccountType,
                InMonth = d.Voucher!.VoucherDate >= monthStart && d.Voucher.VoucherDate <= today
            })
            .Select(g => new
            {
                g.Key.AccountType,
                g.Key.InMonth,
                Debit = g.Sum(x => x.Debit),
                Credit = g.Sum(x => x.Credit)
            })
            .ToListAsync(ct);

        var loan = await PostedDetails()
            .Where(d => d.Account!.LinkedMemberId == memberId)
            .GroupBy(_ => 1)
            .Select(g => new { Taken = g.Sum(x => x.Debit), Repaid = g.Sum(x => x.Credit) })
            .FirstOrDefaultAsync(ct);

        var transactionCount = await db.Vouchers.AsNoTracking()
            .CountAsync(v => (v.MemberId == memberId || v.CreatedBy == memberId)
                             && v.Status != VoucherStatus.Draft, ct);

        return new MemberStatementDto
        {
            MemberId = member.Id,
            MemberName = member.FullName,
            ProfilePictureUrl = member.ProfilePicturePath,
            TotalContribution = sums.Where(s => s.AccountType == AccountType.Income).Sum(s => s.Credit - s.Debit),
            TotalExpense = sums.Where(s => s.AccountType == AccountType.Expense).Sum(s => s.Debit - s.Credit),
            MonthContribution = sums.Where(s => s.AccountType == AccountType.Income && s.InMonth).Sum(s => s.Credit - s.Debit),
            MonthExpense = sums.Where(s => s.AccountType == AccountType.Expense && s.InMonth).Sum(s => s.Debit - s.Credit),
            LoanTaken = loan?.Taken ?? 0,
            LoanRepaid = loan?.Repaid ?? 0,
            LoanOutstanding = (loan?.Taken ?? 0) - (loan?.Repaid ?? 0),
            TransactionCount = transactionCount
        };
    }

    public async Task<List<MonthlyAmountDto>> GetMonthlyAsync(Guid memberId, int months, CancellationToken ct = default)
    {
        await EnsureAccessAsync(memberId, ct);
        months = Math.Clamp(months, 1, 24);
        var today = clock.Today;
        var start = new DateOnly(today.Year, today.Month, 1).AddMonths(-(months - 1));

        var rows = await MemberDetails(memberId)
            .Where(d => d.Voucher!.VoucherDate >= start
                        && (d.Account!.AccountType == AccountType.Income || d.Account.AccountType == AccountType.Expense))
            .GroupBy(d => new { d.Voucher!.VoucherDate.Year, d.Voucher.VoucherDate.Month, d.Account!.AccountType })
            .Select(g => new
            {
                g.Key.Year, g.Key.Month, g.Key.AccountType,
                Debit = g.Sum(x => x.Debit),
                Credit = g.Sum(x => x.Credit)
            })
            .ToListAsync(ct);

        var result = new List<MonthlyAmountDto>();
        for (var i = 0; i < months; i++)
        {
            var m = start.AddMonths(i);
            result.Add(new MonthlyAmountDto
            {
                Year = m.Year,
                Month = m.Month,
                Label = m.ToDateTime(TimeOnly.MinValue).ToString("MMM yy"),
                Income = rows.Where(r => r.Year == m.Year && r.Month == m.Month && r.AccountType == AccountType.Income)
                    .Sum(r => r.Credit - r.Debit),
                Expense = rows.Where(r => r.Year == m.Year && r.Month == m.Month && r.AccountType == AccountType.Expense)
                    .Sum(r => r.Debit - r.Credit)
            });
        }
        return result;
    }

    public async Task<List<CategoryAmountDto>> GetExpenseCategoriesAsync(Guid memberId, CancellationToken ct = default)
    {
        await EnsureAccessAsync(memberId, ct);
        var today = clock.Today;
        var start = new DateOnly(today.Year, today.Month, 1);

        var rows = await MemberDetails(memberId)
            .Where(d => d.Account!.AccountType == AccountType.Expense
                        && d.Voucher!.VoucherDate >= start && d.Voucher.VoucherDate <= today)
            .GroupBy(d => new { d.AccountId, d.Account!.Name })
            .Select(g => new { g.Key.AccountId, g.Key.Name, Net = g.Sum(x => x.Debit) - g.Sum(x => x.Credit) })
            .Where(x => x.Net > 0)
            .OrderByDescending(x => x.Net)
            .Take(12)
            .ToListAsync(ct);

        return rows.Select(r => new CategoryAmountDto { AccountId = r.AccountId, Category = r.Name, Amount = r.Net }).ToList();
    }

    public async Task<PagedResult<VoucherListItemDto>> GetTransactionsAsync(Guid memberId, PagedRequest request, CancellationToken ct = default)
    {
        await EnsureAccessAsync(memberId, ct);

        var query = db.Vouchers.AsNoTracking()
            .Where(v => v.MemberId == memberId || v.CreatedBy == memberId);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(v => v.VoucherNumber.Contains(term) || (v.Narration != null && v.Narration.Contains(term)));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(v => v.VoucherDate)
            .ThenByDescending(v => v.CreatedDate)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(v => new VoucherListItemDto
            {
                Id = v.Id,
                VoucherNumber = v.VoucherNumber,
                VoucherType = v.VoucherType,
                VoucherDate = v.VoucherDate,
                Narration = v.Narration,
                Amount = v.TotalDebit,
                Status = v.Status,
                CreatedByName = db.Users.Where(u => u.Id == v.CreatedBy).Select(u => u.FullName).FirstOrDefault() ?? "",
                MemberName = db.Users.Where(u => u.Id == v.MemberId).Select(u => u.FullName).FirstOrDefault()
            })
            .ToListAsync(ct);

        return new PagedResult<VoucherListItemDto> { Items = items, Page = request.Page, PageSize = request.PageSize, TotalCount = total };
    }

    // ---- internals ----------------------------------------------------------

    private IQueryable<VoucherDetail> PostedDetails() => db.VoucherDetails.AsNoTracking()
        .Where(d => d.Voucher!.Status == VoucherStatus.Posted || d.Voucher.Status == VoucherStatus.Reversed);

    private IQueryable<VoucherDetail> MemberDetails(Guid memberId) =>
        PostedDetails().Where(d => d.Voucher!.MemberId == memberId);

    private async Task EnsureAccessAsync(Guid memberId, CancellationToken ct)
    {
        if (memberId == currentUser.UserId)
            return;
        // Other members' statements are for the admin's eyes only.
        var set = await permissions.GetUserPermissionsAsync(currentUser.FamilyId, currentUser.UserId, ct);
        if (!set.Contains(PermissionCodes.DashboardAdmin))
            throw new ForbiddenException("You can only view your own statement.");
    }
}
