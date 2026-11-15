using System.Text.RegularExpressions;
using MyVocaList.Contracts.DTOs.Venues;
using MyVocaList.Domain.Rules;

namespace MyVocaList.Services.Places;

/// <summary>
/// Maps a raw geocoding record to a venue place (REQ-VEN-26, 30, 32). Pure: no I/O, no state.
/// Keys are read from <see cref="GeocodingRecord.Address"/>; a missing or white-space value counts as blank.
/// </summary>
public static class NominatimAddressMapper
{
    // REQ-VEN-22: the venue name maximum. Declared here on purpose; the Services-layer VenueService is not referenced.
    private const int VenueNameMax = 60;

    private static readonly HashSet<string> NumberFirstCountries = new(StringComparer.OrdinalIgnoreCase)
    {
        "us", "ca", "gb", "ie", "au", "nz", "fr", "lu", "za", "sg", "in",
    };

    private static readonly HashSet<string> CommaCountries = new(StringComparer.OrdinalIgnoreCase)
    {
        "br", "pt", "es", "it", "mx", "ar", "cl", "co", "pe", "uy",
    };

    private static readonly HashSet<string> VenueCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "amenity", "leisure", "tourism", "shop", "club", "craft", "office",
    };

    private const string CommonsPrefix = "File:";

    // REQ-VEN-37: a Wikidata id is Q followed by digits only.
    private static readonly Regex WikidataIdPattern = new("^Q[0-9]+$", RegexOptions.CultureInvariant);

    private static readonly string[] RoadKeys = ["road", "pedestrian", "footway"];
    private static readonly string[] Line2Keys = ["suburb", "neighbourhood", "quarter", "city_district"];
    private static readonly string[] CityKeys = ["city", "town", "village", "municipality", "hamlet", "county"];
    private static readonly string[] RegionKeys = ["state", "region", "state_district", "province"];

    /// <summary>Maps one geocoding answer to a venue place; every address value is cut to its field maximum.</summary>
    public static VenuePlace ToPlace(GeocodingRecord record)
    {
        var address = record.Address;

        var line1 = Cut(BuildStreet(address), VenueAddressRules.Line1Max);
        var line2 = Cut(FirstNonBlank(address, Line2Keys), VenueAddressRules.Line2Max);
        var city = Cut(FirstNonBlank(address, CityKeys), VenueAddressRules.CityMax);
        var region = Cut(FirstNonBlank(address, RegionKeys), VenueAddressRules.RegionMax);
        var postalCode = Cut(Value(address, "postcode"), VenueAddressRules.PostalCodeMax);
        var country = Cut(Value(address, "country"), VenueAddressRules.CountryMax);

        var venueAddress = new VenueAddress(line1, line2, city, region, postalCode, country);

        return new VenuePlace(
            record.Latitude,
            record.Longitude,
            venueAddress,
            BuildName(record),
            BuildAddressLine(line1, city, region, country, record.DisplayName),
            BuildPictureSource(record.ExtraTags));
    }

    /// <summary>
    /// REQ-VEN-37: picture references from the extra tags. Always returns a source; all three members are null when
    /// no tag is accepted. A tag that is present but invalid is rejected; it does not fall back to the other tag.
    /// </summary>
    private static VenuePictureSource BuildPictureSource(IReadOnlyDictionary<string, string>? tags)
    {
        if (tags is null)
            return new VenuePictureSource(null, null, null);

        var wikidataId = TagValue(tags, "brand:wikidata") ?? TagValue(tags, "wikidata");
        if (wikidataId is not null && !WikidataIdPattern.IsMatch(wikidataId))
            wikidataId = null;

        string? commonsFile = null;
        var commons = TagValue(tags, "wikimedia_commons");
        if (commons is not null && commons.StartsWith(CommonsPrefix, StringComparison.Ordinal))
            commonsFile = Trimmed(commons[CommonsPrefix.Length..]);

        var imageUrl = TagValue(tags, "image");
        if (imageUrl is not null && !(Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps))
            imageUrl = null;

        return new VenuePictureSource(wikidataId, commonsFile, imageUrl);
    }

    private static string? TagValue(IReadOnlyDictionary<string, string> tags, string key) =>
        tags.TryGetValue(key, out var raw) ? Trimmed(raw) : null;

    private static string? BuildStreet(IReadOnlyDictionary<string, string> address)
    {
        var road = FirstNonBlank(address, RoadKeys);
        if (road is null)
            return null;

        var number = Value(address, "house_number");
        if (number is null)
            return road;

        var countryCode = Value(address, "country_code");
        if (countryCode is not null && NumberFirstCountries.Contains(countryCode))
            return $"{number} {road}";

        if (countryCode is not null && CommaCountries.Contains(countryCode))
            return $"{road}, {number}";

        return $"{road} {number}";
    }

    private static string? BuildName(GeocodingRecord record)
    {
        var name = Trimmed(record.Name);
        if (name is null || record.Category is null || !VenueCategories.Contains(record.Category))
            return null;

        return Cut(name, VenueNameMax);
    }

    private static string BuildAddressLine(string? line1, string? city, string? region, string? country, string? displayName)
    {
        var parts = new[] { line1, city, region, country }.Where(part => part is not null).ToArray();
        if (parts.Length == 0)
            return Trimmed(displayName) ?? string.Empty;

        return string.Join(", ", parts);
    }

    private static string? FirstNonBlank(IReadOnlyDictionary<string, string> address, string[] keys)
    {
        foreach (var key in keys)
        {
            var value = Value(address, key);
            if (value is not null)
                return value;
        }

        return null;
    }

    private static string? Value(IReadOnlyDictionary<string, string> address, string key) =>
        address.TryGetValue(key, out var raw) ? Trimmed(raw) : null;

    private static string? Trimmed(string? raw) =>
        string.IsNullOrWhiteSpace(raw) ? null : raw.Trim();

    private static string? Cut(string? value, int maximum)
    {
        if (value is null)
            return null;

        var cut = value.Length > maximum ? value[..maximum] : value;
        return Trimmed(cut);
    }
}
