using CommunityToolkit.Mvvm.Messaging;
using MyVocaList.Contracts.DTOs;
using MyVocaList.Contracts.Messages;
using MyVocaList.UI.Collections;

namespace MyVocaList.UI.ViewModels;

public sealed partial class ArtistPickerViewModel : ViewModelBase
{
    private readonly IMusicMetadataService _service;
    private readonly IMessenger _messenger;
    private readonly ILogger<ArtistPickerViewModel> _logger;

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

    public ArtistPickerViewModel(
        IMusicMetadataService service,
        IMessenger messenger,
        ILogger<ArtistPickerViewModel> logger)
    {
        _service = service;
        _messenger = messenger;
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
            var items = await _service.SearchArtistsAsync(SearchText, ct);
            Results.ReplaceRange(items);
            HasResults = Results.Count > 0;
            HasSearched = true;
            EmptyStateMessage = "No artists found";
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
    private void SelectResult(MusicSearchResultDto result)
    {
        _messenger.Send(new ArtistPickedMessage(result));
        Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private void Back() => Shell.Current.GoToAsync("..");
}
