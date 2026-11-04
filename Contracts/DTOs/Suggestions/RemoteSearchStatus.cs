namespace MyVocaList.Contracts.DTOs.Suggestions;

/// <summary>
/// Outcome of one remote (Deezer) search query (REQ-SFT-06, REQ-SFT-53).
/// </summary>
public enum RemoteSearchStatus
{
    /// <summary>Deezer answered, including with zero results.</summary>
    Ok,

    /// <summary>The device reported no internet; Deezer was not called.</summary>
    Offline,

    /// <summary>Deezer was called and failed (no route, HTTP error, or timeout).</summary>
    Unavailable
}
