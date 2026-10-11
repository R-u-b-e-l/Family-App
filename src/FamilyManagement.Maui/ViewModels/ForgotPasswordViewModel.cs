using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FamilyManagement.Maui.Services;
using FamilyManagement.Shared.Dtos;

namespace FamilyManagement.Maui.ViewModels;

/// <summary>Three steps on one page: identifier → OTP → new password.</summary>
public partial class ForgotPasswordViewModel(ApiClient api) : BaseViewModel
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdentifierStep), nameof(IsOtpStep), nameof(IsResetStep))]
    private int _step;

    [ObservableProperty] private string _identifier = string.Empty;
    [ObservableProperty] private string _otp = string.Empty;
    [ObservableProperty] private string _newPassword = string.Empty;
    [ObservableProperty] private string _confirmPassword = string.Empty;

    private string _resetToken = string.Empty;

    public bool IsIdentifierStep => Step == 0;
    public bool IsOtpStep => Step == 1;
    public bool IsResetStep => Step == 2;

    [RelayCommand]
    private Task SendOtpAsync() => RunAsync(async () =>
    {
        if (string.IsNullOrWhiteSpace(Identifier))
        {
            await ToastAsync("Enter your email or mobile number.");
            return;
        }
        await api.ForgotPasswordAsync(Identifier.Trim());
        await ToastAsync("If the account exists, a 6-digit code was sent.");
        Step = 1;
    });

    [RelayCommand]
    private Task VerifyOtpAsync() => RunAsync(async () =>
    {
        var result = await api.VerifyOtpAsync(new VerifyOtpRequest { Identifier = Identifier.Trim(), Otp = Otp.Trim() });
        _resetToken = result.ResetToken;
        Step = 2;
    });

    [RelayCommand]
    private Task ResetPasswordAsync() => RunAsync(async () =>
    {
        if (NewPassword != ConfirmPassword)
        {
            await ToastAsync("Passwords do not match.");
            return;
        }
        await api.ResetPasswordAsync(new ResetPasswordRequest
        {
            ResetToken = _resetToken,
            NewPassword = NewPassword,
            ConfirmPassword = ConfirmPassword
        });
        await ToastAsync("Password reset. Sign in with your new password.");
        await Shell.Current.GoToAsync("..");
    });

    [RelayCommand]
    private Task ResendAsync() => SendOtpAsync();
}
