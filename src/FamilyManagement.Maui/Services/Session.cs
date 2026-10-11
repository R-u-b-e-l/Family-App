using System.Text.Json;
using FamilyManagement.Shared.Dtos;

namespace FamilyManagement.Maui.Services;

/// <summary>Signed-in user context. Permissions only drive UI visibility — the API re-checks everything.</summary>
public class Session
{
    private const string UserKey = "fm_user";
    private const string PermissionsKey = "fm_permissions";
    private const string CurrencyKey = "fm_currency";

    public UserDto? CurrentUser { get; private set; }
    public HashSet<string> Permissions { get; private set; } = [];

    public string Currency
    {
        get => Preferences.Default.Get(CurrencyKey, "BDT");
        set => Preferences.Default.Set(CurrencyKey, value);
    }

    public bool IsAdmin => CurrentUser?.Roles.Contains("FamilyAdmin") == true;

    public void Start(AuthResponse auth)
    {
        CurrentUser = auth.User;
        Permissions = [.. auth.Permissions];
        Preferences.Default.Set(UserKey, JsonSerializer.Serialize(auth.User));
        Preferences.Default.Set(PermissionsKey, JsonSerializer.Serialize(auth.Permissions));
    }

    public void UpdateUser(UserDto user)
    {
        CurrentUser = user;
        Preferences.Default.Set(UserKey, JsonSerializer.Serialize(user));
    }

    public void UpdatePermissions(IEnumerable<string> permissions)
    {
        Permissions = [.. permissions];
        Preferences.Default.Set(PermissionsKey, JsonSerializer.Serialize(Permissions));
    }

    /// <summary>Restores the last session snapshot (offline-friendly display data).</summary>
    public void Restore()
    {
        try
        {
            var userJson = Preferences.Default.Get(UserKey, string.Empty);
            if (!string.IsNullOrEmpty(userJson))
                CurrentUser = JsonSerializer.Deserialize<UserDto>(userJson);
            var permJson = Preferences.Default.Get(PermissionsKey, string.Empty);
            if (!string.IsNullOrEmpty(permJson))
                Permissions = JsonSerializer.Deserialize<HashSet<string>>(permJson) ?? [];
        }
        catch
        {
            // Corrupted prefs — treat as signed out.
        }
    }

    public bool Can(string permissionCode) => Permissions.Contains(permissionCode);

    public void Clear()
    {
        CurrentUser = null;
        Permissions = [];
        Preferences.Default.Remove(UserKey);
        Preferences.Default.Remove(PermissionsKey);
    }
}
