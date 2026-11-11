namespace MyVocaList.UI.Constants;

/// <summary>User-facing notes shared by the song and artist form view models for the remote-search status line.</summary>
public static class SearchNotes
{
    /// <summary>
    /// Shown when the remote music provider is unavailable (not offline); the suggestion list falls back to library results only.
    /// </summary>
    public const string Unavailable = "Online search isn't available right now — showing your library only.";
}
