using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FamilyManagement.Maui.Services;
using FamilyManagement.Shared.Dtos;

namespace FamilyManagement.Maui.ViewModels;

/// <summary>
/// The member-wise dashboard. With no query parameter it shows the signed-in user;
/// the admin opens it with ?memberId=… to inspect any member.
/// </summary>
[QueryProperty(nameof(MemberIdParam), "memberId")]
[QueryProperty(nameof(MemberNameParam), "memberName")]
public partial class MyDashboardViewModel(ApiClient api, Session session) : BaseViewModel
{
    [ObservableProperty] private string _memberIdParam = string.Empty;
    [ObservableProperty] private string _memberNameParam = string.Empty;

    [ObservableProperty] private MemberStatementDto _statement = new();
    [ObservableProperty] private List<MonthlyAmountDto> _monthly = [];
    [ObservableProperty] private List<CategoryAmountDto> _expenseCategories = [];
    [ObservableProperty] private string _heading = "My Dashboard";

    public ObservableCollection<VoucherListItemDto> Transactions { get; } = [];

    private int _page = 1;
    private int _totalPages = 1;

    private Guid TargetMemberId =>
        Guid.TryParse(MemberIdParam, out var id) ? id : session.CurrentUser?.Id ?? Guid.Empty;

    private bool IsSelf => session.CurrentUser?.Id == TargetMemberId;

    [RelayCommand]
    private Task LoadAsync() => RunAsync(async () =>
    {
        var memberId = TargetMemberId;
        if (memberId == Guid.Empty)
            return;

        Statement = await api.GetMemberStatementAsync(memberId);
        Heading = IsSelf ? "My Dashboard" : Statement.MemberName;
        Monthly = await api.GetMemberMonthlyAsync(memberId, 6);
        ExpenseCategories = await api.GetMemberExpenseCategoriesAsync(memberId);

        _page = 1;
        var transactions = await api.GetMemberTransactionsAsync(memberId, _page);
        _totalPages = transactions.TotalPages;
        Transactions.Clear();
        foreach (var tx in transactions.Items)
            Transactions.Add(tx);
    });

    [RelayCommand]
    private async Task LoadMoreAsync()
    {
        if (IsBusy || _page >= _totalPages)
            return;
        await RunAsync(async () =>
        {
            _page++;
            var transactions = await api.GetMemberTransactionsAsync(TargetMemberId, _page);
            foreach (var tx in transactions.Items)
                Transactions.Add(tx);
        });
    }

    [RelayCommand]
    private async Task OpenTransactionAsync(VoucherListItemDto? voucher)
    {
        if (voucher is not null)
            await Shell.Current.GoToAsync($"voucher-detail?id={voucher.Id}");
    }

    [RelayCommand]
    private Task OpenLoanStatementAsync() =>
        Shell.Current.GoToAsync($"loan-detail?memberId={TargetMemberId}");

    [RelayCommand]
    private Task NewEntryAsync() => Shell.Current.GoToAsync("quick-voucher");
}
