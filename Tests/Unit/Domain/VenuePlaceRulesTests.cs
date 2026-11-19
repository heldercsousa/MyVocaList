using MyVocaList.Contracts.DTOs.Venues;
using MyVocaList.Domain.Rules;

namespace MyVocaList.Tests.Unit.Domain;

public class VenuePlaceRulesTests
{
    private static readonly VenueAddress PlaceAddress = new("Rua Augusta, 100", "Loja 2", "São Paulo", "SP", "01304-000", "Brasil");
    private static readonly VenueAddress CurrentAddress = new("Av. Paulista, 1", null, "São Paulo", null, null, "Brasil");

    private static VenuePlace Place(string? name = "Bar Brahma", VenueAddress? address = null)
        => new(-23.5505, -46.6333, address ?? PlaceAddress, name, "Rua Augusta, 100, São Paulo, SP, Brasil");

    // [AC] REQ-VEN-24: the picker centres on the form's coordinates first, at zoom 17 with a marker.
    [Fact]
    public void FirstView_FormCoordinatesGiven_CentresOnThemWithMarkerAtZoom17()
    {
        var view = VenuePlaceRules.FirstView((-10.5, -40.25), Place(), (1.0, 2.0));

        Assert.Equal(new MapView(-10.5, -40.25, 17, true), view);
    }

    // [AC] REQ-VEN-24: without form coordinates, the first address search hit is the centre, at zoom 17 with a marker.
    [Fact]
    public void FirstView_NoFormCoordinatesButSearchHit_CentresOnHitWithMarkerAtZoom17()
    {
        var view = VenuePlaceRules.FirstView(null, Place(), (1.0, 2.0));

        Assert.Equal(new MapView(-23.5505, -46.6333, 17, true), view);
    }

    // [AC] REQ-VEN-24: without a search hit, the last confirmed place is the centre, at zoom 15 without a marker.
    [Fact]
    public void FirstView_OnlyLastPlace_CentresOnItAtZoom15WithoutMarker()
    {
        var view = VenuePlaceRules.FirstView(null, null, (1.0, 2.0));

        Assert.Equal(new MapView(1.0, 2.0, 15, false), view);
    }

    // [AC] REQ-VEN-24: with nothing known, the whole world is shown at zoom 2 without a marker.
    [Fact]
    public void FirstView_NothingKnown_ShowsWholeWorldAtZoom2WithoutMarker()
    {
        var view = VenuePlaceRules.FirstView(null, null, null);

        Assert.Equal(new MapView(0, 0, 2, false), view);
    }

    // [AC] REQ-VEN-30: a place with an address replaces all six address fields.
    [Fact]
    public void Merge_PlaceHasAddress_ReplacesAddressWithPlaceAddress()
    {
        var result = VenuePlaceRules.Merge("Typed name", CurrentAddress, Place());

        Assert.Equal(PlaceAddress, result.address);
    }

    // [AC] REQ-VEN-30: a place address field the place lacks is blanked, not kept from the form.
    [Fact]
    public void Merge_PlaceAddressHasBlankFields_BlanksThoseFieldsInForm()
    {
        var placeAddress = new VenueAddress("Rua Augusta, 100", null, "São Paulo", "  ", null, null);

        var result = VenuePlaceRules.Merge("Typed name", CurrentAddress, Place(address: placeAddress));

        Assert.Equal(new VenueAddress("Rua Augusta, 100", null, "São Paulo", null, null, null), result.address);
    }

    // [AC] REQ-VEN-30: a place without any address field leaves the form's address unchanged.
    [Fact]
    public void Merge_PlaceHasNoAddress_KeepsCurrentAddress()
    {
        var result = VenuePlaceRules.Merge("Typed name", CurrentAddress, Place(address: VenueAddress.Empty with { Region = " " }));

        Assert.Equal(CurrentAddress, result.address);
    }

    // [AC] REQ-VEN-30: the coordinates always come from the place.
    [Fact]
    public void Merge_AlwaysTakesCoordinatesFromPlace()
    {
        var result = VenuePlaceRules.Merge("Typed name", CurrentAddress, Place(address: VenueAddress.Empty));

        Assert.Equal((-23.5505, -46.6333), (result.latitude, result.longitude));
    }

    // [AC] REQ-VEN-30: a blank form name becomes the place name.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Merge_BlankCurrentName_TakesPlaceName(string? currentName)
    {
        var result = VenuePlaceRules.Merge(currentName, CurrentAddress, Place("Bar Brahma"));

        Assert.Equal("Bar Brahma", result.name);
    }

    // [AC] REQ-VEN-30: a non-blank form name is never changed by a pick.
    [Fact]
    public void Merge_TypedCurrentName_IsKept()
    {
        var result = VenuePlaceRules.Merge("My own name", CurrentAddress, Place("Bar Brahma"));

        Assert.Equal("My own name", result.name);
    }

    // [AC] REQ-VEN-30: a place without a name leaves a blank form name blank, as an empty string.
    [Fact]
    public void Merge_PlaceWithoutName_ReturnsEmptyNameWhenFormNameBlank()
    {
        var result = VenuePlaceRules.Merge(null, CurrentAddress, Place(name: null));

        Assert.Equal("", result.name);
    }
}
