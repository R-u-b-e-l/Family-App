using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FamilyManagement.Maui.Services;
using FamilyManagement.Maui.Views;
using FamilyManagement.Maui.Helpers;
using FamilyManagement.Shared.Dtos;

namespace FamilyManagement.Maui.ViewModels;

public partial class DashboardViewModel(ApiClient api, Session session, LocalCache cache) : BaseViewModel
{
    [ObservableProperty] private DashboardSummaryDto _summary = new();
    [ObservableProperty] private List<MonthlyAmountDto> _monthly = [];
    [ObservableProperty] private List<CategoryAmountDto> _expenseCategories = [];
    [ObservableProperty] private List<MemberContributionDto> _memberContributions = [];
    [ObservableProperty] private List<LoanSummaryDto> _loanSummaries = [];
    [ObservableProperty] private string _greeting = "Welcome";
    [ObservableProperty] private string _familyName = "";

    public ObservableCollection<RecentTransactionDto> RecentTransactions { get; } = [];

    public bool CanViewVouchers => session.Can("Voucher.Create");

    [RelayCommand]
    private async Task LoadAsync()
    {
        Greeting = DateTime.Now.Hour switch
        {
            < 12 => $"Good morning, {FirstName()}",
            < 17 => $"Good afternoon, {FirstName()}",
            _ => $"Good evening, {FirstName()}"
        };
        FamilyName = session.CurrentUser?.FamilyName ?? "";

        // Cache-first for instant paint and offline support.
        var (cachedSummary, cachedAt) = await cache.GetAsync<DashboardSummaryDto>("dash_summary");
        if (cachedSummary is not null)
        {
            Summary = cachedSummary;
            IsOfflineData = true;
        }

        await RunAsync(async () =>
        {
            Summary = await api.GetDashboardSummaryAsync();
            Monthly = await api.GetMonthlyIncomeExpenseAsync(6);
            ExpenseCategories = await api.GetExpenseByCategoryAsync();

            MemberContributions = await api.GetMemberContributionsAsync();
            LoanSummaries = await api.GetLoanSummariesAsync();

            var recent = await api.GetRecentTransactionsAsync(8);
            RecentTransactions.Clear();
            foreach (var tx in recent)
                RecentTransactions.Add(tx);

            IsOfflineData = false;
            session.Currency = Summary.Currency;
            MoneyConverter.Symbol = SplashPage.CurrencySymbol(Summary.Currency);
            await cache.SetAsync("dash_summary", Summary);
        }, useBusyFlag: cachedSummary is null);
    }

    [RelayCommand]
    private Task RefreshCommandAsync() => LoadAsync();

    [RelayCommand]
    private Task NewVoucherAsync() => Shell.Current.GoToAsync("quick-voucher");

    [RelayCommand]
    private Task OpenLoansAsync() => Shell.Current.GoToAsync("loans");

    [RelayCommand]
    private async Task OpenMemberAsync(MemberContributionDto? member)
    {
        if (member is not null)
            await Shell.Current.GoToAsync($"member-statement?memberId={member.MemberId}");
    }

    [RelayCommand]
    private Task OpenNotificationsAsync() => Shell.Current.GoToAsync("notifications");

    [RelayCommand]
    private async Task OpenTransactionAsync(RecentTransactionDto? transaction)
    {
        if (transaction is not null)
            await Shell.Current.GoToAsync($"voucher-detail?id={transaction.VoucherId}");
    }

    private string FirstName()
    {
        var name = session.CurrentUser?.FullName;
        return string.IsNullOrWhiteSpace(name) ? "there" : name.Split(' ')[0];
    }
}
