namespace MyVocaList.UI.Services;

/// <summary>
/// Thread-safe wrapper for dialog operations
/// </summary>
public interface IThreadSafeDialogService
{
    Task<bool> ConfirmAsync(string title, string message, string accept = "OK", string cancel = "Cancel");
    Task AlertAsync(string title, string message, string accept = "OK");
    Task<string?> PromptAsync(string title, string message, string accept = "OK", string cancel = "Cancel");
}

/// <summary>
/// Thread-safe dialog service implementation using Application.Current.Dispatcher
/// </summary>
public class ThreadSafeDialogService : IThreadSafeDialogService
{
    public async Task<bool> ConfirmAsync(string title, string message, string accept = "OK", string cancel = "Cancel")
    {
        if (Application.Current?.Dispatcher.IsDispatchRequired == true)
        {
            return await Application.Current.Dispatcher.DispatchAsync(async () =>
                await Application.Current.MainPage!.DisplayAlert(title, message, accept, cancel));
        }
        return await Application.Current!.MainPage!.DisplayAlert(title, message, accept, cancel);
    }

    public async Task AlertAsync(string title, string message, string accept = "OK")
    {
        if (Application.Current?.Dispatcher.IsDispatchRequired == true)
        {
            await Application.Current.Dispatcher.DispatchAsync(async () =>
                await Application.Current.MainPage!.DisplayAlert(title, message, accept));
            return;
        }
        await Application.Current!.MainPage!.DisplayAlert(title, message, accept);
    }

    public async Task<string?> PromptAsync(string title, string message, string accept = "OK", string cancel = "Cancel")
    {
        if (Application.Current?.Dispatcher.IsDispatchRequired == true)
        {
            return await Application.Current.Dispatcher.DispatchAsync(async () =>
                await Application.Current.MainPage!.DisplayPromptAsync(title, message, accept, cancel));
        }
        return await Application.Current!.MainPage!.DisplayPromptAsync(title, message, accept, cancel);
    }
}
