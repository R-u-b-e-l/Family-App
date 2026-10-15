namespace FamilyManagement.Maui;

public partial class App : Application
{
    /// <summary>True while the user has the app on screen; the background poller skips then.</summary>
    public static bool IsInForeground { get; private set; }

    public App()
    {
        InitializeComponent();

        var theme = Preferences.Default.Get("fm_theme", "system");
        UserAppTheme = theme switch
        {
            "light" => AppTheme.Light,
            "dark" => AppTheme.Dark,
            _ => AppTheme.Unspecified
        };
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell());
        window.Resumed += (_, _) => IsInForeground = true;
        window.Activated += (_, _) => IsInForeground = true;
        window.Deactivated += (_, _) => IsInForeground = false;
        window.Stopped += (_, _) => IsInForeground = false;
        return window;
    }
}
