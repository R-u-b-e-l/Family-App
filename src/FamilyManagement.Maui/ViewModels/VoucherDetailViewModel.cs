using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FamilyManagement.Maui.Services;
using FamilyManagement.Shared.Authorization;
using FamilyManagement.Shared.Dtos;
using FamilyManagement.Shared.Enums;

namespace FamilyManagement.Maui.ViewModels;

[QueryProperty(nameof(VoucherId), "id")]
public partial class VoucherDetailViewModel(ApiClient api, Session session) : BaseViewModel
{
    [ObservableProperty] private string _voucherId = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanPost), nameof(CanCancel), nameof(CanReverse), nameof(CanDelete))]
    private VoucherDto? _voucher;

    public bool CanPost => Voucher?.Status == VoucherStatus.Draft && session.Can(PermissionCodes.VoucherPost);
    public bool CanCancel => Voucher?.Status == VoucherStatus.Posted && session.Can(PermissionCodes.VoucherApprove);
    public bool CanReverse => Voucher?.Status == VoucherStatus.Posted && session.Can(PermissionCodes.VoucherApprove);
    public bool CanDelete => Voucher?.Status == VoucherStatus.Draft && session.Can(PermissionCodes.VoucherDelete);

    [RelayCommand]
    private Task LoadAsync() => RunAsync(async () =>
    {
        if (Guid.TryParse(VoucherId, out var id))
            Voucher = await api.GetVoucherAsync(id);
    });

    [RelayCommand]
    private Task PostAsync() => RunAsync(async () =>
    {
        if (Voucher is null) return;
        var confirmed = await Shell.Current.DisplayAlert("Post voucher",
            "Posting is final — the voucher hits the books and can only be reversed, not edited. Continue?",
            "Post", "Cancel");
        if (!confirmed) return;

        Voucher = await api.PostVoucherAsync(Voucher.Id);
        await ToastAsync($"Posted {Voucher.VoucherNumber}");
    });

    [RelayCommand]
    private Task CancelVoucherAsync() => RunAsync(async () =>
    {
        if (Voucher is null) return;
        var confirmed = await Shell.Current.DisplayAlert("Cancel voucher",
            "The voucher will be excluded from all reports. Continue?", "Cancel Voucher", "Keep");
        if (!confirmed) return;

        Voucher = await api.CancelVoucherAsync(Voucher.Id);
        await ToastAsync("Voucher cancelled");
    });

    [RelayCommand]
    private Task ReverseAsync() => RunAsync(async () =>
    {
        if (Voucher is null) return;
        var confirmed = await Shell.Current.DisplayAlert("Reverse voucher",
            "A mirror voucher will be posted today to undo this one. Continue?", "Reverse", "Keep");
        if (!confirmed) return;

        var reversal = await api.ReverseVoucherAsync(Voucher.Id);
        await ToastAsync($"Reversed by {reversal.VoucherNumber}");
        await LoadAsync();
    });

    [RelayCommand]
    private Task DeleteAsync() => RunAsync(async () =>
    {
        if (Voucher is null) return;
        var confirmed = await Shell.Current.DisplayAlert("Delete draft",
            "Delete this draft voucher permanently?", "Delete", "Keep");
        if (!confirmed) return;

        await api.DeleteVoucherDraftAsync(Voucher.Id);
        await ToastAsync("Draft deleted");
        await Shell.Current.GoToAsync("..");
    });
}
