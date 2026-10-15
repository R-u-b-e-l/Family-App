using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;

namespace FamilyManagement.Maui.Platforms.Android;

/// <summary>Shows notifications in the Android notification bar (channel + tap-to-open).</summary>
public static class LocalNotifications
{
    public const string ChannelId = "fm_general";

    public static void EnsureChannel(Context context)
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            return;
        var manager = (NotificationManager?)context.GetSystemService(Context.NotificationService);
        if (manager?.GetNotificationChannel(ChannelId) is null)
        {
            var channel = new NotificationChannel(ChannelId, "Family Manager", NotificationImportance.Default)
            {
                Description = "Family transactions, loans and member updates"
            };
            manager?.CreateNotificationChannel(channel);
        }
    }

    public static void Show(Context context, int id, string title, string? body)
    {
        EnsureChannel(context);

        var launchIntent = context.PackageManager?.GetLaunchIntentForPackage(context.PackageName ?? "");
        var pendingIntent = launchIntent is null
            ? null
            : PendingIntent.GetActivity(context, 0, launchIntent,
                PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);

        var builder = new NotificationCompat.Builder(context, ChannelId)
            .SetSmallIcon(context.ApplicationInfo!.Icon)
            .SetContentTitle(title)
            .SetAutoCancel(true)
            .SetPriority(NotificationCompat.PriorityDefault);

        if (!string.IsNullOrWhiteSpace(body))
            builder.SetContentText(body).SetStyle(new NotificationCompat.BigTextStyle().BigText(body));
        if (pendingIntent is not null)
            builder.SetContentIntent(pendingIntent);

        NotificationManagerCompat.From(context).Notify(id, builder.Build());
    }
}
