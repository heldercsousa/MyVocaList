using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MyVocaList.Domain.RepositoryInterface;
using MyVocaList.UI.Collections;

namespace MyVocaList.UI.ViewModels;

/// <summary>ViewModel for song picker modal—search and select songs for queue entry.</summary>
public partial class SongPickerViewModel : ViewModelBase
{
    private readonly ISongRepository _songRepository;
    private readonly ILogger<SongPickerViewModel> _logger;

    public SongPickerViewModel(ISongRepository songRepository, ILogger<SongPickerViewModel> logger)
    {
        _songRepository = songRepository;
        _logger = logger;
        Results = [];
    }

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private ObservableRangeCollection<SongListItemDto> results;

    partial void OnSearchTextChanged(string value)
    {
        _ = SearchAsync(value);
    }

    [RelayCommand]
    public async Task InitializeCommand()
    {
        await SearchAsync(SearchText);
    }

    [RelayCommand]
    public async Task SelectSongAsync(SongListItemDto? song)
    {
        if (song == null) return;

        // Send message to QueueManagementViewModel
        WeakReferenceMessenger.Default.Send(new SongPickedMessage { SongId = song.Id });

        // Close modal
        await Shell.Current.GoToAsync("..");
    }

    private async Task SearchAsync(string query)
    {
        try
        {
            var songs = await _songRepository.GetAllAsync();
            var filtered = string.IsNullOrWhiteSpace(query)
                ? songs
                : songs.Where(s => s.Title.Contains(query, StringComparison.OrdinalIgnoreCase));

            var dtos = filtered
                .Select(s => new SongListItemDto(
                    s.Id,
                    s.Title,
                    s.OriginalArtistId,
                    s.OriginalArtist?.Name,
                    s.FeaturedArtists,
                    s.ExternalProvider,
                    false))
                .ToList();

            RunOnUiThread(() => Results.ReplaceRange(dtos));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching songs");
        }
    }
}

public class SongPickedMessage
{
    public int SongId { get; set; }
}
