namespace FamilyManagement.Shared.Authorization;

/// <summary>
/// The global permission catalog. The API seeds these codes and enforces them;
/// the mobile app uses them only to show/hide UI.
/// </summary>
public static class PermissionCodes
{
    public const string DashboardView = "Dashboard.View";

    public const string FamilyView = "Family.View";
    public const string FamilyEdit = "Family.Edit";

    public const string MemberView = "Member.View";
    public const string MemberCreate = "Member.Create";
    public const string MemberEdit = "Member.Edit";
    public const string MemberDelete = "Member.Delete";

    public const string RoleView = "Role.View";
    public const string RoleManage = "Role.Manage";

    public const string AccountView = "Account.View";
    public const string AccountCreate = "Account.Create";
    public const string AccountEdit = "Account.Edit";
    public const string AccountDelete = "Account.Delete";

    public const string PeriodView = "Period.View";
    public const string PeriodManage = "Period.Manage";

    public const string VoucherView = "Voucher.View";
    public const string VoucherCreate = "Voucher.Create";
    public const string VoucherEdit = "Voucher.Edit";
    public const string VoucherDelete = "Voucher.Delete";
    public const string VoucherPost = "Voucher.Post";
    public const string VoucherApprove = "Voucher.Approve";

    public const string LedgerView = "Ledger.View";
    public const string CashBookView = "CashBook.View";
    public const string BankBookView = "BankBook.View";
    public const string TrialBalanceView = "TrialBalance.View";
    public const string BalanceSheetView = "BalanceSheet.View";
    public const string ProfitLossView = "ProfitLoss.View";
    public const string ReportsView = "Reports.View";

    public const string AuditLogView = "AuditLog.View";

    public const string SettingsView = "Settings.View";
    public const string SettingsEdit = "Settings.Edit";

    public const string LoanView = "Loan.View";
    public const string LoanCreate = "Loan.Create";

    /// <summary>The family-wide dashboard. Admin-only by design.</summary>
    public const string DashboardAdmin = "Dashboard.Admin";

    /// <summary>All codes grouped by module — single source for seeding and for admin UI.</summary>
    public static readonly IReadOnlyDictionary<string, string[]> Groups = new Dictionary<string, string[]>
    {
        ["Dashboard"] = [DashboardView],
        ["Family"] = [FamilyView, FamilyEdit],
        ["Member"] = [MemberView, MemberCreate, MemberEdit, MemberDelete],
        ["Role"] = [RoleView, RoleManage],
        ["Account"] = [AccountView, AccountCreate, AccountEdit, AccountDelete],
        ["Period"] = [PeriodView, PeriodManage],
        ["Voucher"] = [VoucherView, VoucherCreate, VoucherEdit, VoucherDelete, VoucherPost, VoucherApprove],
        ["Reports"] = [LedgerView, CashBookView, BankBookView, TrialBalanceView, BalanceSheetView, ProfitLossView, ReportsView],
        ["AuditLog"] = [AuditLogView],
        ["Settings"] = [SettingsView, SettingsEdit],
        // New groups are appended only — permission ids are positional and must stay stable.
        ["Loan"] = [LoanView, LoanCreate],
        ["AdminDashboard"] = [DashboardAdmin],
    };

    public static IEnumerable<string> All => Groups.Values.SelectMany(x => x);
}
