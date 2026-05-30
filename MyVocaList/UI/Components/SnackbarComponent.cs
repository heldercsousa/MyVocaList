using CommunityToolkit.Maui.Alerts;

namespace MyVocaList.UI.Components;

/// <summary>Thread-safe snackbar notification component.</summary>
public interface ISnackbarComponent
{
    Task ShowSuccessAsync(string message);
    Task ShowErrorAsync(string message);

    /// <summary>Shows a snackbar with an action button for undo scenarios.</summary>
    Task ShowWithUndoAsync(string message, string actionLabel, Func<Task> onAction);
}

/// <summary>Snackbar implementation using CommunityToolkit.Maui.</summary>
public class SnackbarComponent : ISnackbarComponent
{
    private static readonly TimeSpan Duration = TimeSpan.FromSeconds(4);

    public async Task ShowSuccessAsync(string message)
    {
        await ShowSnackbarAsync(message);
    }

    public async Task ShowErrorAsync(string message)
    {
        await ShowSnackbarAsync(message);
    }

    public async Task ShowWithUndoAsync(string message, string actionLabel, Func<Task> onAction)
    {
        if (Application.Current?.Dispatcher.IsDispatchRequired == true)
        {
            await Application.Current.Dispatcher.DispatchAsync(async () =>
                await DisplaySnackbarWithActionAsync(message, actionLabel, onAction));
            return;
        }

        await DisplaySnackbarWithActionAsync(message, actionLabel, onAction);
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

    private static async Task DisplaySnackbarWithActionAsync(string message, string actionLabel, Func<Task> onAction)
    {
        var snackbar = Snackbar.Make(message, action: () => _ = onAction(), actionButtonText: actionLabel, duration: Duration);
        await snackbar.Show();
    }
}
