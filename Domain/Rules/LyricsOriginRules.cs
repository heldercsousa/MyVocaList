using MyVocaList.Contracts.DTOs;
using MyVocaList.Contracts.Enums;

namespace MyVocaList.Domain.Rules;

/// <summary>
/// Lyrics origin rules (REQ-SLD-19, 24, 26, 27). Lyrics are compared ordinally on trimmed values; null, empty and
/// white-space text are all blank and equal to each other.
/// </summary>
public static class LyricsOriginRules
{
    /// <summary>
    /// Origin of a new song on Save (REQ-SLD-26), rules tried in order: lyrics equal to the Found text of the pick
    /// lookup give <see cref="LyricsOrigin.LyricsSource"/>; other non-blank lyrics give <see cref="LyricsOrigin.User"/>;
    /// blank lyrics with a NoMatch pick lookup give <see cref="LyricsOrigin.NoneFound"/>; otherwise null (not recorded).
    /// </summary>
    /// <param name="savedLyrics">The lyrics being saved.</param>
    /// <param name="pickLookup">The pick lookup of the held pick, or null when there is none.</param>
    public static LyricsOrigin? OnCreate(string? savedLyrics, LyricsLookupResult? pickLookup)
    {
        var saved = Normalize(savedLyrics);

        if (saved.Length > 0)
        {
            if (pickLookup is { Status: LyricsLookupStatus.Found } found
                && string.Equals(saved, Normalize(found.Lyrics), StringComparison.Ordinal))
            {
                return LyricsOrigin.LyricsSource;
            }

            return LyricsOrigin.User;
        }

        return pickLookup is { Status: LyricsLookupStatus.NoMatch } ? LyricsOrigin.NoneFound : null;
    }

    /// <summary>
    /// Origin of a stored song on Save (REQ-SLD-27): changed lyrics (including emptied) give
    /// <see cref="LyricsOrigin.User"/>; unchanged lyrics keep the stored origin.
    /// </summary>
    /// <param name="storedLyrics">The lyrics stored at that moment.</param>
    /// <param name="storedOrigin">The origin stored at that moment; null = not recorded.</param>
    /// <param name="savedLyrics">The lyrics being saved.</param>
    public static LyricsOrigin? OnUpdate(string? storedLyrics, LyricsOrigin? storedOrigin, string? savedLyrics)
        => string.Equals(Normalize(storedLyrics), Normalize(savedLyrics), StringComparison.Ordinal)
            ? storedOrigin
            : LyricsOrigin.User;

    /// <summary>
    /// True when opening the song starts a lookup on open (REQ-SLD-19, 24): a music-database song with blank lyrics and no
    /// recorded origin.
    /// </summary>
    /// <param name="externalProvider">The song's music-database provider.</param>
    /// <param name="externalId">The song's music-database id.</param>
    /// <param name="lyrics">The stored lyrics.</param>
    /// <param name="origin">The stored origin; null = not recorded.</param>
    public static bool NeedsLookupOnOpen(string? externalProvider, string? externalId, string? lyrics, LyricsOrigin? origin)
        => SongIdentityRules.IsRemoteSong(externalProvider, externalId)
            && string.IsNullOrWhiteSpace(lyrics)
            && origin is null;

    private static string Normalize(string? lyrics)
        => string.IsNullOrWhiteSpace(lyrics) ? string.Empty : lyrics.Trim();
}
