using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MyVocaList.UI.Pages.Samples.BottomSheet;

public class BottomSheetSampleViewModel : INotifyPropertyChanged
{
    private string _selectedGenre = "All";
    private double _selectedYear = 2024;
    private bool _hasFilters = false;
    private string _filterSummary = string.Empty;

    public ObservableCollection<GenreItem> Genres { get; }

    public string SelectedGenre
    {
        get => _selectedGenre;
        set => SetProperty(ref _selectedGenre, value);
    }

    public double SelectedYear
    {
        get => _selectedYear;
        set => SetProperty(ref _selectedYear, value);
    }

    public bool HasFilters
    {
        get => _hasFilters;
        set => SetProperty(ref _hasFilters, value);
    }

    public string FilterSummary
    {
        get => _filterSummary;
        set => SetProperty(ref _filterSummary, value);
    }

    public BottomSheetSampleViewModel()
    {
        Genres = new ObservableCollection<GenreItem>
        {
            new GenreItem("All"),
            new GenreItem("Rock"),
            new GenreItem("Pop"),
            new GenreItem("Country"),
            new GenreItem("R&B"),
            new GenreItem("Hip-Hop"),
            new GenreItem("Jazz")
        };
    }

    public void ApplyFilters()
    {
        FilterSummary = $"Genre: {SelectedGenre}, Year: {SelectedYear:F0}";
        HasFilters = true;
    }

    public void ClearFilters()
    {
        SelectedGenre = "All";
        SelectedYear = 2024;
        HasFilters = false;
        FilterSummary = string.Empty;
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

public class GenreItem
{
    public string Name { get; set; }

    public GenreItem(string name)
    {
        Name = name;
    }
}