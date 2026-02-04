using DevExpress.Maui.DataForm;
using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace MyVocaList.UI.Pages.Samples.DataForm;

public class ComboBoxEditorSampleViewModel : INotifyPropertyChanged
{
    public SongRequest Request { get; set; }
    public ICommand SubmitCommand { get; }

    public ComboBoxEditorSampleViewModel()
    {
        Request = new SongRequest();
        SubmitCommand = new Command(OnSubmit);
    }

    private async void OnSubmit()
    {
        var selectedSong = SongPickerProvider.Instance.GetSource("SelectedSongId")
            .Cast<SongInfo>()
            .FirstOrDefault(s => s.Id == Request.SelectedSongId);

        await Application.Current!.MainPage!.DisplayAlert(
            "Request Submitted",
            $"Singer: {Request.SingerName}\n" +
            $"Song: {selectedSong?.FullTitle ?? "N/A"}\n" +
            $"Urgency: {Request.Urgency}",
            "OK");
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class SongRequest : INotifyPropertyChanged
{
    private string _singerName = string.Empty;
    private int _selectedSongId;
    private UrgencyLevel _urgency = UrgencyLevel.Normal;
    private string _specialRequests = string.Empty;

    public string SingerName
    {
        get => _singerName;
        set => SetProperty(ref _singerName, value);
    }

    public int SelectedSongId
    {
        get => _selectedSongId;
        set => SetProperty(ref _selectedSongId, value);
    }

    public UrgencyLevel Urgency
    {
        get => _urgency;
        set => SetProperty(ref _urgency, value);
    }

    public string SpecialRequests
    {
        get => _specialRequests;
        set => SetProperty(ref _specialRequests, value);
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

public class SongInfo
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Artist { get; set; }
    public string Genre { get; set; }
    public string FullTitle => $"{Title} - {Artist}";

    public SongInfo(int id, string title, string artist, string genre)
    {
        Id = id;
        Title = title;
        Artist = artist;
        Genre = genre;
    }
}

public enum UrgencyLevel
{
    Low,
    Normal,
    High,
    VIP
}

// Picker Providers
public class SingerNamePickerProvider : IPickerSourceProvider
{
    public static SingerNamePickerProvider Instance { get; } = new SingerNamePickerProvider();

    public IEnumerable GetSource(string propertyName)
    {
        return new List<string>
        {
            "John Doe",
            "Jane Smith",
            "Mike Johnson",
            "Sarah Williams",
            "Tom Brown",
            "Emily Davis",
            "Chris Wilson",
            "Lisa Anderson"
        };
    }
}

public class SongPickerProvider : IPickerSourceProvider
{
    public static SongPickerProvider Instance { get; } = new SongPickerProvider();

    public IEnumerable GetSource(string propertyName)
    {
        return new List<SongInfo>
        {
            new SongInfo(1, "Bohemian Rhapsody", "Queen", "Rock"),
            new SongInfo(2, "Don't Stop Believin'", "Journey", "Rock"),
            new SongInfo(3, "Sweet Child O' Mine", "Guns N' Roses", "Rock"),
            new SongInfo(4, "Hotel California", "Eagles", "Rock"),
            new SongInfo(5, "Billie Jean", "Michael Jackson", "Pop"),
            new SongInfo(6, "I Want It That Way", "Backstreet Boys", "Pop"),
            new SongInfo(7, "Shape of You", "Ed Sheeran", "Pop"),
            new SongInfo(8, "Jolene", "Dolly Parton", "Country"),
            new SongInfo(9, "Superstition", "Stevie Wonder", "R&B"),
            new SongInfo(10, "Lose Yourself", "Eminem", "Hip-Hop")
        };
    }
}

public class UrgencyPickerProvider : IPickerSourceProvider
{
    public static UrgencyPickerProvider Instance { get; } = new UrgencyPickerProvider();

    public IEnumerable GetSource(string propertyName)
    {
        return new List<UrgencyLevel>
        {
            UrgencyLevel.Low,
            UrgencyLevel.Normal,
            UrgencyLevel.High,
            UrgencyLevel.VIP
        };
    }
}
