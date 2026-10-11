namespace FamilyManagement.Maui.Services;

/// <summary>Tokens live in Android SecureStorage only. Passwords are never stored.</summary>
public class TokenStore
{
    private const string AccessKey = "fm_access_token";
    private const string RefreshKey = "fm_refresh_token";

    private string? _accessToken;

    public async Task<string?> GetAccessTokenAsync()
    {
        _accessToken ??= await SecureStorage.Default.GetAsync(AccessKey);
        return _accessToken;
    }

    public Task<string?> GetRefreshTokenAsync() => SecureStorage.Default.GetAsync(RefreshKey);

    public async Task SaveAsync(string accessToken, string refreshToken)
    {
        _accessToken = accessToken;
        await SecureStorage.Default.SetAsync(AccessKey, accessToken);
        await SecureStorage.Default.SetAsync(RefreshKey, refreshToken);
    }

    public void Clear()
    {
        _accessToken = null;
        SecureStorage.Default.Remove(AccessKey);
        SecureStorage.Default.Remove(RefreshKey);
    }

    public async Task<bool> HasSessionAsync() => await GetRefreshTokenAsync() is not null;
}
