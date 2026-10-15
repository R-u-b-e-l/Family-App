using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FamilyManagement.Maui.Services;
using FamilyManagement.Shared.Dtos;
using FamilyManagement.Shared.Enums;

namespace FamilyManagement.Maui.ViewModels;

/// <summary>
/// Beginner-friendly voucher entry: pick what happened, a category and one amount —
/// the balanced double-entry voucher (debit/credit) is built automatically.
/// </summary>
public partial class QuickVoucherViewModel(ApiClient api, Session session) : BaseViewModel
{
    [ObservableProperty] private UserDto? _selectedMember;
    public ObservableCollection<UserDto> Members { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsExpense), nameof(IsIncome), nameof(IsTransfer),
        nameof(CategoryLabel), nameof(MoneyLabel), nameof(ExplainText))]
    private int _modeIndex; // 0 = Expense, 1 = Income, 2 = Transfer

    [ObservableProperty] private AccountDto? _category;
    [ObservableProperty] private AccountDto? _moneyAccount;
    [ObservableProperty] private AccountDto? _transferFrom;
    [ObservableProperty] private AccountDto? _transferTo;
    [ObservableProperty] private string _amount = string.Empty;
    [ObservableProperty] private DateTime _date = DateTime.Today;
    [ObservableProperty] private string _narration = string.Empty;

    public ObservableCollection<AccountDto> Categories { get; } = [];
    public ObservableCollection<AccountDto> MoneyAccounts { get; } = [];

    private List<AccountDto> _expenseHeads = [];
    private List<AccountDto> _incomeHeads = [];

    public bool IsExpense => ModeIndex == 0;
    public bool IsIncome => ModeIndex == 1;
    public bool IsTransfer => ModeIndex == 2;

    public string CategoryLabel => IsExpense ? "কিসের খরচ? (Category)" : "কিসের আয়? (Source)";
    public string MoneyLabel => IsExpense ? "কোথা থেকে টাকা গেল?" : "টাকা কোথায় রাখলেন?";

    public string ExplainText => ModeIndex switch
    {
        0 => "যেমন: বাজার ৫০০ টাকা → Category: Food Expense, টাকা গেল: Cash in Hand",
        1 => "যেমন: রুবেল বেতন দিল ৪০০০ টাকা → Source: Salary Income, রাখলেন: Cash in Hand",
        _ => "যেমন: ব্যাংক থেকে ২০০০ টাকা তুললেন → From: Bank Account, To: Cash in Hand",
    };

    [RelayCommand]
    private void SetMode(string index)
    {
        ModeIndex = int.Parse(index);
        RefreshCategories();
    }

    [RelayCommand]
    private Task LoadAsync() => RunAsync(async () =>
    {
        if (_expenseHeads.Count > 0)
            return;

        var result = await api.GetAccountsAsync(pageSize: 100);
        var leaves = result.Items.Where(a => !a.IsGroup && a.IsActive).OrderBy(a => a.Code).ToList();

        _expenseHeads = leaves.Where(a => a.AccountType == AccountType.Expense).ToList();
        _incomeHeads = leaves.Where(a => a.AccountType == AccountType.Income).ToList();

        MoneyAccounts.Clear();
        foreach (var account in leaves.Where(a => a.CashBankKind != CashBankKind.None))
            MoneyAccounts.Add(account);
        MoneyAccount = MoneyAccounts.FirstOrDefault();

        try
        {
            var members = await api.GetMembersAsync(pageSize: 100);
            Members.Clear();
            foreach (var member in members.Items)
                Members.Add(member);
            SelectedMember = Members.FirstOrDefault(m => m.Id == session.CurrentUser?.Id);
        }
        catch
        {
            // Member tagging is optional — the entry still works without the list.
        }

        RefreshCategories();
    });

    private void RefreshCategories()
    {
        Categories.Clear();
        foreach (var account in IsIncome ? _incomeHeads : _expenseHeads)
            Categories.Add(account);
        Category = null;
    }

    [RelayCommand]
    private Task SaveAsync() => RunAsync(async () =>
    {
        if (!decimal.TryParse(Amount, out var amount) || amount <= 0)
        {
            await ToastAsync("টাকার পরিমাণ লিখুন (০-এর বেশি)।");
            return;
        }

        SaveVoucherRequest request;
        if (IsTransfer)
        {
            if (TransferFrom is null || TransferTo is null)
            {
                await ToastAsync("কোথা থেকে এবং কোথায় — দুটোই বেছে নিন।");
                return;
            }
            if (TransferFrom.Id == TransferTo.Id)
            {
                await ToastAsync("From আর To একই account হতে পারে না।");
                return;
            }
            request = new SaveVoucherRequest
            {
                VoucherType = VoucherType.CV,
                VoucherDate = DateOnly.FromDateTime(Date),
                MemberId = SelectedMember?.Id,
                Narration = DefaultNarration($"Transfer: {TransferFrom.Name} → {TransferTo.Name}"),
                Lines =
                [
                    new SaveVoucherLineRequest { AccountId = TransferTo.Id, Debit = amount, Credit = 0 },
                    new SaveVoucherLineRequest { AccountId = TransferFrom.Id, Debit = 0, Credit = amount }
                ]
            };
        }
        else
        {
            if (Category is null)
            {
                await ToastAsync(IsExpense ? "কিসের খরচ সেটা বেছে নিন।" : "আয়ের উৎস বেছে নিন।");
                return;
            }
            if (MoneyAccount is null)
            {
                await ToastAsync("Cash না Bank — টাকার জায়গাটা বেছে নিন।");
                return;
            }

            request = IsExpense
                ? new SaveVoucherRequest
                {
                    VoucherType = VoucherType.PV,
                    VoucherDate = DateOnly.FromDateTime(Date),
                    MemberId = SelectedMember?.Id,
                    Narration = DefaultNarration(Category.Name),
                    Lines =
                    [
                        new SaveVoucherLineRequest { AccountId = Category.Id, Debit = amount, Credit = 0 },
                        new SaveVoucherLineRequest { AccountId = MoneyAccount.Id, Debit = 0, Credit = amount }
                    ]
                }
                : new SaveVoucherRequest
                {
                    VoucherType = VoucherType.RV,
                    VoucherDate = DateOnly.FromDateTime(Date),
                    MemberId = SelectedMember?.Id,
                    Narration = DefaultNarration(Category.Name),
                    Lines =
                    [
                        new SaveVoucherLineRequest { AccountId = MoneyAccount.Id, Debit = amount, Credit = 0 },
                        new SaveVoucherLineRequest { AccountId = Category.Id, Debit = 0, Credit = amount }
                    ]
                };
        }

        var draft = await api.CreateVoucherDraftAsync(request);
        var posted = await api.PostVoucherAsync(draft.Id);

        await ToastAsync($"সেভ হয়েছে ✓  ({posted.VoucherNumber})");

        // Reset for the next entry.
        Amount = string.Empty;
        Narration = string.Empty;
        Category = null;
    });

    [RelayCommand]
    private Task OpenAdvancedAsync() => Shell.Current.GoToAsync("voucher-entry");

    private string? DefaultNarration(string fallback) =>
        string.IsNullOrWhiteSpace(Narration) ? fallback : Narration.Trim();
}
