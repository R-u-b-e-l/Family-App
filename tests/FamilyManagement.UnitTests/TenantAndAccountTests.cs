using FamilyManagement.Application.Common.Exceptions;
using FamilyManagement.Application.Services;
using FamilyManagement.Domain.Entities;
using FamilyManagement.Shared.Dtos;
using Microsoft.EntityFrameworkCore;

namespace FamilyManagement.UnitTests;

public class TenantGuardTests : IDisposable
{
    private readonly TestContext _ctx = new();

    [Fact]
    public async Task QueryFilter_HidesOtherFamilies()
    {
        var foreignFamilyId = Guid.NewGuid();
        _ctx.Db.AuditLogs.Add(new AuditLog { FamilyId = _ctx.CurrentUser.FamilyId, Action = "Mine", CreatedDate = DateTime.UtcNow });

        // Insert a foreign row while "unauthenticated" (bypasses the write guard, like a different request would).
        _ctx.CurrentUser.IsAuthenticated = false;
        _ctx.Db.AuditLogs.Add(new AuditLog { FamilyId = foreignFamilyId, Action = "Foreign", CreatedDate = DateTime.UtcNow });
        await _ctx.Db.SaveChangesAsync();
        _ctx.CurrentUser.IsAuthenticated = true;

        var visible = await _ctx.Db.AuditLogs.ToListAsync();
        Assert.Single(visible);
        Assert.Equal("Mine", visible[0].Action);
    }

    [Fact]
    public async Task WriteGuard_BlocksCrossTenantInsert()
    {
        _ctx.Db.AuditLogs.Add(new AuditLog { FamilyId = Guid.NewGuid(), Action = "Evil", CreatedDate = DateTime.UtcNow });
        await Assert.ThrowsAsync<InvalidOperationException>(() => _ctx.Db.SaveChangesAsync());
    }

    public void Dispose() => _ctx.Dispose();
}

public class AccountServiceTests : IDisposable
{
    private readonly TestContext _ctx = new();
    private readonly AccountService _accounts;

    public AccountServiceTests()
    {
        var audit = new AuditService(_ctx.Db, _ctx.CurrentUser, _ctx.Clock);
        _accounts = new AccountService(_ctx.Db, _ctx.CurrentUser, new FakeCache(), audit, _ctx.Clock);
    }

    [Fact]
    public async Task Create_ChildOfBank_GeneratesNextCode_AndInheritsKind()
    {
        var bankGroupId = _ctx.AccountId("1120");
        var account = await _accounts.CreateAsync(new CreateAccountRequest
        {
            ParentAccountId = bankGroupId,
            Name = "DBBL Savings"
        });

        Assert.Equal("1122", account.Code); // 1121 exists from the default CoA
        Assert.Equal(Shared.Enums.CashBankKind.Bank, account.CashBankKind);
        Assert.Equal(Shared.Enums.AccountType.Asset, account.AccountType);
    }

    [Fact]
    public async Task Create_UnderLeafAccount_IsRejected()
    {
        var leafId = _ctx.AccountId("1111");
        await Assert.ThrowsAsync<BadRequestException>(() =>
            _accounts.CreateAsync(new CreateAccountRequest { ParentAccountId = leafId, Name = "X" }));
    }

    [Fact]
    public async Task Delete_AccountWithTransactions_IsRejected()
    {
        var cashId = _ctx.AccountId("1111");
        var salaryId = _ctx.AccountId("4100");
        var audit = new AuditService(_ctx.Db, _ctx.CurrentUser, _ctx.Clock);
        var notifications = new NotificationService(_ctx.Db, _ctx.CurrentUser, _ctx.Clock);
        var vouchers = new VoucherService(_ctx.Db, _ctx.CurrentUser, new FakeCache(), audit, notifications, _ctx.Clock);

        var draft = await vouchers.CreateDraftAsync(new SaveVoucherRequest
        {
            VoucherType = Shared.Enums.VoucherType.RV,
            VoucherDate = new DateOnly(2026, 10, 5),
            Lines =
            [
                new SaveVoucherLineRequest { AccountId = cashId, Debit = 100, Credit = 0 },
                new SaveVoucherLineRequest { AccountId = salaryId, Debit = 0, Credit = 100 }
            ]
        });
        await vouchers.PostAsync(draft.Id);

        await Assert.ThrowsAsync<ConflictException>(() => _accounts.DeleteAsync(salaryId));
    }

    public void Dispose() => _ctx.Dispose();
}
