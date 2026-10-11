using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FamilyManagement.Maui.Services;
using FamilyManagement.Shared.Authorization;

namespace FamilyManagement.Maui.ViewModels;

public partial class MoreViewModel(ApiClient api, Session session, LocalCache cache) : BaseViewModel
{
    [ObservableProperty] private string _fullName = "";
    [ObservableProperty] private string _familyName = "";
    [ObservableProperty] private string _roleText = "";
    [ObservableProperty] private int _themeIndex;
    [ObservableProperty] private string _serverUrl = ApiClient.ServerUrl;

    public bool CanManageMembers => session.Can(PermissionCodes.MemberView);
    public bool CanEditSettings => session.Can(PermissionCodes.SettingsView);

    [RelayCommand]
    private void Load()
    {
        FullName = session.CurrentUser?.FullName ?? "";
        FamilyName = session.CurrentUser?.FamilyName ?? "";
        RoleText = string.Join(", ", session.CurrentUser?.Roles ?? []);
        ThemeIndex = Preferences.Default.Get("fm_theme", "system") switch
        {
            "light" => 1, "dark" => 2, _ => 0
        };
        ServerUrl = ApiClient.ServerUrl;
        OnPropertyChanged(nameof(CanManageMembers));
        OnPropertyChanged(nameof(CanEditSettings));
    }

    partial void OnThemeIndexChanged(int value)
    {
        var (name, theme) = value switch
        {
            1 => ("light", AppTheme.Light),
            2 => ("dark", AppTheme.Dark),
            _ => ("system", AppTheme.Unspecified)
        };
        Preferences.Default.Set("fm_theme", name);
        if (Application.Current is not null)
            Application.Current.UserAppTheme = theme;
    }

    [RelayCommand]
    private void SaveServerUrl()
    {
        if (Uri.TryCreate(ServerUrl.Trim(), UriKind.Absolute, out var uri))
        {
            ApiClient.ServerUrl = uri.ToString().TrimEnd('/');
            _ = ToastAsync("Server address saved");
        }
        else
        {
            _ = ToastAsync("Enter a valid URL, e.g. http://10.0.2.2:5005");
        }
    }

    [RelayCommand]
    private Task OpenProfileAsync() => Shell.Current.GoToAsync("profile");

    [RelayCommand]
    private Task OpenMembersAsync() => Shell.Current.GoToAsync("members");

    [RelayCommand]
    private Task OpenFamilySettingsAsync() => Shell.Current.GoToAsync("family-settings");

    [RelayCommand]
    private Task OpenNotificationsAsync() => Shell.Current.GoToAsync("notifications");

    [RelayCommand]
    private async Task SignOutAsync()
    {
        var confirmed = await Shell.Current.DisplayAlert("Sign out", "Sign out of Family Manager?", "Sign Out", "Stay");
        if (!confirmed)
            return;
        await api.LogoutAsync();
        await cache.ClearAsync();
        await Shell.Current.GoToAsync("//login");
    }
}
