namespace MyVocaList.Contracts.DTOs.Venues;

/// <summary>
/// The six address fields of a venue (REQ-VEN-12). Each field may be null or blank.
/// </summary>
public sealed record VenueAddress(string? Line1, string? Line2, string? City, string? Region, string? PostalCode, string? Country)
{
    /// <summary>An address with all six fields null.</summary>
    public static VenueAddress Empty { get; } = new(null, null, null, null, null, null);
}
