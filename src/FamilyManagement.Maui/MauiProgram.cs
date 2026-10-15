using CommunityToolkit.Maui;
using FamilyManagement.Maui.Services;
using FamilyManagement.Maui.ViewModels;
using FamilyManagement.Maui.Views;
using Microsoft.Extensions.Logging;

namespace FamilyManagement.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Services
        builder.Services.AddSingleton<TokenStore>();
        builder.Services.AddSingleton<Session>();
        builder.Services.AddSingleton<LocalCache>();
        builder.Services.AddSingleton<ApiClient>();

        // ViewModels
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<RegisterViewModel>();
        builder.Services.AddTransient<ForgotPasswordViewModel>();
        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddTransient<MyDashboardViewModel>();
        builder.Services.AddTransient<LoansViewModel>();
        builder.Services.AddTransient<LoanDetailViewModel>();
        builder.Services.AddTransient<LoanEntryViewModel>();
        builder.Services.AddTransient<RolesViewModel>();
        builder.Services.AddTransient<RoleEditViewModel>();
        builder.Services.AddTransient<MemberManageViewModel>();
        builder.Services.AddTransient<AccountsViewModel>();
        builder.Services.AddTransient<AccountEntryViewModel>();
        builder.Services.AddTransient<VouchersViewModel>();
        builder.Services.AddTransient<QuickVoucherViewModel>();
        builder.Services.AddTransient<VoucherEntryViewModel>();
        builder.Services.AddTransient<VoucherDetailViewModel>();
        builder.Services.AddTransient<LedgerViewModel>();
        builder.Services.AddTransient<BookViewModel>();
        builder.Services.AddTransient<TrialBalanceViewModel>();
        builder.Services.AddTransient<ProfitLossViewModel>();
        builder.Services.AddTransient<BalanceSheetViewModel>();
        builder.Services.AddTransient<MembersViewModel>();
        builder.Services.AddTransient<MemberEntryViewModel>();
        builder.Services.AddTransient<FamilySettingsViewModel>();
        builder.Services.AddTransient<ProfileViewModel>();
        builder.Services.AddTransient<NotificationsViewModel>();
        builder.Services.AddTransient<MoreViewModel>();

        // Pages
        builder.Services.AddTransient<SplashPage>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RegisterPage>();
        builder.Services.AddTransient<ForgotPasswordPage>();
        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<MyDashboardPage>();
        builder.Services.AddTransient<MemberStatementPage>();
        builder.Services.AddTransient<LoansPage>();
        builder.Services.AddTransient<LoanDetailPage>();
        builder.Services.AddTransient<LoanEntryPage>();
        builder.Services.AddTransient<RolesPage>();
        builder.Services.AddTransient<RoleEditPage>();
        builder.Services.AddTransient<MemberManagePage>();
        builder.Services.AddTransient<AccountsPage>();
        builder.Services.AddTransient<AccountEntryPage>();
        builder.Services.AddTransient<VouchersPage>();
        builder.Services.AddTransient<QuickVoucherPage>();
        builder.Services.AddTransient<VoucherEntryPage>();
        builder.Services.AddTransient<VoucherDetailPage>();
        builder.Services.AddTransient<ReportsPage>();
        builder.Services.AddTransient<LedgerPage>();
        builder.Services.AddTransient<BookPage>();
        builder.Services.AddTransient<TrialBalancePage>();
        builder.Services.AddTransient<ProfitLossPage>();
        builder.Services.AddTransient<BalanceSheetPage>();
        builder.Services.AddTransient<MembersPage>();
        builder.Services.AddTransient<MemberEntryPage>();
        builder.Services.AddTransient<FamilySettingsPage>();
        builder.Services.AddTransient<ProfilePage>();
        builder.Services.AddTransient<NotificationsPage>();
        builder.Services.AddTransient<MorePage>();

        return builder.Build();
    }
}
