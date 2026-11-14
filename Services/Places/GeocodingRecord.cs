namespace MyVocaList.Services.Places;

/// <summary>
/// One raw place answer from the geocoding service, before mapping to a venue place.
/// <see cref="Address"/> is never null: it is empty when the answer has no address object.
/// Keys are kept exactly as the service returns them (for example <c>road</c>, <c>house_number</c>, <c>country_code</c>).
/// </summary>
public sealed record GeocodingRecord(
    double Latitude,
    double Longitude,
    string? Name,
    string? Category,
    string? DisplayName,
    IReadOnlyDictionary<string, string> Address);
