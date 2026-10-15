using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FamilyManagement.Maui.Helpers;
using FamilyManagement.Maui.Services;
using FamilyManagement.Maui.Views;
using FamilyManagement.Shared.Dtos;

namespace FamilyManagement.Maui.ViewModels;

public partial class RegisterViewModel(ApiClient api, Session session) : BaseViewModel
{
    [ObservableProperty] private string _familyName = string.Empty;
    [ObservableProperty] private string _adminFullName = string.Empty;
    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _mobile = string.Empty;
    [ObservableProperty] private string _userName = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string _confirmPassword = string.Empty;

    [RelayCommand]
    private Task RegisterAsync() => RunAsync(async () =>
    {
        if (Password != ConfirmPassword)
        {
            await ToastAsync("Passwords do not match.");
            return;
        }

        await api.RegisterFamilyAsync(new RegisterFamilyRequest
        {
            FamilyName = FamilyName.Trim(),
            AdminFullName = AdminFullName.Trim(),
            Email = Email.Trim(),
            Mobile = Mobile.Trim(),
            UserName = UserName.Trim(),
            Password = Password,
            ConfirmPassword = ConfirmPassword
        });

        MoneyConverter.Symbol = SplashPage.CurrencySymbol(session.Currency);
        await ToastAsync("Family created — welcome!");
        AppShell.ApplySession(session);
        await Shell.Current.GoToAsync("//main");
    });

    [RelayCommand]
    private Task BackToLoginAsync() => Shell.Current.GoToAsync("..");
}
