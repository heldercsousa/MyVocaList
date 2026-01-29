using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MyVocaList.UI.Pages.DesignSystem;

/// <summary>
/// Comprehensive form ViewModel with multiple field types
/// </summary>
public class DSFormsViewModel : INotifyPropertyChanged
{
    private string _firstName = string.Empty;
    private string _lastName = string.Empty;
    private string _email = string.Empty;
    private string _phone = string.Empty;
    private string _address = string.Empty;
    private string _city = string.Empty;
    private string _country = string.Empty;
    private string _postalCode = string.Empty;
    private string _company = string.Empty;
    private string _jobTitle = string.Empty;
    private string _website = string.Empty;
    private string _notes = string.Empty;
    private DateTime _birthDate = new DateTime(1990, 1, 1);
    private DateTime _joinDate = DateTime.Now;
    private bool _isActive = true;
    private bool _emailNotifications = true;
    private bool _smsNotifications = false;

    public string FirstName
    {
        get => _firstName;
        set => SetProperty(ref _firstName, value);
    }

    public string LastName
    {
        get => _lastName;
        set => SetProperty(ref _lastName, value);
    }

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string Phone
    {
        get => _phone;
        set => SetProperty(ref _phone, value);
    }

    public string Address
    {
        get => _address;
        set => SetProperty(ref _address, value);
    }

    public string City
    {
        get => _city;
        set => SetProperty(ref _city, value);
    }

    public string Country
    {
        get => _country;
        set => SetProperty(ref _country, value);
    }

    public string PostalCode
    {
        get => _postalCode;
        set => SetProperty(ref _postalCode, value);
    }

    public string Company
    {
        get => _company;
        set => SetProperty(ref _company, value);
    }

    public string JobTitle
    {
        get => _jobTitle;
        set => SetProperty(ref _jobTitle, value);
    }

    public string Website
    {
        get => _website;
        set => SetProperty(ref _website, value);
    }

    public string Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    public DateTime BirthDate
    {
        get => _birthDate;
        set => SetProperty(ref _birthDate, value);
    }

    public DateTime JoinDate
    {
        get => _joinDate;
        set => SetProperty(ref _joinDate, value);
    }

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

    public bool EmailNotifications
    {
        get => _emailNotifications;
        set => SetProperty(ref _emailNotifications, value);
    }

    public bool SmsNotifications
    {
        get => _smsNotifications;
        set => SetProperty(ref _smsNotifications, value);
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
