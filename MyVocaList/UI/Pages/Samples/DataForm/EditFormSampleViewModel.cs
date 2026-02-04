using DevExpress.Maui.DataForm;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace MyVocaList.UI.Pages.Samples.DataForm;

public class EditFormSampleViewModel : INotifyPropertyChanged
{
    public Singer Singer { get; set; }
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public EditFormSampleViewModel()
    {
        Singer = new Singer();
        SaveCommand = new Command(OnSave);
        CancelCommand = new Command(OnCancel);
    }

    private async void OnSave()
    {
        // In real app: save to database via service
        await Application.Current!.MainPage!.DisplayAlert(
            "Success",
            $"Singer {Singer.FirstName} {Singer.LastName} saved!",
            "OK");

        Console.WriteLine($"Saved Singer: {Singer.FirstName} {Singer.LastName}");
    }

    private async void OnCancel()
    {
        var confirmed = await Application.Current!.MainPage!.DisplayAlert(
            "Cancel",
            "Discard changes?",
            "Yes",
            "No");

        if (confirmed)
        {
            Singer = new Singer();
            OnPropertyChanged(nameof(Singer));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class Singer : INotifyPropertyChanged
{
    private string _firstName = string.Empty;
    private string _lastName = string.Empty;
    private string _email = string.Empty;
    private string _phone = string.Empty;
    private string _preferredGenre = string.Empty;
    private string _notes = string.Empty;
    private bool _isActive = true;

    [Required(ErrorMessage = "First name is required")]
    [MinLength(2, ErrorMessage = "First name must be at least 2 characters")]
    public string FirstName
    {
        get => _firstName;
        set => SetProperty(ref _firstName, value);
    }

    [Required(ErrorMessage = "Last name is required")]
    [MinLength(2, ErrorMessage = "Last name must be at least 2 characters")]
    public string LastName
    {
        get => _lastName;
        set => SetProperty(ref _lastName, value);
    }

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    [Phone(ErrorMessage = "Invalid phone format")]
    public string Phone
    {
        get => _phone;
        set => SetProperty(ref _phone, value);
    }

    public string PreferredGenre
    {
        get => _preferredGenre;
        set => SetProperty(ref _preferredGenre, value);
    }

    public string Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
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

// ComboBox Data Provider
public class GenrePickerProvider : IPickerSourceProvider
{
    public static GenrePickerProvider Instance { get; } = new GenrePickerProvider();

    public IEnumerable GetSource(string propertyName)
    {
        return new List<string>
        {
            "Rock",
            "Pop",
            "Country",
            "R&B",
            "Hip-Hop",
            "Jazz",
            "Blues",
            "Electronic",
            "Classical",
            "Alternative"
        };
    }
}