using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FamilyManagement.Maui.Services;
using FamilyManagement.Shared.Authorization;
using FamilyManagement.Shared.Dtos;

namespace FamilyManagement.Maui.ViewModels;

public partial class LoansViewModel(ApiClient api, Session session) : BaseViewModel
{
    public ObservableCollection<LoanSummaryDto> Summaries { get; } = [];

    [ObservableProperty] private decimal _totalOutstanding;

    public bool CanManage => session.Can(PermissionCodes.LoanCreate);

    [RelayCommand]
    private Task LoadAsync() => RunAsync(async () =>
    {
        var summaries = await api.GetLoanSummariesAsync();
        Summaries.Clear();
        foreach (var summary in summaries)
            Summaries.Add(summary);
        TotalOutstanding = summaries.Sum(s => s.Outstanding);
    });

    [RelayCommand]
    private async Task OpenStatementAsync(LoanSummaryDto? summary)
    {
        if (summary is not null)
            await Shell.Current.GoToAsync($"loan-detail?memberId={summary.MemberId}");
    }

    [RelayCommand]
    private Task GiveLoanAsync() => Shell.Current.GoToAsync("loan-entry?mode=give");

    [RelayCommand]
    private Task RepayLoanAsync() => Shell.Current.GoToAsync("loan-entry?mode=repay");
}

[QueryProperty(nameof(MemberIdParam), "memberId")]
public partial class LoanDetailViewModel(ApiClient api) : BaseViewModel
{
    [ObservableProperty] private string _memberIdParam = string.Empty;
    [ObservableProperty] private LoanStatementDto _statement = new();

    public ObservableCollection<LedgerRowDto> Rows { get; } = [];

    [RelayCommand]
    private Task LoadAsync() => RunAsync(async () =>
    {
        if (!Guid.TryParse(MemberIdParam, out var memberId))
            return;
        Statement = await api.GetLoanStatementAsync(memberId);
        Rows.Clear();
        foreach (var row in Statement.Rows.Items)
            Rows.Add(row);
    });

    [RelayCommand]
    private Task DownloadPdfAsync() => RunAsync(async () =>
    {
        if (Statement.LoanAccountId is not { } accountId)
        {
            await ToastAsync("এখনো কোনো loan লেনদেন নেই।");
            return;
        }
        var today = DateOnly.FromDateTime(DateTime.Today);
        var path = await api.DownloadPdfAsync(
            $"/reports/ledger/pdf?accountId={accountId}&dateFrom=2000-01-01&dateTo={today:yyyy-MM-dd}",
            $"LoanStatement_{Statement.MemberName.Replace(" ", "")}.pdf");
        await OpenPdfAsync(path, "Loan Statement");
    });
}

[QueryProperty(nameof(Mode), "mode")]
public partial class LoanEntryViewModel(ApiClient api) : BaseViewModel
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsGive), nameof(PageTitle), nameof(ActionText))]
    private string _mode = "give";

    [ObservableProperty] private UserDto? _selectedMember;
    [ObservableProperty] private AccountDto? _moneyAccount;
    [ObservableProperty] private string _amount = string.Empty;
    [ObservableProperty] private DateTime _date = DateTime.Today;
    [ObservableProperty] private string _narration = string.Empty;

    public ObservableCollection<UserDto> Members { get; } = [];
    public ObservableCollection<AccountDto> MoneyAccounts { get; } = [];

    public bool IsGive => Mode != "repay";
    public string PageTitle => IsGive ? "Loan দিন" : "Loan ফেরত নিন";
    public string ActionText => IsGive ? "✓ Loan দিন" : "✓ ফেরত নিন";

    [RelayCommand]
    private Task LoadAsync() => RunAsync(async () =>
    {
        if (Members.Count == 0)
        {
            var members = await api.GetMembersAsync(pageSize: 100);
            foreach (var member in members.Items)
                Members.Add(member);
        }
        if (MoneyAccounts.Count == 0)
        {
            var accounts = await api.GetAccountsAsync(pageSize: 100);
            foreach (var account in accounts.Items.Where(a => !a.IsGroup && a.IsActive && a.CashBankKind != Shared.Enums.CashBankKind.None))
                MoneyAccounts.Add(account);
            MoneyAccount = MoneyAccounts.FirstOrDefault();
        }
    });

    [RelayCommand]
    private Task SaveAsync() => RunAsync(async () =>
    {
        if (SelectedMember is null)
        {
            await ToastAsync("Member বেছে নিন।");
            return;
        }
        if (MoneyAccount is null)
        {
            await ToastAsync("Cash/Bank account বেছে নিন।");
            return;
        }
        if (!decimal.TryParse(Amount, out var amount) || amount <= 0)
        {
            await ToastAsync("টাকার পরিমাণ লিখুন।");
            return;
        }

        var request = new LoanTransactionRequest
        {
            MemberId = SelectedMember.Id,
            Amount = amount,
            MoneyAccountId = MoneyAccount.Id,
            Date = DateOnly.FromDateTime(Date),
            Narration = string.IsNullOrWhiteSpace(Narration) ? null : Narration.Trim()
        };

        var summary = IsGive
            ? await api.GiveLoanAsync(request)
            : await api.RepayLoanAsync(request);

        await ToastAsync($"সেভ হয়েছে ✓ — {summary.MemberName}-এর বাকি: {summary.Outstanding:N2}");
        await Shell.Current.GoToAsync("..");
    });
}
