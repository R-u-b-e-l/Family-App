using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FamilyManagement.Maui.Services;
using FamilyManagement.Shared.Dtos;
using FamilyManagement.Shared.Enums;

namespace FamilyManagement.Maui.ViewModels;

[QueryProperty(nameof(AccountId), "accountId")]
[QueryProperty(nameof(AccountName), "accountName")]
public partial class LedgerViewModel(ApiClient api) : BaseViewModel
{
    [ObservableProperty] private string _accountId = string.Empty;
    [ObservableProperty] private string _accountName = string.Empty;
    [ObservableProperty] private AccountDto? _selectedAccount;
    [ObservableProperty] private DateTime _dateFrom = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    [ObservableProperty] private DateTime _dateTo = DateTime.Today;
    [ObservableProperty] private LedgerReportDto? _report;

    public ObservableCollection<AccountDto> Accounts { get; } = [];
    public ObservableCollection<LedgerRowDto> Rows { get; } = [];

    private int _page = 1;
    private int _totalPages = 1;

    [RelayCommand]
    private Task InitAsync() => RunAsync(async () =>
    {
        if (Accounts.Count == 0)
        {
            var result = await api.GetAccountsAsync(pageSize: 100);
            foreach (var account in result.Items.Where(a => !a.IsGroup).OrderBy(a => a.Code))
                Accounts.Add(account);
        }
        if (Guid.TryParse(AccountId, out var preselected))
        {
            SelectedAccount = Accounts.FirstOrDefault(a => a.Id == preselected);
            if (SelectedAccount is not null)
                await LoadPageAsync(reset: true);
        }
    });

    [RelayCommand]
    private Task GenerateAsync() => RunAsync(() => LoadPageAsync(reset: true));

    [RelayCommand]
    private async Task LoadMoreAsync()
    {
        if (IsBusy || Report is null || _page >= _totalPages)
            return;
        await RunAsync(async () =>
        {
            _page++;
            await LoadPageAsync(reset: false);
        });
    }

    private async Task LoadPageAsync(bool reset)
    {
        if (SelectedAccount is null)
        {
            await ToastAsync("Choose an account first.");
            return;
        }
        if (reset)
        {
            _page = 1;
            Rows.Clear();
        }

        Report = await api.GetLedgerAsync(SelectedAccount.Id,
            DateOnly.FromDateTime(DateFrom), DateOnly.FromDateTime(DateTo), _page);
        _totalPages = Report.Rows.TotalPages;
        foreach (var row in Report.Rows.Items)
            Rows.Add(row);
    }

    [RelayCommand]
    private Task DownloadPdfAsync() => RunAsync(async () =>
    {
        if (SelectedAccount is null)
        {
            await ToastAsync("Choose an account first.");
            return;
        }
        var from = DateOnly.FromDateTime(DateFrom);
        var to = DateOnly.FromDateTime(DateTo);
        var path = await api.DownloadPdfAsync(
            $"/reports/ledger/pdf?accountId={SelectedAccount.Id}&dateFrom={from:yyyy-MM-dd}&dateTo={to:yyyy-MM-dd}",
            $"Ledger_{SelectedAccount.Code}_{from:yyyyMMdd}.pdf");
        await OpenPdfAsync(path, "Ledger Statement");
    });
}

[QueryProperty(nameof(Kind), "kind")]
public partial class BookViewModel(ApiClient api) : BaseViewModel
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PageTitle))]
    private string _kind = "cash";

    [ObservableProperty] private DateTime _dateFrom = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    [ObservableProperty] private DateTime _dateTo = DateTime.Today;
    [ObservableProperty] private BookReportDto? _report;

    public ObservableCollection<BookRowDto> Rows { get; } = [];
    public string PageTitle => Kind == "bank" ? "Bank Book" : "Cash Book";

    private int _page = 1;
    private int _totalPages = 1;

    [RelayCommand]
    private Task GenerateAsync() => RunAsync(async () =>
    {
        _page = 1;
        Rows.Clear();
        await LoadPageAsync();
    });

    [RelayCommand]
    private async Task LoadMoreAsync()
    {
        if (IsBusy || Report is null || _page >= _totalPages)
            return;
        await RunAsync(async () =>
        {
            _page++;
            await LoadPageAsync();
        });
    }

    private async Task LoadPageAsync()
    {
        var kind = Kind == "bank" ? CashBankKind.Bank : CashBankKind.Cash;
        Report = await api.GetBookAsync(kind, DateOnly.FromDateTime(DateFrom), DateOnly.FromDateTime(DateTo), _page);
        _totalPages = Report.Rows.TotalPages;
        foreach (var row in Report.Rows.Items)
            Rows.Add(row);
    }

    [RelayCommand]
    private Task DownloadPdfAsync() => RunAsync(async () =>
    {
        var endpoint = Kind == "bank" ? "bankbook" : "cashbook";
        var from = DateOnly.FromDateTime(DateFrom);
        var to = DateOnly.FromDateTime(DateTo);
        var path = await api.DownloadPdfAsync(
            $"/reports/{endpoint}/pdf?dateFrom={from:yyyy-MM-dd}&dateTo={to:yyyy-MM-dd}",
            $"{PageTitle.Replace(" ", "")}_{from:yyyyMMdd}.pdf");
        await OpenPdfAsync(path, PageTitle);
    });
}

public partial class TrialBalanceViewModel(ApiClient api) : BaseViewModel
{
    [ObservableProperty] private DateTime _asOf = DateTime.Today;
    [ObservableProperty] private TrialBalanceDto? _report;

    [RelayCommand]
    private Task GenerateAsync() => RunAsync(async () =>
    {
        Report = await api.GetTrialBalanceAsync(DateOnly.FromDateTime(AsOf));
    });

    [RelayCommand]
    private Task DownloadPdfAsync() => RunAsync(async () =>
    {
        var asOf = DateOnly.FromDateTime(AsOf);
        var path = await api.DownloadPdfAsync(
            $"/reports/trial-balance/pdf?asOf={asOf:yyyy-MM-dd}",
            $"TrialBalance_{asOf:yyyyMMdd}.pdf");
        await OpenPdfAsync(path, "Trial Balance");
    });
}

public partial class ProfitLossViewModel(ApiClient api) : BaseViewModel
{
    [ObservableProperty] private DateTime _dateFrom = new(DateTime.Today.Year, 1, 1);
    [ObservableProperty] private DateTime _dateTo = DateTime.Today;
    [ObservableProperty] private ProfitLossDto? _report;
    [ObservableProperty] private bool _isProfit;

    [RelayCommand]
    private Task GenerateAsync() => RunAsync(async () =>
    {
        Report = await api.GetProfitLossAsync(DateOnly.FromDateTime(DateFrom), DateOnly.FromDateTime(DateTo));
        IsProfit = Report.NetProfit >= 0;
    });

    [RelayCommand]
    private Task DownloadPdfAsync() => RunAsync(async () =>
    {
        var from = DateOnly.FromDateTime(DateFrom);
        var to = DateOnly.FromDateTime(DateTo);
        var path = await api.DownloadPdfAsync(
            $"/reports/profit-loss/pdf?dateFrom={from:yyyy-MM-dd}&dateTo={to:yyyy-MM-dd}",
            $"ProfitLoss_{from:yyyyMMdd}.pdf");
        await OpenPdfAsync(path, "Profit & Loss");
    });
}

public partial class BalanceSheetViewModel(ApiClient api) : BaseViewModel
{
    [ObservableProperty] private DateTime _asOf = DateTime.Today;
    [ObservableProperty] private BalanceSheetDto? _report;

    [RelayCommand]
    private Task GenerateAsync() => RunAsync(async () =>
    {
        Report = await api.GetBalanceSheetAsync(DateOnly.FromDateTime(AsOf));
    });

    [RelayCommand]
    private Task DownloadPdfAsync() => RunAsync(async () =>
    {
        var asOf = DateOnly.FromDateTime(AsOf);
        var path = await api.DownloadPdfAsync(
            $"/reports/balance-sheet/pdf?asOf={asOf:yyyy-MM-dd}",
            $"BalanceSheet_{asOf:yyyyMMdd}.pdf");
        await OpenPdfAsync(path, "Balance Sheet");
    });
}
