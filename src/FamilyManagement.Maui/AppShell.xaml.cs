using FamilyManagement.Maui.Views;

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
        Routing.RegisterRoute("account-entry", typeof(AccountEntryPage));
        Routing.RegisterRoute("ledger", typeof(LedgerPage));
        Routing.RegisterRoute("book", typeof(BookPage));
        Routing.RegisterRoute("trial-balance", typeof(TrialBalancePage));
        Routing.RegisterRoute("profit-loss", typeof(ProfitLossPage));
        Routing.RegisterRoute("balance-sheet", typeof(BalanceSheetPage));
        Routing.RegisterRoute("members", typeof(MembersPage));
        Routing.RegisterRoute("member-entry", typeof(MemberEntryPage));
        Routing.RegisterRoute("family-settings", typeof(FamilySettingsPage));
        Routing.RegisterRoute("profile", typeof(ProfilePage));
        Routing.RegisterRoute("notifications", typeof(NotificationsPage));
    }
}
