using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FamilyManagement.Maui.Services;
using FamilyManagement.Shared.Dtos;

namespace FamilyManagement.Maui.ViewModels;

public partial class ProfileViewModel(ApiClient api, Session session) : BaseViewModel
{
    [ObservableProperty] private string _fullName = string.Empty;
    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _mobile = string.Empty;
    [ObservableProperty] private string _address = string.Empty;
    [ObservableProperty] private string _userName = string.Empty;
    [ObservableProperty] private string _familyName = string.Empty;
    [ObservableProperty] private string _roleText = string.Empty;

    [ObservableProperty] private string _currentPassword = string.Empty;
    [ObservableProperty] private string _newPassword = string.Empty;
    [ObservableProperty] private string _confirmPassword = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPicture))]
    private string? _pictureUrl;

    public bool HasPicture => !string.IsNullOrEmpty(PictureUrl);

    [RelayCommand]
    private Task LoadAsync() => RunAsync(async () =>
    {
        var profile = await api.GetProfileAsync();
        session.UpdateUser(profile);
        FullName = profile.FullName;
        Email = profile.Email ?? "";
        Mobile = profile.Mobile ?? "";
        Address = profile.Address ?? "";
        UserName = profile.UserName;
        FamilyName = profile.FamilyName;
        RoleText = string.Join(", ", profile.Roles);
        PictureUrl = BuildPictureUrl(profile.ProfilePictureUrl);
    });

    [RelayCommand]
    private Task ChangePictureAsync() => RunAsync(async () =>
    {
        var photo = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
        {
            Title = "Choose your profile picture"
        });
        if (photo is null)
            return;

        var contentType = Path.GetExtension(photo.FileName).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "image/jpeg"
        };

        await using var stream = await photo.OpenReadAsync();
        var updated = await api.UploadProfilePictureAsync(stream, photo.FileName, contentType);
        PictureUrl = BuildPictureUrl(updated.ProfilePictureUrl);
        await ToastAsync("Profile picture updated ✓");
    });

    /// <summary>Relative path from the API → absolute URL, cache-busted so the new photo shows immediately.</summary>
    public static string? BuildPictureUrl(string? relativePath) =>
        string.IsNullOrEmpty(relativePath) ? null : $"{ApiClient.ServerUrl}{relativePath}?t={DateTime.UtcNow.Ticks}";

    [RelayCommand]
    private Task SaveAsync() => RunAsync(async () =>
    {
        var updated = await api.UpdateProfileAsync(new UpdateProfileRequest
        {
            FullName = FullName.Trim(),
            Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim(),
            Mobile = string.IsNullOrWhiteSpace(Mobile) ? null : Mobile.Trim(),
            Address = string.IsNullOrWhiteSpace(Address) ? null : Address.Trim()
        });
        session.UpdateUser(updated);
        await ToastAsync("Profile updated");
    });

    [RelayCommand]
    private Task ChangePasswordAsync() => RunAsync(async () =>
    {
        if (NewPassword != ConfirmPassword)
        {
            await ToastAsync("New passwords do not match.");
            return;
        }
        await api.ChangePasswordAsync(new ChangePasswordRequest
        {
            CurrentPassword = CurrentPassword,
            NewPassword = NewPassword,
            ConfirmPassword = ConfirmPassword
        });
        CurrentPassword = NewPassword = ConfirmPassword = string.Empty;
        await ToastAsync("Password changed. Other devices were signed out.");
    });
}

public partial class FamilySettingsViewModel(ApiClient api, Session session) : BaseViewModel
{
    [ObservableProperty] private string _currency = "BDT";
    [ObservableProperty] private string _dateFormat = "dd-MMM-yyyy";
    [ObservableProperty] private string _timezone = "Asia/Dhaka";
    [ObservableProperty] private int _financialYearStartMonth = 1;
    [ObservableProperty] private string _address = string.Empty;
    [ObservableProperty] private string _contactPhone = string.Empty;
    [ObservableProperty] private string _contactEmail = string.Empty;

    [RelayCommand]
    private Task LoadAsync() => RunAsync(async () =>
    {
        var settings = await api.GetFamilySettingsAsync();
        Currency = settings.Currency;
        DateFormat = settings.DateFormat;
        Timezone = settings.Timezone;
        FinancialYearStartMonth = settings.FinancialYearStartMonth;
        Address = settings.Address ?? "";
        ContactPhone = settings.ContactPhone ?? "";
        ContactEmail = settings.ContactEmail ?? "";
    });

    [RelayCommand]
    private Task SaveAsync() => RunAsync(async () =>
    {
        var updated = await api.UpdateFamilySettingsAsync(new UpdateFamilySettingsRequest
        {
            Currency = Currency.Trim().ToUpperInvariant(),
            DateFormat = DateFormat.Trim(),
            Timezone = Timezone.Trim(),
            FinancialYearStartMonth = Math.Clamp(FinancialYearStartMonth, 1, 12),
            Address = string.IsNullOrWhiteSpace(Address) ? null : Address.Trim(),
            ContactPhone = string.IsNullOrWhiteSpace(ContactPhone) ? null : ContactPhone.Trim(),
            ContactEmail = string.IsNullOrWhiteSpace(ContactEmail) ? null : ContactEmail.Trim()
        });
        session.Currency = updated.Currency;
        Helpers.MoneyConverter.Symbol = Views.SplashPage.CurrencySymbol(updated.Currency);
        await ToastAsync("Settings saved");
    });
}

public partial class NotificationsViewModel(ApiClient api) : BaseViewModel
{
    public ObservableCollection<NotificationDto> Notifications { get; } = [];

    [RelayCommand]
    private Task LoadAsync() => RunAsync(async () =>
    {
        var result = await api.GetNotificationsAsync();
        Notifications.Clear();
        foreach (var notification in result.Items)
            Notifications.Add(notification);
    });

    [RelayCommand]
    private Task MarkAllReadAsync() => RunAsync(async () =>
    {
        await api.MarkAllNotificationsReadAsync();
        await LoadAsync();
    });
}
