namespace MyVocaList.Contracts.DTOs.Venues;

/// <summary>
/// A stored venue as the edit form loads it.
/// </summary>
/// <param name="Id">Venue id.</param>
/// <param name="Name">Venue name.</param>
/// <param name="ImageUrl">Image reference of the picture; null when none.</param>
/// <param name="Address">Stored address.</param>
/// <param name="Latitude">Stored latitude; null when none.</param>
/// <param name="Longitude">Stored longitude; null when none.</param>
public sealed record VenueEditDto(int Id, string Name, string? ImageUrl, VenueAddress Address, double? Latitude, double? Longitude);
