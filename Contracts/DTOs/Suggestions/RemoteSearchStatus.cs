namespace MyVocaList.Contracts.DTOs.Suggestions;

/// <summary>
/// Outcome of one remote (MusicBrainz) search query (REQ-SFT-06, REQ-SFT-53).
/// </summary>
public enum RemoteSearchStatus
{
    /// <summary>The provider answered, including with zero results.</summary>
    Ok,

    /// <summary>The device reported no internet; the provider was not called.</summary>
    Offline,

    /// <summary>The provider was called and failed (no route, HTTP error, or timeout).</summary>
    Unavailable
}
