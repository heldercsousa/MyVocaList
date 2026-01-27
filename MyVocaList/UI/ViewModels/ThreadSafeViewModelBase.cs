using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MyVocaList.UI.ViewModels;

/// <summary>
/// Base ViewModel with thread-safe UI update helpers
/// </summary>
public abstract class ThreadSafeViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        RunOnUiThread(() =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));
    }

    protected void RunOnUiThread(Action action)
    {
        if (Application.Current?.Dispatcher.IsDispatchRequired == true)
            Application.Current.Dispatcher.Dispatch(action);
        else
            action();
    }

    protected Task RunOnUiThreadAsync(Func<Task> asyncAction)
    {
        if (Application.Current?.Dispatcher.IsDispatchRequired == true)
            return Application.Current.Dispatcher.DispatchAsync(asyncAction);
        else
            return asyncAction();
    }

    protected async Task<T> RunOnUiThreadAsync<T>(Func<Task<T>> asyncAction)
    {
        if (Application.Current?.Dispatcher.IsDispatchRequired == true)
            return await Application.Current.Dispatcher.DispatchAsync(asyncAction);
        else
            return await asyncAction();
    }
}
