namespace MyVocaList.UI.Constants;

/// <summary>User-facing notes shared by the song and artist form view models for the remote-search status line.</summary>
public static class SearchNotes
{
    /// <summary>
    /// Shown when the remote music provider is unavailable (not offline); the suggestion list falls back to library results only.
    /// </summary>
    public const string Unavailable = "Online search isn't available right now — showing your library only.";

    /// <summary>Shown under a song-title field when neither the library nor the online search has a row for the query (REQ-ITS-08, supporting-text form).</summary>
    public const string NoResults = "No results";
}
