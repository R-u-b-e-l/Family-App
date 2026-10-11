using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FamilyManagement.Shared.Common;
using FamilyManagement.Shared.Dtos;
using FamilyManagement.Shared.Enums;

namespace FamilyManagement.Maui.Services;

public class ApiException(string message, IReadOnlyList<string> errors, HttpStatusCode statusCode) : Exception(message)
{
    public IReadOnlyList<string> Errors { get; } = errors;
    public HttpStatusCode StatusCode { get; } = statusCode;
    public string FullMessage => Errors.Count > 0 ? string.Join("\n", Errors) : Message;
}

public class ApiClient(TokenStore tokens, Session session)
{
    private const string ServerUrlKey = "fm_server_url";
    private const string ApiPrefix = "/api/v1";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(30) };
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    /// <summary>Raised when the session can no longer be refreshed; the app returns to login.</summary>
    public event Action? SessionExpired;

    public static string ServerUrl
    {
        get => Preferences.Default.Get(ServerUrlKey, DeviceInfo.Platform == DevicePlatform.Android
            ? "http://10.0.2.2:5005"   // Android emulator loopback to the host machine
            : "http://localhost:5005");
        set => Preferences.Default.Set(ServerUrlKey, value.TrimEnd('/'));
    }

    // ---- auth ---------------------------------------------------------------

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var auth = await PostAsync<LoginRequest, AuthResponse>("/auth/login", request, authorized: false, ct);
        await StartSessionAsync(auth);
        return auth;
    }

    public async Task<AuthResponse> RegisterFamilyAsync(RegisterFamilyRequest request, CancellationToken ct = default)
    {
        var auth = await PostAsync<RegisterFamilyRequest, AuthResponse>("/auth/register-family", request, authorized: false, ct);
        await StartSessionAsync(auth);
        return auth;
    }

    public Task ForgotPasswordAsync(string identifier, CancellationToken ct = default) =>
        PostAsync<ForgotPasswordRequest, object?>("/auth/forgot-password", new ForgotPasswordRequest { Identifier = identifier }, false, ct);

    public Task<VerifyOtpResponse> VerifyOtpAsync(VerifyOtpRequest request, CancellationToken ct = default) =>
        PostAsync<VerifyOtpRequest, VerifyOtpResponse>("/auth/verify-otp", request, false, ct);

    public Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default) =>
        PostAsync<ResetPasswordRequest, object?>("/auth/reset-password", request, false, ct);

    public Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default) =>
        PostAsync<ChangePasswordRequest, object?>("/auth/change-password", request, true, ct);

    public async Task LogoutAsync()
    {
        try
        {
            var refreshToken = await tokens.GetRefreshTokenAsync();
            if (refreshToken is not null)
                await PostAsync<LogoutRequest, object?>("/auth/logout", new LogoutRequest { RefreshToken = refreshToken }, false);
        }
        catch
        {
            // Local sign-out proceeds even if the server is unreachable.
        }
        finally
        {
            tokens.Clear();
            session.Clear();
        }
    }

    private async Task StartSessionAsync(AuthResponse auth)
    {
        await tokens.SaveAsync(auth.AccessToken, auth.RefreshToken);
        session.Start(auth);
    }

    // ---- typed endpoints ------------------------------------------------------

    public Task<DashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken ct = default) =>
        GetAsync<DashboardSummaryDto>("/dashboard/summary", ct);

    public Task<List<MonthlyAmountDto>> GetMonthlyIncomeExpenseAsync(int months = 6, CancellationToken ct = default) =>
        GetAsync<List<MonthlyAmountDto>>($"/dashboard/charts/income-expense?months={months}", ct);

    public Task<List<CategoryAmountDto>> GetExpenseByCategoryAsync(CancellationToken ct = default) =>
        GetAsync<List<CategoryAmountDto>>("/dashboard/charts/expense-by-category", ct);

    public Task<List<RecentTransactionDto>> GetRecentTransactionsAsync(int count = 10, CancellationToken ct = default) =>
        GetAsync<List<RecentTransactionDto>>($"/dashboard/recent-transactions?count={count}", ct);

    public Task<List<AccountTreeNodeDto>> GetAccountTreeAsync(CancellationToken ct = default) =>
        GetAsync<List<AccountTreeNodeDto>>("/accounts/tree", ct);

    public Task<PagedResult<AccountDto>> GetAccountsAsync(int page = 1, int pageSize = 100, string? search = null, CancellationToken ct = default) =>
        GetAsync<PagedResult<AccountDto>>($"/accounts?page={page}&pageSize={pageSize}&search={Uri.EscapeDataString(search ?? "")}", ct);

    public Task<AccountDto> CreateAccountAsync(CreateAccountRequest request, CancellationToken ct = default) =>
        PostAsync<CreateAccountRequest, AccountDto>("/accounts", request, true, ct);

    public Task<AccountBalanceDto> GetAccountBalanceAsync(Guid accountId, CancellationToken ct = default) =>
        GetAsync<AccountBalanceDto>($"/accounts/{accountId}/balance", ct);

    public Task<PagedResult<VoucherListItemDto>> GetVouchersAsync(VoucherFilterRequest filter, CancellationToken ct = default)
    {
        var query = $"/vouchers?page={filter.Page}&pageSize={filter.PageSize}";
        if (filter.VoucherType is { } type) query += $"&voucherType={(int)type}";
        if (filter.Status is { } status) query += $"&status={(int)status}";
        if (filter.DateFrom is { } from) query += $"&dateFrom={from:yyyy-MM-dd}";
        if (filter.DateTo is { } to) query += $"&dateTo={to:yyyy-MM-dd}";
        if (!string.IsNullOrWhiteSpace(filter.Search)) query += $"&search={Uri.EscapeDataString(filter.Search)}";
        return GetAsync<PagedResult<VoucherListItemDto>>(query, ct);
    }

    public Task<VoucherDto> GetVoucherAsync(Guid id, CancellationToken ct = default) =>
        GetAsync<VoucherDto>($"/vouchers/{id}", ct);

    public Task<VoucherDto> CreateVoucherDraftAsync(SaveVoucherRequest request, CancellationToken ct = default) =>
        PostAsync<SaveVoucherRequest, VoucherDto>("/vouchers", request, true, ct);

    public Task<VoucherDto> PostVoucherAsync(Guid id, CancellationToken ct = default) =>
        PostAsync<object?, VoucherDto>($"/vouchers/{id}/post", null, true, ct);

    public Task<VoucherDto> CancelVoucherAsync(Guid id, CancellationToken ct = default) =>
        PostAsync<object?, VoucherDto>($"/vouchers/{id}/cancel", null, true, ct);

    public Task<VoucherDto> ReverseVoucherAsync(Guid id, CancellationToken ct = default) =>
        PostAsync<object?, VoucherDto>($"/vouchers/{id}/reverse", null, true, ct);

    public Task DeleteVoucherDraftAsync(Guid id, CancellationToken ct = default) =>
        DeleteAsync($"/vouchers/{id}", ct);

    public Task<LedgerReportDto> GetLedgerAsync(Guid accountId, DateOnly from, DateOnly to, int page = 1, int pageSize = 50, CancellationToken ct = default) =>
        GetAsync<LedgerReportDto>($"/reports/ledger?accountId={accountId}&dateFrom={from:yyyy-MM-dd}&dateTo={to:yyyy-MM-dd}&page={page}&pageSize={pageSize}", ct);

    public Task<BookReportDto> GetBookAsync(CashBankKind kind, DateOnly from, DateOnly to, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var endpoint = kind == CashBankKind.Cash ? "cashbook" : "bankbook";
        return GetAsync<BookReportDto>($"/reports/{endpoint}?dateFrom={from:yyyy-MM-dd}&dateTo={to:yyyy-MM-dd}&page={page}&pageSize={pageSize}", ct);
    }

    public Task<TrialBalanceDto> GetTrialBalanceAsync(DateOnly asOf, CancellationToken ct = default) =>
        GetAsync<TrialBalanceDto>($"/reports/trial-balance?asOf={asOf:yyyy-MM-dd}", ct);

    public Task<ProfitLossDto> GetProfitLossAsync(DateOnly from, DateOnly to, CancellationToken ct = default) =>
        GetAsync<ProfitLossDto>($"/reports/profit-loss?dateFrom={from:yyyy-MM-dd}&dateTo={to:yyyy-MM-dd}", ct);

    public Task<BalanceSheetDto> GetBalanceSheetAsync(DateOnly asOf, CancellationToken ct = default) =>
        GetAsync<BalanceSheetDto>($"/reports/balance-sheet?asOf={asOf:yyyy-MM-dd}", ct);

    public Task<FamilyDto> GetFamilyAsync(CancellationToken ct = default) =>
        GetAsync<FamilyDto>("/families/current", ct);

    public Task<FamilySettingsDto> GetFamilySettingsAsync(CancellationToken ct = default) =>
        GetAsync<FamilySettingsDto>("/families/settings", ct);

    public Task<FamilySettingsDto> UpdateFamilySettingsAsync(UpdateFamilySettingsRequest request, CancellationToken ct = default) =>
        PutAsync<UpdateFamilySettingsRequest, FamilySettingsDto>("/families/settings", request, ct);

    public Task<PagedResult<UserDto>> GetMembersAsync(int page = 1, int pageSize = 50, string? search = null, CancellationToken ct = default) =>
        GetAsync<PagedResult<UserDto>>($"/users?page={page}&pageSize={pageSize}&search={Uri.EscapeDataString(search ?? "")}", ct);

    public Task<UserDto> CreateMemberAsync(CreateMemberRequest request, CancellationToken ct = default) =>
        PostAsync<CreateMemberRequest, UserDto>("/users", request, true, ct);

    public Task<List<RoleDto>> GetRolesAsync(CancellationToken ct = default) =>
        GetAsync<List<RoleDto>>("/roles", ct);

    public Task<UserDto> GetProfileAsync(CancellationToken ct = default) =>
        GetAsync<UserDto>("/profile", ct);

    public Task<UserDto> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken ct = default) =>
        PutAsync<UpdateProfileRequest, UserDto>("/profile", request, ct);

    public Task<List<string>> GetMyPermissionsAsync(CancellationToken ct = default) =>
        GetAsync<List<string>>("/permissions/mine", ct);

    public Task<PagedResult<NotificationDto>> GetNotificationsAsync(int page = 1, int pageSize = 30, CancellationToken ct = default) =>
        GetAsync<PagedResult<NotificationDto>>($"/notifications?page={page}&pageSize={pageSize}", ct);

    public Task MarkNotificationReadAsync(Guid id, CancellationToken ct = default) =>
        PatchAsync($"/notifications/{id}/read", ct);

    public Task MarkAllNotificationsReadAsync(CancellationToken ct = default) =>
        PatchAsync("/notifications/read-all", ct);

    // ---- transport ------------------------------------------------------------

    private Task<T> GetAsync<T>(string path, CancellationToken ct = default) =>
        SendAsync<T>(HttpMethod.Get, path, null, true, ct);

    private Task<TResponse> PostAsync<TRequest, TResponse>(string path, TRequest? body, bool authorized = true, CancellationToken ct = default) =>
        SendAsync<TResponse>(HttpMethod.Post, path, body, authorized, ct);

    private Task<TResponse> PutAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken ct = default) =>
        SendAsync<TResponse>(HttpMethod.Put, path, body, true, ct);

    private Task PatchAsync(string path, CancellationToken ct = default) =>
        SendAsync<object?>(HttpMethod.Patch, path, null, true, ct);

    private Task DeleteAsync(string path, CancellationToken ct = default) =>
        SendAsync<object?>(HttpMethod.Delete, path, null, true, ct);

    private async Task<T> SendAsync<T>(HttpMethod method, string path, object? body, bool authorized, CancellationToken ct)
    {
        var response = await SendRawAsync(method, path, body, authorized, ct);

        if (response.StatusCode == HttpStatusCode.Unauthorized && authorized)
        {
            if (await TryRefreshAsync(ct))
            {
                response.Dispose();
                response = await SendRawAsync(method, path, body, authorized, ct);
            }
            else
            {
                tokens.Clear();
                session.Clear();
                SessionExpired?.Invoke();
                throw new ApiException("Your session has expired. Please sign in again.", [], HttpStatusCode.Unauthorized);
            }
        }

        using (response)
        {
            ApiResponse<T>? envelope = null;
            try
            {
                envelope = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(JsonOptions, ct);
            }
            catch (JsonException)
            {
                // Non-envelope body (proxy error page, rate limiter plain text, …)
            }

            if (envelope is { Success: true })
                return envelope.Data!;

            var message = envelope?.Message
                ?? (response.StatusCode == HttpStatusCode.TooManyRequests
                    ? "Too many attempts. Please wait a moment."
                    : "Something went wrong. Please try again.");
            throw new ApiException(message, envelope?.Errors ?? [], response.StatusCode);
        }
    }

    private async Task<HttpResponseMessage> SendRawAsync(HttpMethod method, string path, object? body, bool authorized, CancellationToken ct)
    {
        var request = new HttpRequestMessage(method, $"{ServerUrl}{ApiPrefix}{path}");
        if (body is not null)
            request.Content = JsonContent.Create(body, body.GetType(), options: JsonOptions);

        if (authorized && await tokens.GetAccessTokenAsync() is { } accessToken)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return await _http.SendAsync(request, ct);
    }

    /// <summary>Single-flight refresh: concurrent 401s wait for one refresh call.</summary>
    public async Task<bool> TryRefreshAsync(CancellationToken ct = default)
    {
        await _refreshLock.WaitAsync(ct);
        try
        {
            var refreshToken = await tokens.GetRefreshTokenAsync();
            if (refreshToken is null)
                return false;

            using var response = await _http.PostAsJsonAsync(
                $"{ServerUrl}{ApiPrefix}/auth/refresh",
                new RefreshTokenRequest { RefreshToken = refreshToken }, JsonOptions, ct);

            if (!response.IsSuccessStatusCode)
                return false;

            var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>(JsonOptions, ct);
            if (envelope is not { Success: true, Data: { } auth })
                return false;

            await tokens.SaveAsync(auth.AccessToken, auth.RefreshToken);
            session.Start(auth);
            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            _refreshLock.Release();
        }
    }
}
