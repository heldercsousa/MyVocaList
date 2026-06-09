using CommunityToolkit.Mvvm.Messaging;
using MyVocaList.Contracts.DTOs;
using MyVocaList.Contracts.Messages;
using MyVocaList.UI.Collections;

namespace MyVocaList.UI.ViewModels;

public sealed partial class SongPickerViewModel : ViewModelBase, IDisposable
{
    private readonly IMusicMetadataService _service;
    private readonly IMessenger _messenger;
    private readonly INavigationService _navigation;
    private readonly ISnackbarComponent _snackbarService;
    private readonly ILogger<SongPickerViewModel> _logger;

    private CancellationTokenSource _cts = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasResults;

    [ObservableProperty]
    private bool _hasSearched;

    [ObservableProperty]
    private string _emptyStateMessage = string.Empty;

    public ObservableRangeCollection<MusicSearchResultDto> Results { get; } = [];

    partial void OnIsLoadingChanged(bool value) => OnPropertyChanged(nameof(IsShowEmptyState));
    partial void OnHasResultsChanged(bool value) => OnPropertyChanged(nameof(IsShowEmptyState));
    partial void OnHasSearchedChanged(bool value) => OnPropertyChanged(nameof(IsShowEmptyState));

    public bool IsShowEmptyState => HasSearched && !HasResults && !IsLoading;

    public SongPickerViewModel(
        IMusicMetadataService service,
        IMessenger messenger,
        INavigationService navigation,
        ISnackbarComponent snackbarService,
        ILogger<SongPickerViewModel> logger)
    {
        _service = service;
        _messenger = messenger;
        _navigation = navigation;
        _snackbarService = snackbarService;
        _logger = logger;
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText)) return;

        _cts.Cancel();
        _cts.Dispose();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        IsLoading = true;
        HasSearched = false;
        Results.Clear();

        try
        {
            var items = await _service.SearchSongsAsync(SearchText, artistHint: null, ct);
            Results.ReplaceRange(items);
            HasResults = Results.Count > 0;
            HasSearched = true;
            EmptyStateMessage = "No songs found";
        }
        catch (OperationCanceledException)
        {
            // Silently ignored — superseded by newer search
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Search failed for query {Query}", SearchText);
            HasResults = false;
            HasSearched = true;
            EmptyStateMessage = "Search failed. Please try again.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SelectResultAsync(MusicSearchResultDto result)
    {
        _messenger.Send(new SongPickedMessage(result));
        await _navigation.GoBackAsync();
    }

    [RelayCommand]
    private async Task LaunchYouTubeSearchAsync(MusicSearchResultDto result)
    {
        if (result == null || string.IsNullOrWhiteSpace(result.SongTitle) || string.IsNullOrWhiteSpace(result.ArtistName))
        {
            await _snackbarService.ShowErrorAsync("Song title or artist missing");
            return;
        }

        var query = $"karaoke {result.SongTitle} {result.ArtistName}";
        var encodedQuery = Uri.EscapeDataString(query);
        var youtubeUri = new Uri($"https://www.youtube.com/results?search_query={encodedQuery}");

        if (!await Launcher.TryOpenAsync(youtubeUri))
            await Browser.OpenAsync(youtubeUri, BrowserLaunchMode.SystemPreferred);
    }

    [RelayCommand]
    private Task BackAsync() => _navigation.GoBackAsync();

    public void Dispose()
    {
        try { _cts?.Cancel(); _cts?.Dispose(); }
        catch { /* ignore disposal races */ }
    }
}
