using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FamilyManagement.Maui.Services;
using FamilyManagement.Shared.Dtos;
using FamilyManagement.Shared.Enums;

namespace FamilyManagement.Maui.ViewModels;

public partial class VoucherLineVm : ObservableObject
{
    [ObservableProperty] private AccountDto? _account;
    [ObservableProperty] private string _debit = string.Empty;
    [ObservableProperty] private string _credit = string.Empty;
    [ObservableProperty] private string _narration = string.Empty;

    public decimal DebitValue => decimal.TryParse(Debit, out var d) ? d : 0;
    public decimal CreditValue => decimal.TryParse(Credit, out var c) ? c : 0;

    public event Action? AmountChanged;
    partial void OnDebitChanged(string value) => AmountChanged?.Invoke();
    partial void OnCreditChanged(string value) => AmountChanged?.Invoke();
}

public partial class VoucherEntryViewModel(ApiClient api) : BaseViewModel
{
    [ObservableProperty] private int _typeIndex = 1; // default: Payment
    [ObservableProperty] private DateTime _voucherDate = DateTime.Today;
    [ObservableProperty] private string _narration = string.Empty;
    [ObservableProperty] private decimal _totalDebit;
    [ObservableProperty] private decimal _totalCredit;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BalanceText))]
    private bool _isBalanced;

    public ObservableCollection<VoucherLineVm> Lines { get; } = [];
    public ObservableCollection<AccountDto> Accounts { get; } = [];

    public string BalanceText => IsBalanced ? "Balanced ✓" : "Not balanced";

    private static readonly VoucherType[] TypeMap = [VoucherType.JV, VoucherType.PV, VoucherType.RV, VoucherType.CV];

    [RelayCommand]
    private Task LoadAsync() => RunAsync(async () =>
    {
        if (Accounts.Count == 0)
        {
            var result = await api.GetAccountsAsync(pageSize: 100);
            foreach (var account in result.Items.Where(a => !a.IsGroup && a.IsActive).OrderBy(a => a.Code))
                Accounts.Add(account);
        }
        if (Lines.Count == 0)
        {
            AddLine();
            AddLine();
        }
    });

    [RelayCommand]
    private void AddLine()
    {
        var line = new VoucherLineVm();
        line.AmountChanged += RecomputeTotals;
        Lines.Add(line);
    }

    [RelayCommand]
    private void RemoveLine(VoucherLineVm? line)
    {
        if (line is null || Lines.Count <= 2)
            return;
        line.AmountChanged -= RecomputeTotals;
        Lines.Remove(line);
        RecomputeTotals();
    }

    private void RecomputeTotals()
    {
        TotalDebit = Lines.Sum(l => l.DebitValue);
        TotalCredit = Lines.Sum(l => l.CreditValue);
        IsBalanced = TotalDebit == TotalCredit && TotalDebit > 0;
    }

    [RelayCommand]
    private Task SaveDraftAsync() => SaveAsync(post: false);

    [RelayCommand]
    private Task SaveAndPostAsync() => SaveAsync(post: true);

    private Task SaveAsync(bool post) => RunAsync(async () =>
    {
        RecomputeTotals();
        if (Lines.Any(l => l.Account is null))
        {
            await ToastAsync("Choose an account for every line.");
            return;
        }
        if (!IsBalanced)
        {
            await ToastAsync($"Debit ({TotalDebit:N2}) must equal credit ({TotalCredit:N2}).");
            return;
        }

        var request = new SaveVoucherRequest
        {
            VoucherType = TypeMap[TypeIndex],
            VoucherDate = DateOnly.FromDateTime(VoucherDate),
            Narration = string.IsNullOrWhiteSpace(Narration) ? null : Narration.Trim(),
            Lines = Lines.Select(l => new SaveVoucherLineRequest
            {
                AccountId = l.Account!.Id,
                Debit = l.DebitValue,
                Credit = l.CreditValue,
                Narration = string.IsNullOrWhiteSpace(l.Narration) ? null : l.Narration.Trim()
            }).ToList()
        };

        var draft = await api.CreateVoucherDraftAsync(request);
        if (post)
        {
            var posted = await api.PostVoucherAsync(draft.Id);
            await ToastAsync($"Posted {posted.VoucherNumber}");
        }
        else
        {
            await ToastAsync("Draft saved");
        }
        await Shell.Current.GoToAsync("..");
    });
}
