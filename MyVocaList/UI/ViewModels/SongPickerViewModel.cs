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

    public SongPickerViewModel(
        IMusicMetadataService service,
        IMessenger messenger,
        INavigationService navigation,
        ILogger<SongPickerViewModel> logger)
    {
        _service = service;
        _messenger = messenger;
        _navigation = navigation;
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
    private Task BackAsync() => _navigation.GoBackAsync();

    public void Dispose()
    {
        try { _cts?.Cancel(); _cts?.Dispose(); }
        catch { /* ignore disposal races */ }
    }
}
