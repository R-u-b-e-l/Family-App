using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using FamilyManagement.Maui.Services;

namespace FamilyManagement.Maui.ViewModels;

public abstract partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    private string _title = string.Empty;

    /// <summary>Set when showing data from the local cache while offline.</summary>
    [ObservableProperty]
    private bool _isOfflineData;

    protected static Task ToastAsync(string message) =>
        MainThread.InvokeOnMainThreadAsync(() => Toast.Make(message).Show());

    /// <summary>Runs an operation with the busy flag and friendly error toasts.</summary>
    protected async Task RunAsync(Func<Task> operation, bool useBusyFlag = true)
    {
        if (useBusyFlag)
        {
            if (IsBusy)
                return;
            IsBusy = true;
        }

        try
        {
            await operation();
        }
        catch (ApiException ex)
        {
            await ToastAsync(ex.FullMessage);
        }
        catch (HttpRequestException)
        {
            await ToastAsync("Cannot reach the server. Check your connection.");
        }
        catch (TaskCanceledException)
        {
            await ToastAsync("The request timed out. Try again.");
        }
        catch (Exception ex)
        {
            // Never let an unexpected error kill the app — surface it instead.
            await ToastAsync($"Unexpected error: {ex.Message}");
        }
        finally
        {
            if (useBusyFlag)
                IsBusy = false;
            IsRefreshing = false;
        }
    }

    protected static Task GoToLoginAsync() =>
        MainThread.InvokeOnMainThreadAsync(() => Shell.Current.GoToAsync("//login"));

    /// <summary>Opens a downloaded PDF with the phone's viewer/share sheet.</summary>
    protected static async Task OpenPdfAsync(string filePath, string title)
    {
        await Launcher.Default.OpenAsync(new OpenFileRequest(title, new ReadOnlyFile(filePath)));
    }
}
