using FamilyManagement.Maui.Services;
using FamilyManagement.Maui.Views;
using FamilyManagement.Shared.Authorization;

namespace FamilyManagement.Maui;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute("register", typeof(RegisterPage));
        Routing.RegisterRoute("forgot-password", typeof(ForgotPasswordPage));
        Routing.RegisterRoute("quick-voucher", typeof(QuickVoucherPage));
        Routing.RegisterRoute("voucher-entry", typeof(VoucherEntryPage));
        Routing.RegisterRoute("voucher-detail", typeof(VoucherDetailPage));
        Routing.RegisterRoute("accounts", typeof(AccountsPage));
        Routing.RegisterRoute("account-entry", typeof(AccountEntryPage));
        Routing.RegisterRoute("ledger", typeof(LedgerPage));
        Routing.RegisterRoute("book", typeof(BookPage));
        Routing.RegisterRoute("trial-balance", typeof(TrialBalancePage));
        Routing.RegisterRoute("profit-loss", typeof(ProfitLossPage));
        Routing.RegisterRoute("balance-sheet", typeof(BalanceSheetPage));
        Routing.RegisterRoute("members", typeof(MembersPage));
        Routing.RegisterRoute("member-entry", typeof(MemberEntryPage));
        Routing.RegisterRoute("member-statement", typeof(MemberStatementPage));
        Routing.RegisterRoute("member-manage", typeof(MemberManagePage));
        Routing.RegisterRoute("loans", typeof(LoansPage));
        Routing.RegisterRoute("loan-detail", typeof(LoanDetailPage));
        Routing.RegisterRoute("loan-entry", typeof(LoanEntryPage));
        Routing.RegisterRoute("roles", typeof(RolesPage));
        Routing.RegisterRoute("role-edit", typeof(RoleEditPage));
        Routing.RegisterRoute("family-settings", typeof(FamilySettingsPage));
        Routing.RegisterRoute("profile", typeof(ProfilePage));
        Routing.RegisterRoute("notifications", typeof(NotificationsPage));
    }

    /// <summary>Shows/hides permission-gated tabs. Call after sign-in and session restore.</summary>
    public static void ApplySession(Session session)
    {
        if (Current is AppShell shell)
            shell.AdminTab.IsVisible = session.Can(PermissionCodes.DashboardAdmin);
    }
}
