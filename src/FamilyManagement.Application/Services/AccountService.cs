using FamilyManagement.Application.Common;
using FamilyManagement.Application.Common.Exceptions;
using FamilyManagement.Application.Common.Interfaces;
using FamilyManagement.Domain.Entities;
using FamilyManagement.Shared.Common;
using FamilyManagement.Shared.Dtos;
using FamilyManagement.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace FamilyManagement.Application.Services;

public interface IAccountService
{
    Task<List<AccountTreeNodeDto>> GetTreeAsync(CancellationToken ct = default);
    Task<PagedResult<AccountDto>> GetListAsync(PagedRequest request, CancellationToken ct = default);
    Task<AccountDto> GetAsync(Guid accountId, CancellationToken ct = default);
    Task<AccountDto> CreateAsync(CreateAccountRequest request, CancellationToken ct = default);
    Task<AccountDto> UpdateAsync(Guid accountId, UpdateAccountRequest request, CancellationToken ct = default);
    Task SetActiveAsync(Guid accountId, bool isActive, CancellationToken ct = default);
    Task DeleteAsync(Guid accountId, CancellationToken ct = default);
    Task<AccountBalanceDto> GetBalanceAsync(Guid accountId, DateOnly? asOf, CancellationToken ct = default);
}

public class AccountService(
    IAppDbContext db,
    ICurrentUser currentUser,
    ICacheService cache,
    IAuditService audit,
    IDateTimeProvider clock) : IAccountService
{
    public async Task<List<AccountTreeNodeDto>> GetTreeAsync(CancellationToken ct = default)
    {
        return await cache.GetOrCreateAsync(
            CacheKeys.ChartOfAccounts(currentUser.FamilyId),
            TimeSpan.FromHours(1),
            async token =>
            {
                var accounts = await db.Accounts.AsNoTracking()
                    .OrderBy(a => a.Code)
                    .Select(a => new { a.Id, a.ParentAccountId, a.Code, a.Name, a.AccountType, a.IsGroup, a.IsActive, a.Level })
                    .ToListAsync(token);

                var nodes = accounts.ToDictionary(a => a.Id, a => new AccountTreeNodeDto
                {
                    Id = a.Id,
                    Code = a.Code,
                    Name = a.Name,
                    AccountType = a.AccountType,
                    IsGroup = a.IsGroup,
                    IsActive = a.IsActive,
                    Level = a.Level
                });

                var roots = new List<AccountTreeNodeDto>();
                foreach (var a in accounts)
                {
                    if (a.ParentAccountId is { } parentId && nodes.TryGetValue(parentId, out var parent))
                        parent.Children.Add(nodes[a.Id]);
                    else
                        roots.Add(nodes[a.Id]);
                }
                return roots;
            }, ct);
    }

    public async Task<PagedResult<AccountDto>> GetListAsync(PagedRequest request, CancellationToken ct = default)
    {
        var query = db.Accounts.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(a => a.Name.Contains(term) || a.Code.Contains(term));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(a => a.Code)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(DtoProjection)
            .ToListAsync(ct);

        return new PagedResult<AccountDto> { Items = items, Page = request.Page, PageSize = request.PageSize, TotalCount = total };
    }

    public async Task<AccountDto> GetAsync(Guid accountId, CancellationToken ct = default) =>
        await db.Accounts.AsNoTracking()
            .Where(a => a.Id == accountId)
            .Select(DtoProjection)
            .FirstOrDefaultAsync(ct)
        ?? throw new NotFoundException("Account not found.");

    public async Task<AccountDto> CreateAsync(CreateAccountRequest request, CancellationToken ct = default)
    {
        Account account;
        if (request.ParentAccountId is { } parentId)
        {
            var parent = await db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == parentId, ct)
                ?? throw new NotFoundException("Parent account not found.");
            if (!parent.IsGroup)
                throw new BadRequestException("The parent must be a group account.");

            var code = string.IsNullOrWhiteSpace(request.Code)
                ? await GenerateChildCodeAsync(parent, ct)
                : request.Code.Trim();

            account = new Account
            {
                FamilyId = currentUser.FamilyId,
                ParentAccountId = parent.Id,
                Code = code,
                Name = request.Name.Trim(),
                AccountType = parent.AccountType,
                NormalBalance = parent.NormalBalance,
                CashBankKind = parent.CashBankKind,
                IsGroup = request.IsGroup,
                Level = parent.Level + 1,
                Description = request.Description
            };
        }
        else
        {
            if (request.AccountType is not { } type)
                throw new BadRequestException("Account type is required for root accounts.");
            if (string.IsNullOrWhiteSpace(request.Code))
                throw new BadRequestException("Code is required for root accounts.");

            account = new Account
            {
                FamilyId = currentUser.FamilyId,
                Code = request.Code.Trim(),
                Name = request.Name.Trim(),
                AccountType = type,
                NormalBalance = type is AccountType.Asset or AccountType.Expense
                    ? NormalBalance.Debit : NormalBalance.Credit,
                IsGroup = request.IsGroup,
                Level = 0,
                Description = request.Description
            };
        }

        var codeExists = await db.Accounts.AnyAsync(a => a.Code == account.Code, ct);
        if (codeExists)
            throw new ConflictException($"Account code '{account.Code}' already exists.");

        account.CreatedBy = currentUser.UserId;
        account.CreatedDate = clock.UtcNow;
        db.Accounts.Add(account);

        audit.Log("Account.Create", nameof(Account), account.Id.ToString(), null,
            new { account.Code, account.Name, account.AccountType, account.IsGroup });
        await db.SaveChangesAsync(ct);

        cache.Remove(CacheKeys.ChartOfAccounts(currentUser.FamilyId));
        return await GetAsync(account.Id, ct);
    }

    public async Task<AccountDto> UpdateAsync(Guid accountId, UpdateAccountRequest request, CancellationToken ct = default)
    {
        var account = await db.Accounts.FirstOrDefaultAsync(a => a.Id == accountId, ct)
            ?? throw new NotFoundException("Account not found.");
        if (account.IsSystem && account.IsGroup)
            throw new BadRequestException("System group accounts cannot be renamed.");

        var old = new { account.Name, account.Description };
        account.Name = request.Name.Trim();
        account.Description = request.Description;
        account.UpdatedBy = currentUser.UserId;
        account.UpdatedDate = clock.UtcNow;

        audit.Log("Account.Update", nameof(Account), account.Id.ToString(), old, new { account.Name, account.Description });
        await db.SaveChangesAsync(ct);

        cache.Remove(CacheKeys.ChartOfAccounts(currentUser.FamilyId));
        return await GetAsync(account.Id, ct);
    }

    public async Task SetActiveAsync(Guid accountId, bool isActive, CancellationToken ct = default)
    {
        var account = await db.Accounts.FirstOrDefaultAsync(a => a.Id == accountId, ct)
            ?? throw new NotFoundException("Account not found.");
        if (account.IsSystem)
            throw new BadRequestException("System accounts cannot be deactivated.");

        account.IsActive = isActive;
        account.UpdatedBy = currentUser.UserId;
        account.UpdatedDate = clock.UtcNow;

        audit.Log(isActive ? "Account.Activate" : "Account.Deactivate", nameof(Account), account.Id.ToString());
        await db.SaveChangesAsync(ct);
        cache.Remove(CacheKeys.ChartOfAccounts(currentUser.FamilyId));
    }

    public async Task DeleteAsync(Guid accountId, CancellationToken ct = default)
    {
        var account = await db.Accounts.FirstOrDefaultAsync(a => a.Id == accountId, ct)
            ?? throw new NotFoundException("Account not found.");
        if (account.IsSystem)
            throw new BadRequestException("System accounts cannot be deleted.");

        var hasChildren = await db.Accounts.AnyAsync(a => a.ParentAccountId == accountId, ct);
        if (hasChildren)
            throw new ConflictException("The account has child accounts and cannot be deleted.");

        var hasTransactions = await db.VoucherDetails.AnyAsync(d => d.AccountId == accountId, ct);
        if (hasTransactions)
            throw new ConflictException("The account has transactions. Deactivate it instead.");

        db.Accounts.Remove(account);
        audit.Log("Account.Delete", nameof(Account), account.Id.ToString(), new { account.Code, account.Name }, null);
        await db.SaveChangesAsync(ct);
        cache.Remove(CacheKeys.ChartOfAccounts(currentUser.FamilyId));
    }

    public async Task<AccountBalanceDto> GetBalanceAsync(Guid accountId, DateOnly? asOf, CancellationToken ct = default)
    {
        var account = await db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == accountId, ct)
            ?? throw new NotFoundException("Account not found.");

        var asOfDate = asOf ?? clock.Today;

        // Group accounts aggregate every descendant leaf.
        var accountIds = account.IsGroup
            ? await GetDescendantIdsAsync(account.Id, ct)
            : [account.Id];

        var sums = await db.VoucherDetails.AsNoTracking()
            .Where(d => accountIds.Contains(d.AccountId)
                        && d.Voucher!.VoucherDate <= asOfDate
                        && (d.Voucher.Status == VoucherStatus.Posted || d.Voucher.Status == VoucherStatus.Reversed))
            .GroupBy(_ => 1)
            .Select(g => new { Debit = g.Sum(x => x.Debit), Credit = g.Sum(x => x.Credit) })
            .FirstOrDefaultAsync(ct);

        var net = (sums?.Debit ?? 0) - (sums?.Credit ?? 0);
        var signed = account.NormalBalance == NormalBalance.Debit ? net : -net;

        return new AccountBalanceDto
        {
            AccountId = account.Id,
            AccountName = account.Name,
            Balance = signed,
            BalanceSide = account.NormalBalance,
            AsOfDate = asOfDate
        };
    }

    internal async Task<List<Guid>> GetDescendantIdsAsync(Guid rootId, CancellationToken ct)
    {
        var all = await db.Accounts.AsNoTracking()
            .Select(a => new { a.Id, a.ParentAccountId })
            .ToListAsync(ct);

        var byParent = all.Where(a => a.ParentAccountId != null)
            .ToLookup(a => a.ParentAccountId!.Value, a => a.Id);

        var result = new List<Guid>();
        var stack = new Stack<Guid>();
        stack.Push(rootId);
        while (stack.Count > 0)
        {
            var id = stack.Pop();
            result.Add(id);
            foreach (var child in byParent[id])
                stack.Push(child);
        }
        return result;
    }

    private async Task<string> GenerateChildCodeAsync(Account parent, CancellationToken ct)
    {
        var childCodes = await db.Accounts.AsNoTracking()
            .Where(a => a.ParentAccountId == parent.Id)
            .Select(a => a.Code)
            .ToListAsync(ct);

        // Numeric scheme: level 1 children step 100 (1100, 1200…), level 2 step 10 (1110…),
        // level 3 step 1 (1111…). Deeper levels append a 2-digit suffix to the parent code.
        var childLevel = parent.Level + 1;
        var step = childLevel switch { 1 => 100, 2 => 10, 3 => 1, _ => 0 };

        if (step > 0 && int.TryParse(parent.Code, out var baseCode))
        {
            var numericChildren = childCodes
                .Select(c => int.TryParse(c, out var n) ? n : (int?)null)
                .Where(n => n.HasValue)
                .Select(n => n!.Value)
                .ToList();

            var next = numericChildren.Count == 0 ? baseCode + step : numericChildren.Max() + step;
            return next.ToString();
        }

        return $"{parent.Code}{(childCodes.Count + 1):D2}";
    }

    private static readonly System.Linq.Expressions.Expression<Func<Account, AccountDto>> DtoProjection = a => new AccountDto
    {
        Id = a.Id,
        ParentAccountId = a.ParentAccountId,
        Code = a.Code,
        Name = a.Name,
        ParentName = a.Parent != null ? a.Parent.Name : null,
        AccountType = a.AccountType,
        NormalBalance = a.NormalBalance,
        IsGroup = a.IsGroup,
        IsSystem = a.IsSystem,
        IsActive = a.IsActive,
        CashBankKind = a.CashBankKind,
        Level = a.Level,
        Description = a.Description
    };
}
