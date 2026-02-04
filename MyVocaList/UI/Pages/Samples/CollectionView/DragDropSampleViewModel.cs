using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MyVocaList.UI.Pages.Samples.CollectionView;

public class DragDropSampleViewModel : INotifyPropertyChanged
{
    public ObservableCollection<DragDropQueueItem> QueueItems { get; }

    public DragDropSampleViewModel()
    {
        QueueItems = new ObservableCollection<DragDropQueueItem>
        {
            new DragDropQueueItem(1, "Bohemian Rhapsody", "John Doe", "5:55"),
            new DragDropQueueItem(2, "Don't Stop Believin'", "Jane Smith", "4:10"),
            new DragDropQueueItem(3, "Sweet Child O' Mine", "Mike Johnson", "5:56"),
            new DragDropQueueItem(4, "Livin' on a Prayer", "Sarah Williams", "4:09"),
            new DragDropQueueItem(5, "Hotel California", "Tom Brown", "6:30"),
            new DragDropQueueItem(6, "Wonderwall", "Emily Davis", "4:18"),
            new DragDropQueueItem(7, "Mr. Brightside", "Chris Wilson", "3:42")
        };
    }

    public void UpdatePositions()
    {
        for (int i = 0; i < QueueItems.Count; i++)
        {
            QueueItems[i].Position = i + 1;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class DragDropQueueItem : INotifyPropertyChanged
{
    private int _position;

    public int Position
    {
        get => _position;
        set
        {
            if (_position != value)
            {
                _position = value;
                OnPropertyChanged();
            }
        }
    }

    public string SongTitle { get; set; }
    public string SingerName { get; set; }
    public string Duration { get; set; }

    public DragDropQueueItem(int position, string songTitle, string singerName, string duration)
    {
        Position = position;
        SongTitle = songTitle;
        SingerName = singerName;
        Duration = duration;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}