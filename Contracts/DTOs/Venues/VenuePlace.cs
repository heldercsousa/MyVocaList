namespace MyVocaList.Contracts.DTOs.Venues;

/// <summary>
/// A point returned by the map picker: coordinates, the address found for it and a place name when known.
/// </summary>
/// <param name="Latitude">Latitude in decimal degrees (WGS 84).</param>
/// <param name="Longitude">Longitude in decimal degrees (WGS 84).</param>
/// <param name="Address">Address fields found for the point; each may be blank.</param>
/// <param name="Name">Place name; null when the point is not a known place.</param>
/// <param name="AddressLine">One-line address shown in the picker panel and result rows.</param>
public sealed record VenuePlace(double Latitude, double Longitude, VenueAddress Address, string? Name, string AddressLine);
