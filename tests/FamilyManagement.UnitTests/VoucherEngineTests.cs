using FamilyManagement.Application.Common.Exceptions;
using FamilyManagement.Application.Services;
using FamilyManagement.Shared.Dtos;
using FamilyManagement.Shared.Enums;

namespace FamilyManagement.UnitTests;

public class VoucherEngineTests : IDisposable
{
    private readonly TestContext _ctx = new();
    private readonly VoucherService _vouchers;
    private readonly ReportService _reports;

    public VoucherEngineTests()
    {
        var audit = new AuditService(_ctx.Db, _ctx.CurrentUser, _ctx.Clock);
        var notifications = new NotificationService(_ctx.Db, _ctx.CurrentUser, _ctx.Clock);
        _vouchers = new VoucherService(_ctx.Db, _ctx.CurrentUser, new FakeCache(), audit, notifications, _ctx.Clock);
        _reports = new ReportService(_ctx.Db);
    }

    private SaveVoucherRequest SalaryReceipt(decimal amount = 50_000m) => new()
    {
        VoucherType = VoucherType.RV,
        VoucherDate = new DateOnly(2026, 10, 5),
        Narration = "Salary",
        Lines =
        [
            new SaveVoucherLineRequest { AccountId = _ctx.AccountId("1111"), Debit = amount, Credit = 0 },
            new SaveVoucherLineRequest { AccountId = _ctx.AccountId("4100"), Debit = 0, Credit = amount }
        ]
    };

    [Fact]
    public async Task Post_BalancedVoucher_AssignsSequentialNumber()
    {
        var draft = await _vouchers.CreateDraftAsync(SalaryReceipt());
        Assert.Equal(VoucherStatus.Draft, draft.Status);
        Assert.StartsWith("DRAFT-", draft.VoucherNumber);

        var posted = await _vouchers.PostAsync(draft.Id);
        Assert.Equal(VoucherStatus.Posted, posted.Status);
        Assert.Equal("RV-2026-0001", posted.VoucherNumber);
        Assert.Equal(posted.TotalDebit, posted.TotalCredit);

        var second = await _vouchers.CreateDraftAsync(SalaryReceipt(1000));
        var secondPosted = await _vouchers.PostAsync(second.Id);
        Assert.Equal("RV-2026-0002", secondPosted.VoucherNumber);
    }

    [Fact]
    public async Task CreateDraft_Unbalanced_IsRejected()
    {
        var request = SalaryReceipt();
        request.Lines[1].Credit = 40_000m;

        var ex = await Assert.ThrowsAsync<BadRequestException>(() => _vouchers.CreateDraftAsync(request));
        Assert.Contains(ex.Errors, e => e.Contains("not balanced"));
    }

    [Fact]
    public async Task CreateDraft_GroupAccount_IsRejected()
    {
        var request = SalaryReceipt();
        request.Lines[0].AccountId = _ctx.AccountId("1110"); // "Cash" group

        await Assert.ThrowsAsync<BadRequestException>(() => _vouchers.CreateDraftAsync(request));
    }

    [Fact]
    public async Task Post_IntoClosedPeriod_IsRejected()
    {
        var draft = await _vouchers.CreateDraftAsync(SalaryReceipt());
        var period = _ctx.Db.AccountingPeriods.Single(p => p.Id == _ctx.PeriodId);
        period.Status = PeriodStatus.Closed;
        await _ctx.Db.SaveChangesAsync();

        await Assert.ThrowsAsync<ConflictException>(() => _vouchers.PostAsync(draft.Id));
    }

    [Fact]
    public async Task PaymentVoucher_WithoutCashCredit_IsRejected()
    {
        var request = new SaveVoucherRequest
        {
            VoucherType = VoucherType.PV,
            VoucherDate = new DateOnly(2026, 10, 5),
            Lines =
            [
                new SaveVoucherLineRequest { AccountId = _ctx.AccountId("5100"), Debit = 500, Credit = 0 },
                new SaveVoucherLineRequest { AccountId = _ctx.AccountId("4100"), Debit = 0, Credit = 500 }
            ]
        };
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => _vouchers.CreateDraftAsync(request));
        Assert.Contains(ex.Errors, e => e.Contains("payment voucher"));
    }

    [Fact]
    public async Task PostedVoucher_CannotBeEditedOrDeleted()
    {
        var draft = await _vouchers.CreateDraftAsync(SalaryReceipt());
        await _vouchers.PostAsync(draft.Id);

        await Assert.ThrowsAsync<ConflictException>(() => _vouchers.UpdateDraftAsync(draft.Id, SalaryReceipt()));
        await Assert.ThrowsAsync<ConflictException>(() => _vouchers.DeleteDraftAsync(draft.Id));
    }

    [Fact]
    public async Task Reverse_CreatesMirror_AndNetsToZero()
    {
        var draft = await _vouchers.CreateDraftAsync(SalaryReceipt());
        var posted = await _vouchers.PostAsync(draft.Id);

        var reversal = await _vouchers.ReverseAsync(posted.Id);
        Assert.Equal(VoucherStatus.Posted, reversal.Status);
        Assert.Equal(posted.Id, reversal.ReversalOfVoucherId);

        var original = await _vouchers.GetAsync(posted.Id);
        Assert.Equal(VoucherStatus.Reversed, original.Status);

        var trialBalance = await _reports.GetTrialBalanceAsync(new DateOnly(2026, 12, 31));
        Assert.True(trialBalance.IsBalanced);
        Assert.Empty(trialBalance.Rows); // everything nets to zero
    }

    [Fact]
    public async Task Ledger_ComputesRunningBalance()
    {
        var first = await _vouchers.CreateDraftAsync(SalaryReceipt(50_000));
        await _vouchers.PostAsync(first.Id);

        var payment = new SaveVoucherRequest
        {
            VoucherType = VoucherType.PV,
            VoucherDate = new DateOnly(2026, 10, 8),
            Lines =
            [
                new SaveVoucherLineRequest { AccountId = _ctx.AccountId("5100"), Debit = 8000, Credit = 0 },
                new SaveVoucherLineRequest { AccountId = _ctx.AccountId("1111"), Debit = 0, Credit = 8000 }
            ]
        };
        var pv = await _vouchers.CreateDraftAsync(payment);
        await _vouchers.PostAsync(pv.Id);

        var ledger = await _reports.GetLedgerAsync(new LedgerRequest
        {
            AccountId = _ctx.AccountId("1111"),
            DateFrom = new DateOnly(2026, 10, 1),
            DateTo = new DateOnly(2026, 10, 31),
            Page = 1,
            PageSize = 20
        });

        Assert.Equal(0, ledger.OpeningBalance);
        Assert.Equal(2, ledger.Rows.Items.Count);
        Assert.Equal(50_000, ledger.Rows.Items[0].RunningBalance);
        Assert.Equal(42_000, ledger.Rows.Items[1].RunningBalance);
        Assert.Equal(42_000, ledger.ClosingBalance);
    }

    [Fact]
    public async Task BalanceSheet_AssetsEqualLiabilitiesPlusEquity()
    {
        var draft = await _vouchers.CreateDraftAsync(SalaryReceipt(75_000));
        await _vouchers.PostAsync(draft.Id);

        var balanceSheet = await _reports.GetBalanceSheetAsync(new DateOnly(2026, 12, 31));
        Assert.True(balanceSheet.IsBalanced);
        Assert.Equal(75_000, balanceSheet.TotalAssets);
        Assert.Equal(75_000, balanceSheet.RetainedEarnings);
    }

    [Fact]
    public async Task ProfitLoss_ComputesNetProfit()
    {
        var income = await _vouchers.CreateDraftAsync(SalaryReceipt(60_000));
        await _vouchers.PostAsync(income.Id);

        var expense = new SaveVoucherRequest
        {
            VoucherType = VoucherType.PV,
            VoucherDate = new DateOnly(2026, 10, 9),
            Lines =
            [
                new SaveVoucherLineRequest { AccountId = _ctx.AccountId("5200"), Debit = 15_000, Credit = 0 },
                new SaveVoucherLineRequest { AccountId = _ctx.AccountId("1111"), Debit = 0, Credit = 15_000 }
            ]
        };
        var pv = await _vouchers.CreateDraftAsync(expense);
        await _vouchers.PostAsync(pv.Id);

        var pnl = await _reports.GetProfitLossAsync(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31));
        Assert.Equal(60_000, pnl.TotalIncome);
        Assert.Equal(15_000, pnl.TotalExpense);
        Assert.Equal(45_000, pnl.NetProfit);
    }

    public void Dispose() => _ctx.Dispose();
}
