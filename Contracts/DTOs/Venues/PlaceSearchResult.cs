namespace MyVocaList.Contracts.DTOs.Venues;

/// <summary>
/// Result of a search lookup (text to places).
/// </summary>
/// <param name="Status">Outcome of the lookup.</param>
/// <param name="Places">Places found, at most 5; empty when none.</param>
public sealed record PlaceSearchResult(PlaceLookupStatus Status, IReadOnlyList<VenuePlace> Places);
