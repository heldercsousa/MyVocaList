using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace MyVocaList.UI.Pages.Samples.Popup;

public class PopupServiceSampleViewModel : INotifyPropertyChanged
{
    private string _lastResult = string.Empty;
    private bool _hasResult = false;

    public string LastResult
    {
        get => _lastResult;
        set => SetProperty(ref _lastResult, value);
    }

    public bool HasResult
    {
        get => _hasResult;
        set => SetProperty(ref _hasResult, value);
    }

    public ICommand ShowSimpleAlertCommand { get; }
    public ICommand ShowConfirmationCommand { get; }
    public ICommand ShowCustomPopupCommand { get; }

    public PopupServiceSampleViewModel()
    {
        ShowSimpleAlertCommand = new Command(ShowSimpleAlert);
        ShowConfirmationCommand = new Command(ShowConfirmation);
        ShowCustomPopupCommand = new Command(ShowCustomPopup);
    }

    private async void ShowSimpleAlert()
    {
        await Application.Current!.MainPage!.DisplayAlert(
            "Information",
            "This is a simple alert dialog.",
            "OK");

        LastResult = "Simple alert dismissed";
        HasResult = true;
    }

    private async void ShowConfirmation()
    {
        bool result = await Application.Current!.MainPage!.DisplayAlert(
            "Confirm Action",
            "Do you want to remove this song from the queue?",
            "Yes",
            "No");

        LastResult = result ? "User confirmed" : "User cancelled";
        HasResult = true;
    }

    private void ShowCustomPopup()
    {
        // Trigger custom popup through code-behind
        if (Application.Current?.MainPage is NavigationPage navPage &&
            navPage.CurrentPage is PopupServiceSample page)
        {
            page.ShowCustomPopup();
        }
    }

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
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}