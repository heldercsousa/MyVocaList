using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MyVocaList.UI.Pages.Samples.Editors;

public class EditorsSampleViewModel : INotifyPropertyChanged
{
    private string _textValue = string.Empty;
    private string _passwordValue = string.Empty;
    private string _selectedGenre = "Rock";
    private int _queuePosition = 1;
    private DateTime _eventDate = DateTime.Now;
    private TimeSpan _eventTime = DateTime.Now.TimeOfDay;
    private bool _isVIP = false;
    private string _notes = string.Empty;

    public ObservableCollection<string> GenreList { get; }

    public string TextValue
    {
        get => _textValue;
        set => SetProperty(ref _textValue, value);
    }

    public string PasswordValue
    {
        get => _passwordValue;
        set => SetProperty(ref _passwordValue, value);
    }

    public string SelectedGenre
    {
        get => _selectedGenre;
        set => SetProperty(ref _selectedGenre, value);
    }

    public int QueuePosition
    {
        get => _queuePosition;
        set => SetProperty(ref _queuePosition, value);
    }

    public DateTime EventDate
    {
        get => _eventDate;
        set => SetProperty(ref _eventDate, value);
    }

    public TimeSpan EventTime
    {
        get => _eventTime;
        set => SetProperty(ref _eventTime, value);
    }

    public bool IsVIP
    {
        get => _isVIP;
        set => SetProperty(ref _isVIP, value);
    }

    public string Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    public EditorsSampleViewModel()
    {
        GenreList = new ObservableCollection<string>
        {
            "Rock", "Pop", "Country", "R&B", "Hip-Hop",
            "Jazz", "Blues", "Electronic", "Classical"
        };
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