namespace MyVocaList.Contracts.DTOs.Venues;

/// <summary>
/// Input of a venue save: a new venue when <see cref="Id"/> is null, otherwise an update of that venue.
/// </summary>
/// <param name="Id">Id of the venue being edited; null in add mode.</param>
/// <param name="Name">Venue name as typed.</param>
/// <param name="Address">Address as typed; normalised by the service.</param>
/// <param name="Latitude">Latitude picked on the map; null when none.</param>
/// <param name="Longitude">Longitude picked on the map; null when none.</param>
/// <param name="ImageReference">Image reference of the picture chosen in the form; null when none.</param>
/// <param name="ImageChanged">True when the user picked or removed a picture in edit mode.</param>
public sealed record VenueSaveRequest(int? Id, string Name, VenueAddress Address,
    double? Latitude = null, double? Longitude = null, string? ImageReference = null, bool ImageChanged = false);
