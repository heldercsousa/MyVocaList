namespace MyVocaList.Domain.Resolution;

/// <summary>What <c>ISongService.SaveFromFormAsync</c> decided (REQ-SFT-51).</summary>
public enum SongFormSaveKind
{
    /// <summary>The song was written.</summary>
    Saved,

    /// <summary>Validation or the write refused the save; see the message and failed field.</summary>
    Failed,

    /// <summary>The typed artist has near-duplicates; the user must choose (REQ-SFT-19).</summary>
    ArtistChoiceNeeded,

    /// <summary>The song may already exist; the user must choose (REQ-SFT-22/23/25).</summary>
    SongChoiceNeeded
}

/// <summary>The form field a <see cref="SongFormSaveKind.Failed"/> outcome belongs to.</summary>
public enum SaveField
{
    /// <summary>Not tied to one field (e.g. a write refusal).</summary>
    None,

    /// <summary>The Title field.</summary>
    Title,

    /// <summary>The Version field.</summary>
    Version,

    /// <summary>The Artist field.</summary>
    Artist
}

/// <summary>A near-duplicate artist offered on the artist-choice sheet.</summary>
public sealed record ArtistMatch(int ArtistId, string Name, double Score);

/// <summary>
/// Result of <c>ISongService.SaveFromFormAsync</c>. Four outcomes do not fit the
/// <c>(bool, string, T?)</c> tuple, so this record is the designed exception to the Service Return
/// Pattern; <see cref="SongFormSaveKind.Failed"/> carries the message exactly as the tuple would.
/// </summary>
/// <param name="Kind">The decision taken.</param>
/// <param name="Message">Saved / Failed text; empty for the choice outcomes.</param>
/// <param name="FailedField">Field of a validation failure; <see cref="SaveField.None"/> otherwise.</param>
/// <param name="SavedSongId">Id of the written song when <see cref="SongFormSaveKind.Saved"/>.</param>
/// <param name="ArtistCandidates">Near-duplicate artists (at most 3, best first) when <see cref="SongFormSaveKind.ArtistChoiceNeeded"/>.</param>
/// <param name="IsExactSongMatch">True when the duplicate song is an exact match (<see cref="SongFormSaveKind.SongChoiceNeeded"/>).</param>
/// <param name="SongCandidates">The duplicate song(s) when <see cref="SongFormSaveKind.SongChoiceNeeded"/>.</param>
public sealed record SongFormSaveOutcome(
    SongFormSaveKind Kind,
    string Message,
    SaveField FailedField,
    int? SavedSongId,
    IReadOnlyList<ArtistMatch> ArtistCandidates,
    bool IsExactSongMatch,
    IReadOnlyList<SongMatch> SongCandidates)
{
    /// <summary>
    /// The artist name the user typed, trimmed and nothing else (REQ-SFT-19, ruling D4). Set on
    /// <see cref="SongFormSaveKind.ArtistChoiceNeeded"/> so the sheet can name the typed artist; empty for
    /// every other outcome. An init property, not a positional parameter, so the constructor is unchanged.
    /// </summary>
    public string TypedName { get; init; } = string.Empty;
}
