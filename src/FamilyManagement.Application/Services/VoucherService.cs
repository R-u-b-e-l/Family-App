using FamilyManagement.Application.Common;
using FamilyManagement.Application.Common.Exceptions;
using FamilyManagement.Application.Common.Interfaces;
using FamilyManagement.Domain.Entities;
using FamilyManagement.Shared.Common;
using FamilyManagement.Shared.Dtos;
using FamilyManagement.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace FamilyManagement.Application.Services;

public interface IVoucherService
{
    Task<PagedResult<VoucherListItemDto>> GetPagedAsync(VoucherFilterRequest request, CancellationToken ct = default);
    Task<VoucherDto> GetAsync(Guid voucherId, CancellationToken ct = default);
    Task<VoucherDto> CreateDraftAsync(SaveVoucherRequest request, CancellationToken ct = default);
    Task<VoucherDto> UpdateDraftAsync(Guid voucherId, SaveVoucherRequest request, CancellationToken ct = default);
    Task DeleteDraftAsync(Guid voucherId, CancellationToken ct = default);
    Task<VoucherDto> PostAsync(Guid voucherId, CancellationToken ct = default);
    Task<VoucherDto> CancelAsync(Guid voucherId, CancellationToken ct = default);
    Task<VoucherDto> ReverseAsync(Guid voucherId, CancellationToken ct = default);
}

public class VoucherService(
    IAppDbContext db,
    ICurrentUser currentUser,
    ICacheService cache,
    IAuditService audit,
    INotificationService notifications,
    IDateTimeProvider clock) : IVoucherService
{
    public async Task<PagedResult<VoucherListItemDto>> GetPagedAsync(VoucherFilterRequest request, CancellationToken ct = default)
    {
        var query = db.Vouchers.AsNoTracking();

        if (request.VoucherType is { } type)
            query = query.Where(v => v.VoucherType == type);
        if (request.Status is { } status)
            query = query.Where(v => v.Status == status);
        if (request.DateFrom is { } from)
            query = query.Where(v => v.VoucherDate >= from);
        if (request.DateTo is { } to)
            query = query.Where(v => v.VoucherDate <= to);
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

    public async Task<VoucherDto> GetAsync(Guid voucherId, CancellationToken ct = default)
    {
        var dto = await db.Vouchers.AsNoTracking()
            .Where(v => v.Id == voucherId)
            .Select(v => new VoucherDto
            {
                Id = v.Id,
                VoucherNumber = v.VoucherNumber,
                VoucherType = v.VoucherType,
                VoucherDate = v.VoucherDate,
                Narration = v.Narration,
                TotalDebit = v.TotalDebit,
                TotalCredit = v.TotalCredit,
                Status = v.Status,
                ReversalOfVoucherId = v.ReversalOfVoucherId,
                ReversalOfVoucherNumber = v.ReversalOfVoucher != null ? v.ReversalOfVoucher.VoucherNumber : null,
                CreatedByName = db.Users.Where(u => u.Id == v.CreatedBy).Select(u => u.FullName).FirstOrDefault() ?? "",
                CreatedDate = v.CreatedDate,
                PostedByName = db.Users.Where(u => u.Id == v.PostedBy).Select(u => u.FullName).FirstOrDefault(),
                PostedDate = v.PostedDate,
                MemberId = v.MemberId,
                MemberName = db.Users.Where(u => u.Id == v.MemberId).Select(u => u.FullName).FirstOrDefault(),
                Lines = v.Details.OrderBy(d => d.LineNo).Select(d => new VoucherLineDto
                {
                    Id = d.Id,
                    LineNo = d.LineNo,
                    AccountId = d.AccountId,
                    AccountCode = d.Account!.Code,
                    AccountName = d.Account.Name,
                    Debit = d.Debit,
                    Credit = d.Credit,
                    Narration = d.Narration
                }).ToList()
            })
            .FirstOrDefaultAsync(ct);

        return dto ?? throw new NotFoundException("Voucher not found.");
    }

    public async Task<VoucherDto> CreateDraftAsync(SaveVoucherRequest request, CancellationToken ct = default)
    {
        var period = await ResolvePeriodAsync(request.VoucherDate, ct);
        await ValidateLinesAsync(request, ct);
        await ValidateMemberAsync(request.MemberId, ct);

        var voucher = new Voucher
        {
            FamilyId = currentUser.FamilyId,
            VoucherType = request.VoucherType,
            VoucherDate = request.VoucherDate,
            PeriodId = period.Id,
            Narration = request.Narration?.Trim(),
            MemberId = request.MemberId,
            Status = VoucherStatus.Draft,
            CreatedBy = currentUser.UserId,
            CreatedDate = clock.UtcNow
        };
        voucher.VoucherNumber = $"DRAFT-{voucher.Id.ToString("N")[..8].ToUpperInvariant()}";
        ApplyLines(voucher, request.Lines);

        db.Vouchers.Add(voucher);
        audit.Log("Voucher.Create", nameof(Voucher), voucher.Id.ToString(), null,
            new { voucher.VoucherType, voucher.VoucherDate, voucher.TotalDebit });
        await db.SaveChangesAsync(ct);

        return await GetAsync(voucher.Id, ct);
    }

    public async Task<VoucherDto> UpdateDraftAsync(Guid voucherId, SaveVoucherRequest request, CancellationToken ct = default)
    {
        var voucher = await db.Vouchers.Include(v => v.Details)
            .FirstOrDefaultAsync(v => v.Id == voucherId, ct)
            ?? throw new NotFoundException("Voucher not found.");
        if (voucher.Status != VoucherStatus.Draft)
            throw new ConflictException("Only draft vouchers can be edited.");

        var period = await ResolvePeriodAsync(request.VoucherDate, ct);
        await ValidateLinesAsync(request, ct);
        await ValidateMemberAsync(request.MemberId, ct);

        voucher.VoucherType = request.VoucherType;
        voucher.VoucherDate = request.VoucherDate;
        voucher.PeriodId = period.Id;
        voucher.Narration = request.Narration?.Trim();
        voucher.MemberId = request.MemberId;
        voucher.UpdatedBy = currentUser.UserId;
        voucher.UpdatedDate = clock.UtcNow;

        db.VoucherDetails.RemoveRange(voucher.Details);
        voucher.Details.Clear();
        ApplyLines(voucher, request.Lines);

        audit.Log("Voucher.Update", nameof(Voucher), voucher.Id.ToString());
        await db.SaveChangesAsync(ct);
        return await GetAsync(voucher.Id, ct);
    }

    public async Task DeleteDraftAsync(Guid voucherId, CancellationToken ct = default)
    {
        var voucher = await db.Vouchers.Include(v => v.Details)
            .FirstOrDefaultAsync(v => v.Id == voucherId, ct)
            ?? throw new NotFoundException("Voucher not found.");
        if (voucher.Status != VoucherStatus.Draft)
            throw new ConflictException("Only draft vouchers can be deleted. Posted vouchers must be reversed.");

        db.Vouchers.Remove(voucher);
        audit.Log("Voucher.Delete", nameof(Voucher), voucher.Id.ToString(), new { voucher.VoucherNumber }, null);
        await db.SaveChangesAsync(ct);
    }

    public async Task<VoucherDto> PostAsync(Guid voucherId, CancellationToken ct = default)
    {
        await db.ExecuteInTransactionAsync<object?>(async token =>
        {
            var voucher = await db.Vouchers.Include(v => v.Details)
                .FirstOrDefaultAsync(v => v.Id == voucherId, token)
                ?? throw new NotFoundException("Voucher not found.");
            if (voucher.Status != VoucherStatus.Draft)
                throw new ConflictException("Only draft vouchers can be posted.");

            var period = await db.AccountingPeriods
                .FirstOrDefaultAsync(p => p.Id == voucher.PeriodId, token)
                ?? throw new BadRequestException("The voucher's accounting period no longer exists.");
            EnsurePostable(period, voucher.VoucherDate);

            // Re-validate the stored lines against current account state.
            var saveRequest = new SaveVoucherRequest
            {
                VoucherType = voucher.VoucherType,
                VoucherDate = voucher.VoucherDate,
                Lines = voucher.Details.Select(d => new SaveVoucherLineRequest
                {
                    AccountId = d.AccountId, Debit = d.Debit, Credit = d.Credit, Narration = d.Narration
                }).ToList()
            };
            await ValidateLinesAsync(saveRequest, token);

            var fiscalYear = period.StartDate.Year;
            var sequence = await db.GetNextVoucherSequenceAsync(currentUser.FamilyId, voucher.VoucherType, fiscalYear, token);

            voucher.VoucherNumber = $"{voucher.VoucherType}-{fiscalYear}-{sequence:D4}";
            voucher.Status = VoucherStatus.Posted;
            voucher.PostedBy = currentUser.UserId;
            voucher.PostedDate = clock.UtcNow;
            voucher.TotalDebit = voucher.Details.Sum(d => d.Debit);
            voucher.TotalCredit = voucher.Details.Sum(d => d.Credit);

            audit.Log("Voucher.Post", nameof(Voucher), voucher.Id.ToString(), null,
                new { voucher.VoucherNumber, voucher.TotalDebit });
            await NotifyFamilyAsync(NotificationType.VoucherPosted, $"Voucher {voucher.VoucherNumber} posted",
                voucher.Narration, token);

            await db.SaveChangesAsync(token);
            return null;
        }, ct);

        cache.Remove(CacheKeys.Dashboard(currentUser.FamilyId));
        return await GetAsync(voucherId, ct);
    }

    public async Task<VoucherDto> CancelAsync(Guid voucherId, CancellationToken ct = default)
    {
        var voucher = await db.Vouchers.FirstOrDefaultAsync(v => v.Id == voucherId, ct)
            ?? throw new NotFoundException("Voucher not found.");
        if (voucher.Status != VoucherStatus.Posted)
            throw new ConflictException("Only posted vouchers can be cancelled.");

        voucher.Status = VoucherStatus.Cancelled;
        voucher.UpdatedBy = currentUser.UserId;
        voucher.UpdatedDate = clock.UtcNow;

        audit.Log("Voucher.Cancel", nameof(Voucher), voucher.Id.ToString(), new { voucher.VoucherNumber }, null);
        await NotifyFamilyAsync(NotificationType.VoucherCancelled, $"Voucher {voucher.VoucherNumber} cancelled", voucher.Narration, ct);
        await db.SaveChangesAsync(ct);

        cache.Remove(CacheKeys.Dashboard(currentUser.FamilyId));
        return await GetAsync(voucherId, ct);
    }

    public async Task<VoucherDto> ReverseAsync(Guid voucherId, CancellationToken ct = default)
    {
        var reversalId = await db.ExecuteInTransactionAsync(async token =>
        {
            var original = await db.Vouchers.Include(v => v.Details)
                .FirstOrDefaultAsync(v => v.Id == voucherId, token)
                ?? throw new NotFoundException("Voucher not found.");
            if (original.Status != VoucherStatus.Posted)
                throw new ConflictException("Only posted vouchers can be reversed.");

            var today = clock.Today;
            var period = await db.AccountingPeriods
                .FirstOrDefaultAsync(p => p.StartDate <= today && p.EndDate >= today, token)
                ?? throw new BadRequestException("No accounting period covers today's date for the reversal entry.");
            EnsurePostable(period, today);

            var fiscalYear = period.StartDate.Year;
            var sequence = await db.GetNextVoucherSequenceAsync(currentUser.FamilyId, original.VoucherType, fiscalYear, token);

            var reversal = new Voucher
            {
                FamilyId = currentUser.FamilyId,
                VoucherType = original.VoucherType,
                VoucherNumber = $"{original.VoucherType}-{fiscalYear}-{sequence:D4}",
                VoucherDate = today,
                PeriodId = period.Id,
                Narration = $"Reversal of {original.VoucherNumber}" +
                            (string.IsNullOrWhiteSpace(original.Narration) ? "" : $" — {original.Narration}"),
                Status = VoucherStatus.Posted,
                ReversalOfVoucherId = original.Id,
                MemberId = original.MemberId,
                CreatedBy = currentUser.UserId,
                CreatedDate = clock.UtcNow,
                PostedBy = currentUser.UserId,
                PostedDate = clock.UtcNow,
                TotalDebit = original.TotalCredit,
                TotalCredit = original.TotalDebit
            };
            var lineNo = 1;
            foreach (var d in original.Details.OrderBy(x => x.LineNo))
            {
                reversal.Details.Add(new VoucherDetail
                {
                    VoucherId = reversal.Id,
                    LineNo = lineNo++,
                    AccountId = d.AccountId,
                    Debit = d.Credit,
                    Credit = d.Debit,
                    Narration = d.Narration
                });
            }

            original.Status = VoucherStatus.Reversed;
            original.UpdatedBy = currentUser.UserId;
            original.UpdatedDate = clock.UtcNow;

            db.Vouchers.Add(reversal);
            audit.Log("Voucher.Reverse", nameof(Voucher), original.Id.ToString(),
                new { original.VoucherNumber }, new { ReversalNumber = reversal.VoucherNumber });
            await NotifyFamilyAsync(NotificationType.VoucherReversed,
                $"Voucher {original.VoucherNumber} reversed by {reversal.VoucherNumber}", null, token);

            await db.SaveChangesAsync(token);
            return reversal.Id;
        }, ct);

        cache.Remove(CacheKeys.Dashboard(currentUser.FamilyId));
        return await GetAsync(reversalId, ct);
    }

    // ---- internals -------------------------------------------------------

    private void ApplyLines(Voucher voucher, List<SaveVoucherLineRequest> lines)
    {
        var lineNo = 1;
        foreach (var line in lines)
        {
            voucher.Details.Add(new VoucherDetail
            {
                VoucherId = voucher.Id,
                LineNo = lineNo++,
                AccountId = line.AccountId,
                Debit = decimal.Round(line.Debit, 2),
                Credit = decimal.Round(line.Credit, 2),
                Narration = line.Narration?.Trim()
            });
        }
        voucher.TotalDebit = voucher.Details.Sum(d => d.Debit);
        voucher.TotalCredit = voucher.Details.Sum(d => d.Credit);
    }

    private async Task ValidateMemberAsync(Guid? memberId, CancellationToken ct)
    {
        if (memberId is null)
            return;
        // Tenant filter on Users guarantees the member belongs to this family.
        var exists = await db.Users.AsNoTracking().AnyAsync(u => u.Id == memberId, ct);
        if (!exists)
            throw new BadRequestException("The selected member was not found.");
    }

    private async Task<AccountingPeriod> ResolvePeriodAsync(DateOnly voucherDate, CancellationToken ct) =>
        await db.AccountingPeriods
            .FirstOrDefaultAsync(p => p.StartDate <= voucherDate && p.EndDate >= voucherDate, ct)
        ?? throw new BadRequestException($"No accounting period covers {voucherDate:dd-MMM-yyyy}. Create the period first.");

    private static void EnsurePostable(AccountingPeriod period, DateOnly date)
    {
        if (period.Status == PeriodStatus.Closed)
            throw new ConflictException($"Accounting period '{period.Name}' is closed. Posting is not allowed.");
        if (!period.Contains(date))
            throw new BadRequestException("The voucher date falls outside its accounting period.");
    }

    private async Task ValidateLinesAsync(SaveVoucherRequest request, CancellationToken ct)
    {
        var errors = new List<string>();

        if (request.Lines.Count < 2)
            errors.Add("A voucher needs at least two lines (one debit, one credit).");

        foreach (var (line, i) in request.Lines.Select((l, i) => (l, i + 1)))
        {
            if (line.Debit < 0 || line.Credit < 0)
                errors.Add($"Line {i}: amounts cannot be negative.");
            if (!((line.Debit > 0) ^ (line.Credit > 0)))
                errors.Add($"Line {i}: exactly one of debit or credit must be greater than zero.");
        }

        var totalDebit = request.Lines.Sum(l => l.Debit);
        var totalCredit = request.Lines.Sum(l => l.Credit);
        if (decimal.Round(totalDebit, 2) != decimal.Round(totalCredit, 2))
            errors.Add($"The voucher is not balanced: total debit {totalDebit:N2} ≠ total credit {totalCredit:N2}.");
        if (totalDebit <= 0)
            errors.Add("The voucher amount must be greater than zero.");

        if (errors.Count > 0)
            throw new BadRequestException("Voucher validation failed.", errors);

        var accountIds = request.Lines.Select(l => l.AccountId).Distinct().ToList();
        var accounts = await db.Accounts.AsNoTracking()
            .Where(a => accountIds.Contains(a.Id))
            .Select(a => new { a.Id, a.Name, a.IsGroup, a.IsActive, a.CashBankKind })
            .ToListAsync(ct);

        foreach (var id in accountIds.Except(accounts.Select(a => a.Id)))
            errors.Add($"Account {id} was not found.");
        foreach (var a in accounts.Where(a => a.IsGroup))
            errors.Add($"'{a.Name}' is a group account and cannot receive postings.");
        foreach (var a in accounts.Where(a => !a.IsActive))
            errors.Add($"Account '{a.Name}' is inactive.");

        if (errors.Count == 0)
        {
            var kinds = accounts.ToDictionary(a => a.Id, a => a.CashBankKind);
            bool IsMoney(Guid id) => kinds[id] != CashBankKind.None;

            switch (request.VoucherType)
            {
                case VoucherType.PV when !request.Lines.Any(l => l.Credit > 0 && IsMoney(l.AccountId)):
                    errors.Add("A payment voucher must credit a cash or bank account.");
                    break;
                case VoucherType.RV when !request.Lines.Any(l => l.Debit > 0 && IsMoney(l.AccountId)):
                    errors.Add("A receipt voucher must debit a cash or bank account.");
                    break;
                case VoucherType.CV when !request.Lines.All(l => IsMoney(l.AccountId)):
                    errors.Add("A contra voucher may only use cash and bank accounts.");
                    break;
            }
        }

        if (errors.Count > 0)
            throw new BadRequestException("Voucher validation failed.", errors);
    }

    private async Task NotifyFamilyAsync(NotificationType type, string title, string? body, CancellationToken ct)
    {
        var recipientIds = await db.Users.AsNoTracking()
            .Where(u => u.Status == EntityStatus.Active && u.Id != currentUser.UserId)
            .Select(u => u.Id)
            .ToListAsync(ct);
        notifications.Notify(currentUser.FamilyId, recipientIds, type, title, body);
    }
}
