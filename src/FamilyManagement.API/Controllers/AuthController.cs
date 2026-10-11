using FamilyManagement.Application.Common.Interfaces;
using FamilyManagement.Shared.Common;
using FamilyManagement.Shared.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FamilyManagement.API.Controllers;

[EnableRateLimiting("auth")]
public class AuthController(IAuthService authService) : ApiControllerBase
{
    /// <summary>Registers a new family; the first user becomes the family admin.</summary>
    [HttpPost("register-family")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> RegisterFamily(RegisterFamilyRequest request, CancellationToken ct) =>
        Ok(Success(await authService.RegisterFamilyAsync(request, ct), "Family registered successfully"));

    /// <summary>Signs in with username, email or mobile.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login(LoginRequest request, CancellationToken ct) =>
        Ok(Success(await authService.LoginAsync(request, ct), "Signed in"));

    /// <summary>Exchanges a refresh token for a new token pair (rotation).</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Refresh(RefreshTokenRequest request, CancellationToken ct) =>
        Ok(Success(await authService.RefreshAsync(request.RefreshToken, ct)));

    /// <summary>Revokes the given refresh token.</summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse>> Logout(LogoutRequest request, CancellationToken ct)
    {
        await authService.LogoutAsync(request.RefreshToken, ct);
        return Ok(Success("Signed out"));
    }

    /// <summary>Sends a password-reset OTP. Always succeeds to avoid user enumeration.</summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse>> ForgotPassword(ForgotPasswordRequest request, CancellationToken ct)
    {
        await authService.ForgotPasswordAsync(request.Identifier, ct);
        return Ok(Success("If the account exists, a reset code has been sent."));
    }

    /// <summary>Verifies the OTP and returns a short-lived reset token.</summary>
    [HttpPost("verify-otp")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<VerifyOtpResponse>>> VerifyOtp(VerifyOtpRequest request, CancellationToken ct) =>
        Ok(Success(await authService.VerifyOtpAsync(request, ct), "Code verified"));

    /// <summary>Sets a new password using the reset token from OTP verification.</summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse>> ResetPassword(ResetPasswordRequest request, CancellationToken ct)
    {
        await authService.ResetPasswordAsync(request, ct);
        return Ok(Success("Password has been reset. Sign in with the new password."));
    }

    /// <summary>Changes the signed-in user's password.</summary>
    [HttpPost("change-password")]
    public async Task<ActionResult<ApiResponse>> ChangePassword(ChangePasswordRequest request, CancellationToken ct)
    {
        await authService.ChangePasswordAsync(request, ct);
        return Ok(Success("Password changed. Sign in again on other devices."));
    }
}
