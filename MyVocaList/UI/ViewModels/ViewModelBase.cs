namespace MyVocaList.UI.ViewModels;

/// <summary>Base class for all ViewModels: INPC + UI thread helpers.</summary>
public abstract partial class ViewModelBase : ObservableObject
{
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
}
