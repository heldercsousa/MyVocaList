using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MyVocaList.Domain.RepositoryInterface;
using MyVocaList.UI.Collections;

namespace MyVocaList.UI.ViewModels;

/// <summary>ViewModel for song picker modal—search and select songs for queue entries.</summary>
public partial class QueueSongPickerViewModel : ViewModelBase
{
    private readonly ISongRepository _songRepository;
    private readonly ILogger<QueueSongPickerViewModel> _logger;

    public QueueSongPickerViewModel(ISongRepository songRepository, ILogger<QueueSongPickerViewModel> logger)
    {
        _songRepository = songRepository;
        _logger = logger;
        Results = [];
    }

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private ObservableRangeCollection<SongDto> results;
    [ObservableProperty] private int queueEntryId;

    partial void OnSearchTextChanged(string value)
    {
        _ = SearchAsync(value);
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        await SearchAsync(SearchText);
    }

    [RelayCommand]
    public async Task SelectSongAsync(SongDto? song)
    {
        if (song == null) return;

        WeakReferenceMessenger.Default.Send(new SongPickedMessage { SongId = song.Id, QueueEntryId = QueueEntryId });

        await Shell.Current.GoToAsync("..");
    }

    private async Task SearchAsync(string query)
    {
        try
        {
            var (songs, _) = await _songRepository.GetPagedAsync(1, int.MaxValue, query, CancellationToken.None);
            var filtered = string.IsNullOrWhiteSpace(query)
                ? songs
                : songs.Where(s => s.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                  (s.OriginalArtistName ?? "").Contains(query, StringComparison.OrdinalIgnoreCase));

            var dtos = filtered.Select(s => new SongDto
            {
                Id = s.Id,
                Title = s.Title,
                Artist = s.OriginalArtistName ?? string.Empty
            }).ToList();
            RunOnUiThread(() => Results.ReplaceRange(dtos));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching songs");
        }
    }
}

public partial class SongDto : ObservableObject
{
    [ObservableProperty] private int id;
    [ObservableProperty] private string title = string.Empty;
    [ObservableProperty] private string artist = string.Empty;
}

public class SongPickedMessage
{
    public int SongId { get; set; }
    public int QueueEntryId { get; set; }
}
