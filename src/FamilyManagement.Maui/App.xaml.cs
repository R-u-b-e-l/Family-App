namespace FamilyManagement.Maui;

public partial class App : Application
{
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

    protected override Window CreateWindow(IActivationState? activationState) => new(new AppShell());
}
