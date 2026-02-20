using CommunityToolkit.Maui.Alerts;

namespace MyVocaList.UI.Services;

/// <summary>
/// Thread-safe snackbar notification service
/// </summary>
public interface ISnackbarService
{
    Task ShowSuccessAsync(string message);
    Task ShowErrorAsync(string message);
}

/// <summary>
/// Snackbar implementation using CommunityToolkit.Maui
/// </summary>
public class SnackbarService : ISnackbarService
{
    private static readonly TimeSpan Duration = TimeSpan.FromSeconds(3);

    public async Task ShowSuccessAsync(string message)
    {
        await ShowSnackbarAsync(message);
    }

    public async Task ShowErrorAsync(string message)
    {
        await ShowSnackbarAsync(message);
    }

    private async Task ShowSnackbarAsync(string message)
    {
        if (Application.Current?.Dispatcher.IsDispatchRequired == true)
        {
            await Application.Current.Dispatcher.DispatchAsync(async () =>
                await DisplaySnackbarAsync(message));
            return;
        }

        await DisplaySnackbarAsync(message);
    }

    private static async Task DisplaySnackbarAsync(string message)
    {
        var snackbar = Snackbar.Make(message, duration: Duration);
        await snackbar.Show();
    }
}
