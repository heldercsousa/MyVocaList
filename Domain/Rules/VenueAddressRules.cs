using MyVocaList.Contracts.DTOs.Venues;

namespace MyVocaList.Domain.Rules;

/// <summary>
/// Pure rules for the six-field address of a venue (REQ-VEN-12..16, 18, 24, 33). Trimming is input clean-up only;
/// letter case and accents are never changed here, comparison is left to the database collation.
/// </summary>
public static class VenueAddressRules
{
    public const int Line1Max = 100;
    public const int Line2Max = 100;
    public const int CityMax = 80;
    public const int RegionMax = 80;
    public const int PostalCodeMax = 20;
    public const int CountryMax = 60;

    private const string Separator = ", ";
    private const double MaxLatitude = 90;
    private const double MaxLongitude = 180;

    /// <summary>Trims each field; blank becomes null; a null address becomes <see cref="VenueAddress.Empty"/>.</summary>
    public static VenueAddress Normalize(VenueAddress? address)
    {
        if (address is null)
            return VenueAddress.Empty;

        return new VenueAddress(
            Clean(address.Line1),
            Clean(address.Line2),
            Clean(address.City),
            Clean(address.Region),
            Clean(address.PostalCode),
            Clean(address.Country));
    }

    /// <summary>True when all six fields are blank.</summary>
    public static bool IsEmpty(VenueAddress address)
        => Fields(address).All(f => Clean(f.Value) is null);

    /// <summary>
    /// Validates an address: an empty address is valid; otherwise Street address and City are required and each
    /// field must not exceed its maximum. The first failure in that order is returned.
    /// </summary>
    public static (bool isValid, string message, VenueField field) Validate(VenueAddress address)
    {
        if (IsEmpty(address))
            return (true, "", VenueField.None);

        if (Clean(address.Line1) is null)
            return (false, "Street address is required.", VenueField.AddressLine1);

        if (Clean(address.City) is null)
            return (false, "City is required.", VenueField.City);

        foreach (var field in Fields(address))
        {
            var value = Clean(field.Value);
            if (value is not null && value.Length > field.Max)
                return (false, $"{field.Label} is too long. Maximum {field.Max} characters.", field.Field);
        }

        return (true, "", VenueField.None);
    }

    /// <summary>Street address and City joined by ", " (blank parts skipped); null when both are blank.</summary>
    public static string? Summary(VenueAddress address)
        => Join(address.Line1, address.City);

    /// <summary>The non-blank fields in field order joined by ", "; null when the address is empty.</summary>
    public static string? ToSearchText(VenueAddress address)
        => Join(address.Line1, address.Line2, address.City, address.Region, address.PostalCode, address.Country);

    /// <summary>
    /// Returns both values when both are present, finite, latitude within -90..90 and longitude within -180..180;
    /// otherwise (null, null).
    /// </summary>
    public static (double? latitude, double? longitude) NormalizeCoordinates(double? latitude, double? longitude)
    {
        if (latitude is not { } lat || longitude is not { } lon)
            return (null, null);

        if (!double.IsFinite(lat) || !double.IsFinite(lon))
            return (null, null);

        if (Math.Abs(lat) > MaxLatitude || Math.Abs(lon) > MaxLongitude)
            return (null, null);

        return (lat, lon);
    }

    private static string? Clean(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? Join(params string?[] values)
    {
        var parts = values.Select(Clean).OfType<string>().ToList();
        return parts.Count == 0 ? null : string.Join(Separator, parts);
    }

    private static (VenueField Field, string Label, int Max, string? Value)[] Fields(VenueAddress address) =>
    [
        (VenueField.AddressLine1, "Street address", Line1Max, address.Line1),
        (VenueField.AddressLine2, "Address line 2", Line2Max, address.Line2),
        (VenueField.City, "City", CityMax, address.City),
        (VenueField.Region, "State / Province / Region", RegionMax, address.Region),
        (VenueField.PostalCode, "Postal code", PostalCodeMax, address.PostalCode),
        (VenueField.Country, "Country", CountryMax, address.Country),
    ];
}
