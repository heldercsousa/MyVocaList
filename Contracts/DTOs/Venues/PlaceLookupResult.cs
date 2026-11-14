namespace MyVocaList.Contracts.DTOs.Venues;

/// <summary>
/// Result of a reverse lookup (coordinates to place).
/// </summary>
/// <param name="Status">Outcome of the lookup.</param>
/// <param name="Place">The place found; null unless the status is <see cref="PlaceLookupStatus.Found"/> or <see cref="PlaceLookupStatus.NoAddress"/>.</param>
public sealed record PlaceLookupResult(PlaceLookupStatus Status, VenuePlace? Place);
