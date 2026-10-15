using Android.App;
using Android.Content.PM;
using Android.OS;
using FamilyManagement.Maui.Platforms.Android;

namespace FamilyManagement.Maui;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        LocalNotifications.EnsureChannel(this);
        // Recurring background check for new family notifications (works while the app is closed).
        NotificationPollWorker.Schedule(this);
    }
}
