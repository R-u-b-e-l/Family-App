using FamilyManagement.Maui.Helpers;
using FamilyManagement.Maui.Services;

namespace FamilyManagement.Maui.Views;

public partial class SplashPage : ContentPage
{
    private readonly TokenStore _tokens;
    private readonly Session _session;
    private readonly ApiClient _api;
    private bool _started;

    public SplashPage(TokenStore tokens, Session session, ApiClient api)
    {
        InitializeComponent();
        _tokens = tokens;
        _session = session;
        _api = api;

        _api.SessionExpired += () => MainThread.BeginInvokeOnMainThread(async () =>
        {
            try { await Shell.Current.GoToAsync("//login"); } catch { /* already there */ }
        });
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_started)
            return;
        _started = true;

        MoneyConverter.Symbol = CurrencySymbol(_session.Currency);

        // Android 13+ needs the user's consent before notifications can appear.
        try
        {
            await Permissions.RequestAsync<Permissions.PostNotifications>();
        }
        catch
        {
            // Older Android or denied — the app works fine without it.
        }

        if (!await _tokens.HasSessionAsync())
        {
            await Shell.Current.GoToAsync("//login");
            return;
        }

        _session.Restore();

        // Try to refresh in the background; cached session lets the app open offline.
        var refreshed = await _api.TryRefreshAsync();
        if (!refreshed && _session.CurrentUser is null)
        {
            await Shell.Current.GoToAsync("//login");
            return;
        }

        AppShell.ApplySession(_session);
        await Shell.Current.GoToAsync("//main");
    }

    public static string CurrencySymbol(string code) => code.ToUpperInvariant() switch
    {
        "BDT" => "৳",
        "USD" => "$",
        "EUR" => "€",
        "GBP" => "£",
        "INR" => "₹",
        _ => code + " "
    };
}
