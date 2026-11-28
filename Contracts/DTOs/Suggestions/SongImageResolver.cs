namespace MyVocaList.Contracts.DTOs.Suggestions;

/// <summary>
/// Pure picture priority for a song: its own picture, the album cover, the artist picture, then the icon
/// (REQ-MSRC-16/18). The cover URL is built from the MusicBrainz release id with no call.
/// </summary>
public static class SongImageResolver
{
    private const string CoverArtBase = "https://coverartarchive.org/release/";
    private const string CoverArtSuffix = "/front-250";

    /// <summary>The Cover Art Archive 250 px front-cover URL of a release; null when the id is not a GUID.</summary>
    public static string? CoverArtUrl(string? releaseMbid) =>
        Guid.TryParse(releaseMbid, out var id) ? $"{CoverArtBase}{id:D}{CoverArtSuffix}" : null;

    /// <summary>The first available image reference: song, album cover, artist; null shows the icon.</summary>
    public static string? Resolve(string? songImage, string? releaseMbid, string? artistImage) =>
        songImage ?? CoverArtUrl(releaseMbid) ?? artistImage;
}
