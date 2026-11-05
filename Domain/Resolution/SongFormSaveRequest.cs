namespace MyVocaList.Domain.Resolution;

/// <summary>
/// Everything the Song form hands to <c>ISongService.SaveFromFormAsync</c> (REQ-SFT-51). The form
/// collects input and the user's sheet choices; every Save decision is taken by the Service.
/// </summary>
/// <param name="SongId">Id of the song being edited; <c>null</c> = add mode.</param>
/// <param name="Title">Song title as typed or filled by a pick.</param>
/// <param name="Version">Variant label; empty string = canonical version.</param>
/// <param name="FeaturedArtists">Optional featured-artist names.</param>
/// <param name="Lyrics">Optional lyrics text.</param>
/// <param name="ArtistId">Picked or stored local artist; <c>null</c> when only text is known.</param>
/// <param name="ArtistText">Typed or Deezer-filled artist name, used when <paramref name="ArtistId"/> is <c>null</c>.</param>
/// <param name="ExternalProvider">Music-database provider of the song's identity (e.g. "Deezer").</param>
/// <param name="ExternalId">Music-database id of the song.</param>
/// <param name="PendingUrls">Karaoke URLs to attach when the song is created.</param>
/// <param name="ArtistDecision">The user's answer to the near-duplicate artist sheet, if any.</param>
/// <param name="SongDecision">The user's answer to the duplicate-song sheet, if any.</param>
public sealed record SongFormSaveRequest(
    int? SongId,
    string Title, string Version, string? FeaturedArtists, string? Lyrics,
    int? ArtistId,
    string? ArtistText,
    string? ExternalProvider, string? ExternalId,
    IReadOnlyList<string> PendingUrls,
    ArtistDecision ArtistDecision,
    SongDecision SongDecision);

/// <summary>The user's answer to the near-duplicate artist sheet (REQ-SFT-19).</summary>
public abstract record ArtistDecision
{
    /// <summary>No choice made yet: the Service resolves the typed artist name.</summary>
    public sealed record None : ArtistDecision;

    /// <summary>"Use '&lt;existing&gt;'": save with this existing artist, no resolution.</summary>
    public sealed record UseExisting(int ArtistId) : ArtistDecision;

    /// <summary>"Create '&lt;typed&gt;'": create the typed artist, no resolution.</summary>
    public sealed record CreateTyped : ArtistDecision;
}

/// <summary>The user's answer to the duplicate-song sheet (REQ-SFT-22/23).</summary>
public enum SongDecision
{
    /// <summary>No choice made yet: the song guard runs in add mode.</summary>
    None,

    /// <summary>Save as a new version of an existing song; a non-empty Version is required.</summary>
    SaveAsNewVersion,

    /// <summary>Save as a different song; no Version required.</summary>
    SaveAsNewSong
}
