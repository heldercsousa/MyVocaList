namespace MyVocaList.Contracts.DTOs.Venues;

/// <summary>
/// Outcome of one address-service lookup (REQ-VEN-25, 27, 28).
/// </summary>
public enum PlaceLookupStatus
{
    /// <summary>The service answered with a place or a list of places.</summary>
    Found,

    /// <summary>The service answered but found no address for the point.</summary>
    NoAddress,

    /// <summary>The lookup failed, timed out or was not possible (offline).</summary>
    Unavailable
}
