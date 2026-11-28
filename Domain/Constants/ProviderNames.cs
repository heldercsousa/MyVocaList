namespace MyVocaList.Domain.Constants;

/// <summary>External metadata provider names stored in <c>ExternalProvider</c> (REQ-MSRC-02).</summary>
public static class ProviderNames
{
    /// <summary>MusicBrainz catalog provider; the exact stored string.</summary>
    public const string MusicBrainz = "MusicBrainz";

    /// <summary>iTunes Search API, the remote source of song-title suggestions (REQ-ITS-01). Not stored as ExternalProvider.</summary>
    public const string ITunes = "iTunes";
}
