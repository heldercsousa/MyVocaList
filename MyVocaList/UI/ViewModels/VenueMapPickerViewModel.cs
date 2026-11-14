using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Maui.Controls;
using MyVocaList.Contracts.DTOs.Venues;
using MyVocaList.Domain.Rules;
using MyVocaList.Domain.ServicesInterfaces;
using MyVocaList.UI.Collections;
using MyVocaList.UI.Messages;
using MyVocaList.UI.Services;

namespace MyVocaList.UI.ViewModels;

/// <summary>
/// ViewModel for the venue map picker page (REQ-VEN-24..31, 35). The page forwards map taps and shows the state;
/// it holds no map control and requests no device location. The first view comes from
/// <see cref="VenuePlaceRules.FirstView"/>; lookups come from <see cref="IPlaceLookupService"/>.
/// </summary>
[QueryProperty(nameof(LatitudeRaw), "lat")]
[QueryProperty(nameof(LongitudeRaw), "lon")]
[QueryProperty(nameof(QueryRaw), "q")]
public sealed partial class VenueMapPickerViewModel : ViewModelBase
{
    private const int MinSearchLength = 3;
    private const double MaxLatitude = 90;
    private const double MaxLongitude = 180;
    private const string SelectedPointTitle = "Selected point";
    private const string SelectedPlaceTitle = "Selected place";
    private const string UsePlaceText = "Use this place";
    private const string UsePointText = "Use this point";
    private const string NoAddressText = "No address found here.";
    private const string UnavailableText = "Address lookup isn't available right now. You can type the address in the form.";

    private readonly IPlaceLookupService _placeLookup;
    private readonly IMapPickerPreferences _preferences;
    private readonly INetworkStatus _networkStatus;
    private readonly IMessenger _messenger;
    private readonly INavigationService _navigation;
    private readonly ILogger<VenueMapPickerViewModel> _logger;

    private double? _latitude;
    private double? _longitude;
    private string? _query;

    // Cancels the lookup of the previous tap (or of a selected result) when a newer one supersedes it.
    private CancellationTokenSource? _tapCts;

    public VenueMapPickerViewModel(
        IPlaceLookupService placeLookup,
        IMapPickerPreferences preferences,
        INetworkStatus networkStatus,
        IMessenger messenger,
        INavigationService navigation,
        ILogger<VenueMapPickerViewModel> logger)
    {
        _placeLookup = placeLookup;
        _preferences = preferences;
        _networkStatus = networkStatus;
        _messenger = messenger;
        _navigation = navigation;
        _logger = logger;

        SearchCommand = new AsyncRelayCommand(SearchAsync);
        ConfirmCommand = new AsyncRelayCommand(ConfirmAsync, () => CanConfirm);
        SelectResultCommand = new RelayCommand<VenuePlace>(SelectResult);
    }

    /// <summary>Raised when the page must move the map (first view, search result selection).</summary>
    public event EventHandler<MapView>? ViewRequested;

    // Shell passes query parameters as strings; they are parsed with the invariant culture (invalid → null).
    public string? LatitudeRaw { set => _latitude = ParseCoordinate(value, MaxLatitude); }
    public string? LongitudeRaw { set => _longitude = ParseCoordinate(value, MaxLongitude); }
    public string? QueryRaw { set => _query = string.IsNullOrWhiteSpace(value) ? null : value; }

    [ObservableProperty] private bool _isOffline;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _isSearching;
    [ObservableProperty] private bool _isLookingUp;
    [ObservableProperty] private bool _hasNoResults;
    // The results list is shown from this flag: the collection raises no Count change on ReplaceRange/ClearRange.
    [ObservableProperty] private bool _hasResults;
    [ObservableProperty] private VenuePlace? _selectedPlace;
    [ObservableProperty] private bool _isPanelVisible;
    [ObservableProperty] private string _panelTitle = string.Empty;
    [ObservableProperty] private string _panelText = string.Empty;
    [ObservableProperty] private string _confirmText = string.Empty;

    public ObservableRangeCollection<VenuePlace> Results { get; } = [];

    public bool CanConfirm => SelectedPlace is not null && !IsLookingUp;

    public string OfflineText => "The map needs an internet connection.";
    public string SearchPlaceholder => "Search a place or address";

    public IAsyncRelayCommand SearchCommand { get; }
    public IAsyncRelayCommand ConfirmCommand { get; }
    public IRelayCommand<VenuePlace> SelectResultCommand { get; }

    partial void OnSelectedPlaceChanged(VenuePlace? value) => RefreshConfirm();

    partial void OnIsLookingUpChanged(bool value) => RefreshConfirm();

    private void RefreshConfirm()
    {
        OnPropertyChanged(nameof(CanConfirm));
        ConfirmCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Called when the map reports ready. Offline: stops. Otherwise decides the first view (REQ-VEN-24) and raises
    /// <see cref="ViewRequested"/>. The address search runs only when there are no form coordinates.
    /// </summary>
    public async Task InitializeAsync()
    {
        IsOffline = !_networkStatus.HasInternetAccess;
        if (IsOffline)
            return;

        (double latitude, double longitude)? formCoordinates = null;
        if (_latitude is { } latitude && _longitude is { } longitude)
            formCoordinates = (latitude, longitude);

        VenuePlace? addressSearchHit = null;
        if (formCoordinates is null && _query is not null)
        {
            var search = await _placeLookup.SearchAsync(_query);
            if (search.Status == PlaceLookupStatus.Found && search.Places.Count > 0)
                addressSearchHit = search.Places[0];
        }

        var view = VenuePlaceRules.FirstView(formCoordinates, addressSearchHit, _preferences.LastPlace);
        ViewRequested?.Invoke(this, view);
    }

    /// <summary>Search on submit only (REQ-VEN-25): fewer than three characters sends nothing.</summary>
    private async Task SearchAsync()
    {
        var text = SearchText?.Trim() ?? string.Empty;
        if (text.Length < MinSearchLength)
            return;

        IsSearching = true;
        try
        {
            var result = await _placeLookup.SearchAsync(text);
            var places = result.Places;
            await RunOnUiThreadAsync(() =>
            {
                Results.ReplaceRange(places);
                HasResults = places.Count > 0;
                HasNoResults = result.Status == PlaceLookupStatus.Found && places.Count == 0;
                if (result.Status == PlaceLookupStatus.Unavailable)
                {
                    _logger.LogDebug("Place search unavailable");
                    SelectedPlace = null;
                    IsPanelVisible = true;
                    PanelTitle = string.Empty;
                    PanelText = UnavailableText;
                }
                return Task.CompletedTask;
            });
        }
        finally
        {
            IsSearching = false;
        }
    }

    /// <summary>Selecting a result centres the map on it; no reverse lookup is sent (REQ-VEN-25).</summary>
    private void SelectResult(VenuePlace? place)
    {
        if (place is null)
            return;

        CancelPendingTap();
        SelectedPlace = place;
        Results.ClearRange();
        HasResults = false;
        HasNoResults = false;
        ShowPlacePanel(place);
        ViewRequested?.Invoke(this, new MapView(place.Latitude, place.Longitude, VenuePlaceRules.PlaceZoom, true));
    }

    /// <summary>
    /// A map tap (REQ-VEN-26..28). A newer tap supersedes the pending one: the older answer is ignored.
    /// </summary>
    public async Task OnMapTappedAsync(double latitude, double longitude)
    {
        CancelPendingTap();
        var cts = new CancellationTokenSource();
        _tapCts = cts;
        var ct = cts.Token;

        Results.ClearRange();
        HasResults = false;
        HasNoResults = false;
        IsLookingUp = true;

        PlaceLookupResult result;
        try
        {
            result = await _placeLookup.ReverseAsync(latitude, longitude, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return;
        }

        if (ct.IsCancellationRequested)
            return;

        switch (result.Status)
        {
            case PlaceLookupStatus.Found when result.Place is { } place:
                SelectedPlace = place;
                ShowPlacePanel(place);
                break;

            case PlaceLookupStatus.NoAddress:
                SelectedPlace = result.Place ?? new VenuePlace(latitude, longitude, VenueAddress.Empty, null, string.Empty);
                ShowPanel(SelectedPointTitle, NoAddressText, UsePointText);
                break;

            default:
                _logger.LogDebug("Place lookup unavailable for a map tap");
                SelectedPlace = new VenuePlace(latitude, longitude, VenueAddress.Empty, null, string.Empty);
                ShowPanel(SelectedPointTitle, UnavailableText, UsePointText);
                break;
        }

        IsLookingUp = false;
    }

    /// <summary>Confirm (REQ-VEN-30): stores the last place, sends the one message, goes back.</summary>
    private async Task ConfirmAsync()
    {
        var place = SelectedPlace;
        if (place is null)
            return;

        _preferences.LastPlace = (place.Latitude, place.Longitude);
        _messenger.Send(new VenuePlacePickedMessage(place));
        await _navigation.GoBackAsync();
    }

    private void CancelPendingTap()
    {
        _tapCts?.Cancel();
        _tapCts = null;
        IsLookingUp = false;
    }

    private void ShowPlacePanel(VenuePlace place)
    {
        var title = string.IsNullOrWhiteSpace(place.Name) ? SelectedPlaceTitle : place.Name.Trim();
        ShowPanel(title, place.AddressLine, UsePlaceText);
    }

    private void ShowPanel(string title, string text, string confirm)
    {
        IsPanelVisible = true;
        PanelTitle = title;
        PanelText = text;
        ConfirmText = confirm;
    }

    private static double? ParseCoordinate(string? value, double limit) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
        && Math.Abs(number) <= limit
            ? number
            : null;
}
