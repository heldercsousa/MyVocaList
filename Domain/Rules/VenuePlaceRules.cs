using MyVocaList.Contracts.DTOs.Venues;

namespace MyVocaList.Domain.Rules;

/// <summary>Where the map picker opens and the first view of it (REQ-VEN-24).</summary>
public sealed record MapView(double Latitude, double Longitude, int Zoom, bool ShowMarker);

/// <summary>Rules for the map picker: the first view and merging a picked place into the form (REQ-VEN-24, 30).</summary>
public static class VenuePlaceRules
{
    public const int PlaceZoom = 17;
    public const int LastPlaceZoom = 15;
    public const int WorldZoom = 2;

    /// <summary>
    /// The first view: form coordinates, else the address search hit, else the last confirmed place, else the whole world.
    /// Only the first two show a marker.
    /// </summary>
    public static MapView FirstView(
        (double latitude, double longitude)? formCoordinates,
        VenuePlace? addressSearchHit,
        (double latitude, double longitude)? lastPlace)
    {
        if (formCoordinates is { } form)
            return new MapView(form.latitude, form.longitude, PlaceZoom, true);

        if (addressSearchHit is { } hit)
            return new MapView(hit.Latitude, hit.Longitude, PlaceZoom, true);

        if (lastPlace is { } last)
            return new MapView(last.latitude, last.longitude, LastPlaceZoom, false);

        return new MapView(0, 0, WorldZoom, false);
    }

    /// <summary>
    /// Merges a picked place into the form: coordinates always from the place; the address from the place when it has
    /// at least one non-blank field, else unchanged; the name from the place only when the current name is blank.
    /// </summary>
    public static (string name, VenueAddress address, double latitude, double longitude) Merge(
        string? currentName,
        VenueAddress currentAddress,
        VenuePlace place)
    {
        var address = place.Address is { } placeAddress && !VenueAddressRules.IsEmpty(placeAddress)
            ? VenueAddressRules.Normalize(placeAddress)
            : currentAddress;

        string name;
        if (!string.IsNullOrWhiteSpace(currentName))
            name = currentName;
        else
            name = string.IsNullOrWhiteSpace(place.Name) ? "" : place.Name.Trim();

        return (name, address, place.Latitude, place.Longitude);
    }
}
