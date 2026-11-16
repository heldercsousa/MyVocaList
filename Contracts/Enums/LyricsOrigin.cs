namespace MyVocaList.Contracts.Enums;

/// <summary>
/// Where a song's stored lyrics came from (REQ-SLD-26..28). A null value on the song means "not recorded".
/// </summary>
public enum LyricsOrigin
{
    /// <summary>The lyrics are the text the lyrics source returned for the song.</summary>
    LyricsSource,

    /// <summary>The user typed or changed the lyrics.</summary>
    User,

    /// <summary>The lyrics source gave a definite "no match"; no lookup starts again for the song.</summary>
    NoneFound
}
