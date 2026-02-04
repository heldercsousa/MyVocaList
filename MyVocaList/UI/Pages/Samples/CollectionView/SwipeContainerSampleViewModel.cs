using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace MyVocaList.UI.Pages.Samples.CollectionView;

public class SwipeContainerSampleViewModel : INotifyPropertyChanged
{
    public ObservableCollection<QueueItem> QueueItems { get; }
    public ICommand ToggleSongStateCommand { get; }
    public ICommand RemoveSongCommand { get; }

    public SwipeContainerSampleViewModel()
    {
        QueueItems = new ObservableCollection<QueueItem>
        {
            new QueueItem("Bohemian Rhapsody", "John Doe"),
            new QueueItem("Don't Stop Believin'", "Jane Smith"),
            new QueueItem("Sweet Child O' Mine", "Mike Johnson"),
            new QueueItem("Livin' on a Prayer", "Sarah Williams"),
            new QueueItem("Hotel California", "Tom Brown"),
            new QueueItem("Wonderwall", "Emily Davis"),
            new QueueItem("Mr. Brightside", "Chris Wilson"),
            new QueueItem("I Want It That Way", "Lisa Anderson")
        };

        ToggleSongStateCommand = new Command<QueueItem>(ToggleSongState);
        RemoveSongCommand = new Command<QueueItem>(RemoveSong);
    }

    private void ToggleSongState(QueueItem item)
    {
        if (item == null) return;

        item.IsPlaying = !item.IsPlaying;

        // In real app: trigger playback or queue management
        Console.WriteLine($"Toggled: {item.SongTitle} - Playing: {item.IsPlaying}");
    }

    private void RemoveSong(QueueItem item)
    {
        if (item == null) return;

        QueueItems.Remove(item);
        Console.WriteLine($"Removed: {item.SongTitle}");
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class QueueItem : INotifyPropertyChanged
{
    private bool _isPlaying;

    public string SongTitle { get; set; }
    public string SingerName { get; set; }

    public bool IsPlaying
    {
        get => _isPlaying;
        set
        {
            if (_isPlaying != value)
            {
                _isPlaying = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ItemColor));
                OnPropertyChanged(nameof(ActionText));
                OnPropertyChanged(nameof(ActionIcon));
            }
        }
    }

    public Color ItemColor => IsPlaying
        ? Color.FromArgb("#c6eccb") // Light green for playing
        : Color.FromArgb("#f5f5f5"); // Light gray for queued

    public string ActionText => IsPlaying ? "Queue" : "Play Now";
    public string ActionIcon => IsPlaying ? "pause_icon.png" : "play_icon.png";

    public QueueItem(string songTitle, string singerName)
    {
        SongTitle = songTitle;
        SingerName = singerName;
        IsPlaying = false;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}