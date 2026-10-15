using Android.Content;
using AndroidX.Work;
using FamilyManagement.Maui.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FamilyManagement.Maui.Platforms.Android;

/// <summary>
/// Periodic background job (WorkManager): while signed in and online, fetches unread
/// notifications from the family server and shows them in the notification bar —
/// even when the app is closed.
/// </summary>
public class NotificationPollWorker(Context context, WorkerParameters workerParams)
    : Worker(context, workerParams)
{
    private const string LastSeenKey = "fm_notif_last_ticks";

    public override Result DoWork()
    {
        try
        {
            PollAsync().GetAwaiter().GetResult();
            return Result.InvokeSuccess()!;
        }
        catch
        {
            // Network hiccup or expired session — try again on the next cycle.
            return Result.InvokeRetry()!;
        }
    }

    private async Task PollAsync()
    {
        var services = IPlatformApplication.Current?.Services;
        if (services is null)
            return;

        var tokens = services.GetService<TokenStore>();
        var api = services.GetService<ApiClient>();
        if (tokens is null || api is null || !await tokens.HasSessionAsync())
            return;

        // Skip while the user is actively inside the app.
        if (App.IsInForeground)
            return;

        var page = await api.GetNotificationsAsync(1, 20);
        var lastSeenTicks = Preferences.Default.Get(LastSeenKey, 0L);

        var fresh = page.Items
            .Where(n => !n.IsRead && n.CreatedDate.Ticks > lastSeenTicks)
            .OrderBy(n => n.CreatedDate)
            .ToList();
        if (fresh.Count == 0)
            return;

        if (fresh.Count <= 3)
        {
            foreach (var notification in fresh)
                LocalNotifications.Show(ApplicationContext, notification.Id.GetHashCode(),
                    notification.Title, notification.Body);
        }
        else
        {
            LocalNotifications.Show(ApplicationContext, LastSeenKey.GetHashCode(),
                $"{fresh.Count} new family updates",
                string.Join("\n", fresh.TakeLast(4).Select(n => $"• {n.Title}")));
        }

        Preferences.Default.Set(LastSeenKey, fresh.Max(n => n.CreatedDate.Ticks));
    }

    /// <summary>Registers the recurring poll (~15 min, only when the phone has internet).</summary>
    public static void Schedule(Context context)
    {
        var constraints = new Constraints.Builder()
            .SetRequiredNetworkType(NetworkType.Connected!)
            .Build();

        var request = new PeriodicWorkRequest.Builder(typeof(NotificationPollWorker), 15, Java.Util.Concurrent.TimeUnit.Minutes!)
            .SetConstraints(constraints)
            .Build();

        WorkManager.GetInstance(context)
            .EnqueueUniquePeriodicWork("fm-notification-poll", ExistingPeriodicWorkPolicy.Keep!, request);
    }
}
