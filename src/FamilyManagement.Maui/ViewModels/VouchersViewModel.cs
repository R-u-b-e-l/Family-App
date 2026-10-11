using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FamilyManagement.Maui.Services;
using FamilyManagement.Shared.Authorization;
using FamilyManagement.Shared.Dtos;
using FamilyManagement.Shared.Enums;

namespace FamilyManagement.Maui.ViewModels;

public partial class VouchersViewModel(ApiClient api, Session session) : BaseViewModel
{
    private const int PageSize = 20;
    private int _page = 1;
    private int _totalPages = 1;
    private CancellationTokenSource? _searchDebounce;

    [ObservableProperty] private string _search = string.Empty;
    [ObservableProperty] private int _typeFilterIndex;   // 0=All, 1=JV, 2=PV, 3=RV, 4=CV
    [ObservableProperty] private int _statusFilterIndex; // 0=All, 1=Draft, 2=Posted, 3=Cancelled, 4=Reversed

    public ObservableCollection<VoucherListItemDto> Vouchers { get; } = [];
    public bool CanCreate => session.Can(PermissionCodes.VoucherCreate);

    [RelayCommand]
    private Task LoadAsync() => RunAsync(async () =>
    {
        _page = 1;
        var result = await api.GetVouchersAsync(BuildFilter());
        _totalPages = result.TotalPages;
        Vouchers.Clear();
        foreach (var voucher in result.Items)
            Vouchers.Add(voucher);
    });

    [RelayCommand]
    private async Task LoadMoreAsync()
    {
        if (IsBusy || _page >= _totalPages)
            return;
        await RunAsync(async () =>
        {
            _page++;
            var filter = BuildFilter();
            filter.Page = _page;
            var result = await api.GetVouchersAsync(filter);
            foreach (var voucher in result.Items)
                Vouchers.Add(voucher);
        });
    }

    private VoucherFilterRequest BuildFilter() => new()
    {
        Page = _page,
        PageSize = PageSize,
        Search = string.IsNullOrWhiteSpace(Search) ? null : Search.Trim(),
        VoucherType = TypeFilterIndex switch
        {
            1 => VoucherType.JV, 2 => VoucherType.PV, 3 => VoucherType.RV, 4 => VoucherType.CV, _ => null
        },
        Status = StatusFilterIndex switch
        {
            1 => VoucherStatus.Draft, 2 => VoucherStatus.Posted,
            3 => VoucherStatus.Cancelled, 4 => VoucherStatus.Reversed, _ => null
        }
    };

    partial void OnSearchChanged(string value)
    {
        // Debounce server-side search.
        _searchDebounce?.Cancel();
        var cts = _searchDebounce = new CancellationTokenSource();
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(400, cts.Token);
                await MainThread.InvokeOnMainThreadAsync(() => LoadCommand.ExecuteAsync(null));
            }
            catch (TaskCanceledException) { }
        });
    }

    partial void OnTypeFilterIndexChanged(int value) => LoadCommand.Execute(null);
    partial void OnStatusFilterIndexChanged(int value) => LoadCommand.Execute(null);

    [RelayCommand]
    private async Task OpenVoucherAsync(VoucherListItemDto? voucher)
    {
        if (voucher is not null)
            await Shell.Current.GoToAsync($"voucher-detail?id={voucher.Id}");
    }

    [RelayCommand]
    private Task NewVoucherAsync() => Shell.Current.GoToAsync("voucher-entry");
}
