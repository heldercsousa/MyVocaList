using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MyVocaList.UI.Pages.Samples.CollectionView;

public class FilterChipsSampleViewModel : INotifyPropertyChanged
{
    private ObservableCollection<Song> _allSongs;
    private ObservableCollection<Song> _filteredSongs;

    public ObservableCollection<GenreFilter> Genres { get; }

    public ObservableCollection<Song> FilteredSongs
    {
        get => _filteredSongs;
        set
        {
            _filteredSongs = value;
            OnPropertyChanged();
        }
    }

    public FilterChipsSampleViewModel()
    {
        // Initialize genres
        Genres = new ObservableCollection<GenreFilter>
        {
            new GenreFilter("All"),
            new GenreFilter("Rock"),
            new GenreFilter("Pop"),
            new GenreFilter("Country"),
            new GenreFilter("R&B"),
            new GenreFilter("Hip-Hop")
        };

        // Initialize songs
        _allSongs = new ObservableCollection<Song>
        {
            new Song("Bohemian Rhapsody", "Queen", "Rock"),
            new Song("Sweet Child O' Mine", "Guns N' Roses", "Rock"),
            new Song("Hotel California", "Eagles", "Rock"),
            new Song("Wonderwall", "Oasis", "Rock"),
            new Song("Don't Stop Believin'", "Journey", "Rock"),

            new Song("Billie Jean", "Michael Jackson", "Pop"),
            new Song("I Want It That Way", "Backstreet Boys", "Pop"),
            new Song("Shape of You", "Ed Sheeran", "Pop"),
            new Song("Uptown Funk", "Bruno Mars", "Pop"),

            new Song("Jolene", "Dolly Parton", "Country"),
            new Song("Take Me Home, Country Roads", "John Denver", "Country"),
            new Song("Ring of Fire", "Johnny Cash", "Country"),

            new Song("Superstition", "Stevie Wonder", "R&B"),
            new Song("Let's Stay Together", "Al Green", "R&B"),

            new Song("Lose Yourself", "Eminem", "Hip-Hop"),
            new Song("Juicy", "The Notorious B.I.G.", "Hip-Hop")
        };

        _filteredSongs = new ObservableCollection<Song>(_allSongs);
    }

    public void ApplyGenreFilter(string genreName)
    {
        if (genreName == "All")
        {
            FilteredSongs = new ObservableCollection<Song>(_allSongs);
        }
        else
        {
            FilteredSongs = new ObservableCollection<Song>(
                _allSongs.Where(s => s.Genre == genreName));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class GenreFilter
{
    public string Name { get; set; }

    public GenreFilter(string name)
    {
        Name = name;
    }
}

public class Song
{
    public string Title { get; set; }
    public string Artist { get; set; }
    public string Genre { get; set; }

    public Song(string title, string artist, string genre)
    {
        Title = title;
        Artist = artist;
        Genre = genre;
    }
}