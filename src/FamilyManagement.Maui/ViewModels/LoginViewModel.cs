using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FamilyManagement.Maui.Helpers;
using FamilyManagement.Maui.Services;
using FamilyManagement.Maui.Views;
using FamilyManagement.Shared.Dtos;

namespace FamilyManagement.Maui.ViewModels;

public partial class LoginViewModel(ApiClient api, Session session) : BaseViewModel
{
    [ObservableProperty]
    private string _identifier = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _rememberMe = true;

    [ObservableProperty]
    private string _serverUrl = ApiClient.ServerUrl;

    [RelayCommand]
    private Task LoginAsync() => RunAsync(async () =>
    {
        if (!ApplyServerUrl())
        {
            // this is local ip
            await ToastAsync("Enter a valid server address, e.g. http://192.168.211.50:5005");
            return;
        }
        if (string.IsNullOrWhiteSpace(Identifier) || string.IsNullOrWhiteSpace(Password))
        {
            await ToastAsync("Enter your username/email and password.");
            return;
        }

        var auth = await api.LoginAsync(new LoginRequest
        {
            Identifier = Identifier.Trim(),
            Password = Password,
            RememberMe = RememberMe
        });

        MoneyConverter.Symbol = SplashPage.CurrencySymbol(session.Currency);
        Password = string.Empty;
        AppShell.ApplySession(session);
        await Shell.Current.GoToAsync("//main");
    });

    [RelayCommand]
    private async Task GoToRegisterAsync()
    {
        ApplyServerUrl();
        await Shell.Current.GoToAsync("register");
    }

    [RelayCommand]
    private async Task GoToForgotPasswordAsync()
    {
        ApplyServerUrl();
        await Shell.Current.GoToAsync("forgot-password");
    }

    /// <summary>Saves the server address so registration/OTP flows use it too. False when invalid.</summary>
    private bool ApplyServerUrl()
    {
        var url = ServerUrl.Trim();
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            return false;
        ApiClient.ServerUrl = url;
        return true;
    }
}
