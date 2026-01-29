using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MyVocaList.UI.Pages.DesignSystem;

/// <summary>
/// ViewModel demonstrating UraniumUI automatic form generation
/// </summary>
public class DSFormsViewModel : INotifyPropertyChanged
{
    private string _fullName = string.Empty;
    private string _email = string.Empty;
    private int _age = 25;
    private string _biography = string.Empty;
    private bool _receiveNotifications = true;
    private string _subscriptionPlan = "Free";
    private DateTime _birthDate = new DateTime(1998, 1, 1);

    public string FullName
    {
        get => _fullName;
        set => SetProperty(ref _fullName, value);
    }

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public int Age
    {
        get => _age;
        set => SetProperty(ref _age, value);
    }

    public string Biography
    {
        get => _biography;
        set => SetProperty(ref _biography, value);
    }

    public bool ReceiveNotifications
    {
        get => _receiveNotifications;
        set => SetProperty(ref _receiveNotifications, value);
    }

    public string SubscriptionPlan
    {
        get => _subscriptionPlan;
        set => SetProperty(ref _subscriptionPlan, value);
    }

    public DateTime BirthDate
    {
        get => _birthDate;
        set => SetProperty(ref _birthDate, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
