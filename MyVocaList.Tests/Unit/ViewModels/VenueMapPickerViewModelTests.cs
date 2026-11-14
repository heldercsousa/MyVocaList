using System.Globalization;
using CommunityToolkit.Mvvm.Messaging;
using MyVocaList.Contracts.DTOs.Venues;
using MyVocaList.Domain.Rules;
using MyVocaList.Domain.ServicesInterfaces;
using MyVocaList.UI.Messages;
using MyVocaList.UI.Services;
using MyVocaList.UI.ViewModels;

namespace MyVocaList.Tests.Unit.ViewModels;

/// <summary>
/// Level A tests for the venue map picker ViewModel (V8). No map control is involved: the page only forwards
/// taps and shows the state. Requirement IDs REQ-VEN-24..31 and 35 come from the venues change requirements.
/// </summary>
public class VenueMapPickerViewModelTests
{
    private const double TappedLatitude = -23.55;
    private const double TappedLongitude = -46.63;

    private static VenuePlace MakePlace(
        string? name = "Bar Brahma",
        string addressLine = "Av. Vieira Souto, 1, Sao Paulo, Brazil",
        double latitude = -23.5505,
        double longitude = -46.6333) =>
        new(latitude, longitude,
            new VenueAddress("Av. Vieira Souto, 1", null, "Sao Paulo", "SP", "01000-000", "Brazil"),
            name, addressLine);

    private static VenuePlace MakeNoAddressPlace(double latitude, double longitude) =>
        new(latitude, longitude, VenueAddress.Empty, null, string.Empty);

    private sealed class FakeMapPickerPreferences : IMapPickerPreferences
    {
        public (double latitude, double longitude)? LastPlace { get; set; }
    }

    private sealed class Harness
    {
        public Mock<IPlaceLookupService> Lookup { get; } = new();
        public FakeMapPickerPreferences Preferences { get; } = new();
        public Mock<INetworkStatus> Network { get; } = new();
        public Mock<INavigationService> Navigation { get; } = new();
        public WeakReferenceMessenger Messenger { get; } = new();
        public List<VenuePlacePickedMessage> Sent { get; } = [];
        public List<MapView> Views { get; } = [];
        private readonly object _recipient = new();
        public VenueMapPickerViewModel Sut { get; }

        public Harness()
        {
            Network.SetupGet(n => n.HasInternetAccess).Returns(true);
            Messenger.Register<VenuePlacePickedMessage>(_recipient, (_, m) => Sent.Add(m));
            Sut = new VenueMapPickerViewModel(
                Lookup.Object,
                Preferences,
                Network.Object,
                Messenger,
                Navigation.Object,
                new Mock<ILogger<VenueMapPickerViewModel>>().Object);
            Sut.ViewRequested += (_, view) => Views.Add(view);
        }
    }

    // ── REQ-VEN-24: first view ──────────────────────────────────────────────

    // [AC] REQ-VEN-24: The picker opens at the form's coordinates, zoom 17, with a marker, and sends no search.
    [Fact]
    public async Task InitializeAsync_FormCoordinates_CentresOnThemWithMarkerAndNoSearch()
    {
        var h = new Harness();
        h.Sut.LatitudeRaw = "-23.5505";
        h.Sut.LongitudeRaw = "-46.6333";

        await h.Sut.InitializeAsync();

        var view = Assert.Single(h.Views);
        Assert.Equal(new MapView(-23.5505, -46.6333, 17, true), view);
        h.Lookup.Verify(s => s.SearchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // [AC] REQ-VEN-24: Without form coordinates, a non-blank address is searched once and the first hit is the view (zoom 17, marker).
    [Fact]
    public async Task InitializeAsync_OnlyQuery_SearchesOnceAndCentresOnFirstHit()
    {
        var h = new Harness();
        h.Sut.QueryRaw = "Bar Brahma Sao Paulo";
        h.Lookup.Setup(s => s.SearchAsync("Bar Brahma Sao Paulo", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PlaceSearchResult(PlaceLookupStatus.Found, [MakePlace(latitude: -23.1, longitude: -46.1)]));

        await h.Sut.InitializeAsync();

        h.Lookup.Verify(s => s.SearchAsync("Bar Brahma Sao Paulo", It.IsAny<CancellationToken>()), Times.Once);
        var view = Assert.Single(h.Views);
        Assert.Equal(new MapView(-23.1, -46.1, 17, true), view);
    }

    // [AC] REQ-VEN-24: When the address search finds nothing, the last confirmed place applies (zoom 15, no marker).
    [Fact]
    public async Task InitializeAsync_SearchFindsNothing_FallsBackToLastPlace()
    {
        var h = new Harness();
        h.Sut.QueryRaw = "Nowhere";
        h.Preferences.LastPlace = (-22.9, -43.2);
        h.Lookup.Setup(s => s.SearchAsync("Nowhere", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PlaceSearchResult(PlaceLookupStatus.Found, []));

        await h.Sut.InitializeAsync();

        var view = Assert.Single(h.Views);
        Assert.Equal(new MapView(-22.9, -43.2, 15, false), view);
    }

    // [AC] REQ-VEN-24: When the address search is unavailable, the last confirmed place applies (zoom 15, no marker).
    [Fact]
    public async Task InitializeAsync_SearchUnavailable_FallsBackToLastPlace()
    {
        var h = new Harness();
        h.Sut.QueryRaw = "Rua X";
        h.Preferences.LastPlace = (-22.9, -43.2);
        h.Lookup.Setup(s => s.SearchAsync("Rua X", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PlaceSearchResult(PlaceLookupStatus.Unavailable, []));

        await h.Sut.InitializeAsync();

        var view = Assert.Single(h.Views);
        Assert.Equal(new MapView(-22.9, -43.2, 15, false), view);
    }

    // [AC] REQ-VEN-24: With nothing known, the picker shows the whole world (zoom 2, no marker).
    [Fact]
    public async Task InitializeAsync_NothingKnown_ShowsWorld()
    {
        var h = new Harness();

        await h.Sut.InitializeAsync();

        var view = Assert.Single(h.Views);
        Assert.Equal(new MapView(0, 0, 2, false), view);
        h.Lookup.Verify(s => s.SearchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // [AC] REQ-VEN-24: Coordinates in the query are read with the invariant culture, whatever the current culture is.
    [Fact]
    public async Task InitializeAsync_CoordinatesParsedWithInvariantCulture_UnderPortugueseBrazil()
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("pt-BR");
        try
        {
            var h = new Harness();
            h.Sut.LatitudeRaw = "-23.5505";
            h.Sut.LongitudeRaw = "-46.6333";

            await h.Sut.InitializeAsync();

            var view = Assert.Single(h.Views);
            Assert.Equal(new MapView(-23.5505, -46.6333, 17, true), view);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    // ── REQ-VEN-29: offline ─────────────────────────────────────────────────

    // [AC] REQ-VEN-29: Offline when the picker opens → IsOffline is true, no lookup is sent and no view is requested.
    [Fact]
    public async Task InitializeAsync_Offline_ShowsOfflineAndSendsNothing()
    {
        var h = new Harness();
        h.Network.SetupGet(n => n.HasInternetAccess).Returns(false);
        h.Sut.LatitudeRaw = "-23.5505";
        h.Sut.LongitudeRaw = "-46.6333";
        h.Sut.QueryRaw = "Bar Brahma";

        await h.Sut.InitializeAsync();

        Assert.True(h.Sut.IsOffline);
        Assert.Empty(h.Views);
        h.Lookup.Verify(s => s.SearchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        h.Lookup.Verify(s => s.ReverseAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── REQ-VEN-25: search ──────────────────────────────────────────────────

    // [AC] REQ-VEN-25: Text of fewer than 3 characters sends no search.
    [Fact]
    public async Task SearchCommand_TwoCharacters_SendsNothing()
    {
        var h = new Harness();
        h.Sut.SearchText = "ab";

        await h.Sut.SearchCommand.ExecuteAsync(null);

        h.Lookup.Verify(s => s.SearchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // [AC] REQ-VEN-25: Submitted text of 3 or more characters is searched trimmed, and the results fill the list.
    [Fact]
    public async Task SearchCommand_ThreeCharacters_SearchesTrimmedTextAndFillsResults()
    {
        var h = new Harness();
        h.Sut.SearchText = "  Bar  ";
        h.Lookup.Setup(s => s.SearchAsync("Bar", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PlaceSearchResult(PlaceLookupStatus.Found,
                    [MakePlace("Bar A"), MakePlace("Bar B", "Rua B, 2")]));

        await h.Sut.SearchCommand.ExecuteAsync(null);

        h.Lookup.Verify(s => s.SearchAsync("Bar", It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(2, h.Sut.Results.Count);
        Assert.False(h.Sut.HasNoResults);
        Assert.False(h.Sut.IsSearching);
    }

    // [AC] REQ-VEN-25: The results list is shown while a search has places and hidden once one is selected.
    [Fact]
    public async Task SearchCommand_WithPlaces_ShowsResultsUntilOneIsSelected()
    {
        var h = new Harness();
        var place = MakePlace("Bar A");
        h.Sut.SearchText = "Bar";
        h.Lookup.Setup(s => s.SearchAsync("Bar", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PlaceSearchResult(PlaceLookupStatus.Found, [place]));

        await h.Sut.SearchCommand.ExecuteAsync(null);
        var shownAfterSearch = h.Sut.HasResults;
        h.Sut.SelectResultCommand.Execute(place);

        Assert.True(shownAfterSearch);
        Assert.False(h.Sut.HasResults);
    }

    // [AC] REQ-VEN-25: A search that finds nothing sets HasNoResults (the page shows "No place found.").
    [Fact]
    public async Task SearchCommand_NoPlaces_SetsHasNoResults()
    {
        var h = new Harness();
        h.Sut.SearchText = "Zzzz";
        h.Lookup.Setup(s => s.SearchAsync("Zzzz", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PlaceSearchResult(PlaceLookupStatus.Found, []));

        await h.Sut.SearchCommand.ExecuteAsync(null);

        Assert.Empty(h.Sut.Results);
        Assert.True(h.Sut.HasNoResults);
    }

    // [AC] REQ-VEN-25: Selecting a result centres the map (zoom 17, marker), clears the list, shows the panel and sends no reverse lookup.
    [Fact]
    public async Task SelectResultCommand_SelectsCentresWithoutReverseLookup()
    {
        var h = new Harness();
        var place = MakePlace();
        h.Sut.SearchText = "Bar";
        h.Lookup.Setup(s => s.SearchAsync("Bar", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PlaceSearchResult(PlaceLookupStatus.Found, [place]));
        await h.Sut.SearchCommand.ExecuteAsync(null);

        h.Sut.SelectResultCommand.Execute(place);

        Assert.Equal(place, h.Sut.SelectedPlace);
        Assert.Empty(h.Sut.Results);
        Assert.False(h.Sut.HasNoResults);
        Assert.Equal(new MapView(place.Latitude, place.Longitude, 17, true), Assert.Single(h.Views));
        Assert.True(h.Sut.IsPanelVisible);
        Assert.Equal("Bar Brahma", h.Sut.PanelTitle);
        Assert.Equal(place.AddressLine, h.Sut.PanelText);
        Assert.Equal("Use this place", h.Sut.ConfirmText);
        h.Lookup.Verify(s => s.ReverseAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── REQ-VEN-26: tap selects a point ─────────────────────────────────────

    // [AC] REQ-VEN-26: A tap with a known place shows its name, its address line and "Use this place".
    [Fact]
    public async Task OnMapTappedAsync_KnownPlace_ShowsNameAddressAndUseThisPlace()
    {
        var h = new Harness();
        var place = MakePlace();
        h.Lookup.Setup(s => s.ReverseAsync(TappedLatitude, TappedLongitude, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PlaceLookupResult(PlaceLookupStatus.Found, place));

        await h.Sut.OnMapTappedAsync(TappedLatitude, TappedLongitude);

        Assert.Equal("Bar Brahma", h.Sut.PanelTitle);
        Assert.Equal(place.AddressLine, h.Sut.PanelText);
        Assert.Equal("Use this place", h.Sut.ConfirmText);
        Assert.Equal(place, h.Sut.SelectedPlace);
        Assert.True(h.Sut.IsPanelVisible);
        Assert.False(h.Sut.IsLookingUp);
    }

    // [AC] REQ-VEN-26: A found place without a name shows "Selected place" as the panel title.
    [Fact]
    public async Task OnMapTappedAsync_FoundPlaceWithoutName_TitleIsSelectedPlace()
    {
        var h = new Harness();
        var place = MakePlace(name: null, addressLine: "Rua das Flores, 10, Sao Paulo");
        h.Lookup.Setup(s => s.ReverseAsync(TappedLatitude, TappedLongitude, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PlaceLookupResult(PlaceLookupStatus.Found, place));

        await h.Sut.OnMapTappedAsync(TappedLatitude, TappedLongitude);

        Assert.Equal("Selected place", h.Sut.PanelTitle);
        Assert.Equal("Rua das Flores, 10, Sao Paulo", h.Sut.PanelText);
        Assert.Equal("Use this place", h.Sut.ConfirmText);
    }

    // ── REQ-VEN-27: lookup with no address ──────────────────────────────────

    // [AC] REQ-VEN-27: A tap with no address shows "No address found here.", "Use this point", and selects the tapped coordinates only.
    [Fact]
    public async Task OnMapTappedAsync_NoAddress_ShowsNoAddressAndSelectsTappedCoordinates()
    {
        var h = new Harness();
        h.Lookup.Setup(s => s.ReverseAsync(TappedLatitude, TappedLongitude, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PlaceLookupResult(PlaceLookupStatus.NoAddress,
                    MakeNoAddressPlace(TappedLatitude, TappedLongitude)));

        await h.Sut.OnMapTappedAsync(TappedLatitude, TappedLongitude);

        Assert.Equal("Selected point", h.Sut.PanelTitle);
        Assert.Equal("No address found here.", h.Sut.PanelText);
        Assert.Equal("Use this point", h.Sut.ConfirmText);
        Assert.NotNull(h.Sut.SelectedPlace);
        Assert.Equal(TappedLatitude, h.Sut.SelectedPlace!.Latitude);
        Assert.Equal(TappedLongitude, h.Sut.SelectedPlace.Longitude);
    }

    // ── REQ-VEN-28: lookup unavailable ──────────────────────────────────────

    // [AC] REQ-VEN-28: A failed tap lookup shows the unavailable text, "Use this point", and selects the tapped point with an empty address.
    [Fact]
    public async Task OnMapTappedAsync_Unavailable_ShowsUnavailableTextAndSelectsTappedPoint()
    {
        var h = new Harness();
        h.Lookup.Setup(s => s.ReverseAsync(TappedLatitude, TappedLongitude, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PlaceLookupResult(PlaceLookupStatus.Unavailable, null));

        await h.Sut.OnMapTappedAsync(TappedLatitude, TappedLongitude);

        Assert.Equal("Address lookup isn't available right now. You can type the address in the form.", h.Sut.PanelText);
        Assert.Equal("Use this point", h.Sut.ConfirmText);
        Assert.Equal(new VenuePlace(TappedLatitude, TappedLongitude, VenueAddress.Empty, null, ""), h.Sut.SelectedPlace);
        Assert.True(h.Sut.CanConfirm);
    }

    // [AC] REQ-VEN-28: A search that is unavailable shows the unavailable text and selects no place.
    [Fact]
    public async Task SearchCommand_Unavailable_ShowsUnavailableTextAndNoSelection()
    {
        var h = new Harness();
        h.Sut.SearchText = "Bar";
        h.Lookup.Setup(s => s.SearchAsync("Bar", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PlaceSearchResult(PlaceLookupStatus.Unavailable, []));

        await h.Sut.SearchCommand.ExecuteAsync(null);

        Assert.Empty(h.Sut.Results);
        Assert.False(h.Sut.HasNoResults);
        Assert.True(h.Sut.IsPanelVisible);
        Assert.Equal("Address lookup isn't available right now. You can type the address in the form.", h.Sut.PanelText);
        Assert.Null(h.Sut.SelectedPlace);
    }

    // [AC] REQ-VEN-28: The picker stays usable: a second tap while the first lookup is pending wins, and the late first answer is ignored.
    [Fact]
    public async Task OnMapTappedAsync_SecondTapWhileFirstPending_IgnoresLateFirstAnswer()
    {
        var h = new Harness();
        var firstAnswer = new TaskCompletionSource<PlaceLookupResult>();
        var secondPlace = MakePlace(name: "Second place", addressLine: "Rua Dois, 2");
        h.Lookup.Setup(s => s.ReverseAsync(1, 1, It.IsAny<CancellationToken>())).Returns(firstAnswer.Task);
        h.Lookup.Setup(s => s.ReverseAsync(2, 2, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PlaceLookupResult(PlaceLookupStatus.Found, secondPlace));

        var first = h.Sut.OnMapTappedAsync(1, 1);
        await h.Sut.OnMapTappedAsync(2, 2);
        firstAnswer.SetResult(new PlaceLookupResult(PlaceLookupStatus.Found, MakePlace(name: "First place")));
        await first;

        Assert.Equal(secondPlace, h.Sut.SelectedPlace);
        Assert.Equal("Second place", h.Sut.PanelTitle);
        Assert.False(h.Sut.IsLookingUp);
    }

    // ── REQ-VEN-30: confirm fills the form ──────────────────────────────────

    // [AC] REQ-VEN-30: Confirm stores the last place, sends exactly one VenuePlacePickedMessage with the selected place, and goes back.
    [Fact]
    public async Task ConfirmCommand_StoresLastPlaceSendsOneMessageAndGoesBack()
    {
        var h = new Harness();
        var place = MakePlace();
        h.Lookup.Setup(s => s.ReverseAsync(TappedLatitude, TappedLongitude, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PlaceLookupResult(PlaceLookupStatus.Found, place));
        await h.Sut.OnMapTappedAsync(TappedLatitude, TappedLongitude);

        await h.Sut.ConfirmCommand.ExecuteAsync(null);

        Assert.Equal((place.Latitude, place.Longitude), h.Preferences.LastPlace);
        var sent = Assert.Single(h.Sent);
        Assert.Equal(place, sent.Place);
        h.Navigation.Verify(n => n.GoBackAsync(), Times.Once);
    }

    // [AC] REQ-VEN-30: Confirm cannot execute before a place is selected, nor while a lookup is running.
    [Fact]
    public async Task ConfirmCommand_CannotExecuteWithoutPlaceOrWhileLookingUp()
    {
        var h = new Harness();
        Assert.False(h.Sut.ConfirmCommand.CanExecute(null));

        var pending = new TaskCompletionSource<PlaceLookupResult>();
        h.Lookup.Setup(s => s.ReverseAsync(TappedLatitude, TappedLongitude, It.IsAny<CancellationToken>()))
                .Returns(pending.Task);

        var tap = h.Sut.OnMapTappedAsync(TappedLatitude, TappedLongitude);
        Assert.False(h.Sut.ConfirmCommand.CanExecute(null));

        pending.SetResult(new PlaceLookupResult(PlaceLookupStatus.NoAddress,
            MakeNoAddressPlace(TappedLatitude, TappedLongitude)));
        await tap;

        Assert.True(h.Sut.ConfirmCommand.CanExecute(null));
    }

    // ── REQ-VEN-31: back changes nothing ────────────────────────────────────

    // [AC] REQ-VEN-31: After a tap and without confirm, nothing is sent, stored or navigated.
    [Fact]
    public async Task OnMapTappedAsync_WithoutConfirm_SendsNoMessage()
    {
        var h = new Harness();
        h.Lookup.Setup(s => s.ReverseAsync(TappedLatitude, TappedLongitude, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PlaceLookupResult(PlaceLookupStatus.Found, MakePlace()));

        await h.Sut.OnMapTappedAsync(TappedLatitude, TappedLongitude);

        Assert.Empty(h.Sent);
        Assert.Null(h.Preferences.LastPlace);
        h.Navigation.Verify(n => n.GoBackAsync(), Times.Never);
    }
}
