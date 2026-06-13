using MyVocaList.Domain.Entity;
using MyVocaList.Domain.Resolution;

namespace MyVocaList.Domain.ServicesInterfaces;

/// <summary>
/// Resolves a <see cref="SongCandidate"/> against the local database and commits
/// the user's chosen action. The artist must be resolved first (INV-1).
/// </summary>
public interface ISongResolutionService
{
    /// <summary>
    /// Determines whether the candidate matches an existing song record exactly
    /// (by external id or by title+version+artist under collation) or fuzzy-matches
    /// one or more candidates scored by <see cref="ISimilarityScorer"/>.
    /// </summary>
    /// <param name="candidate">The song to resolve. The candidate's artist must already be resolved.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A <see cref="SongResolution"/> describing the match kind, the exact match id (if any),
    /// ranked fuzzy candidates (if any), and field diffs when the target has manual edits.
    /// </returns>
    Task<SongResolution> ResolveAsync(SongCandidate candidate, CancellationToken ct = default);

    /// <summary>
    /// Applies the user's resolution choice for a song candidate.
    /// For <see cref="ResolutionChoice.UpdateExisting"/> when the target has manual edits,
    /// only the fields listed in <paramref name="acceptedFields"/> are overwritten.
    /// </summary>
    /// <param name="candidate">The song candidate being committed.</param>
    /// <param name="choice">The action chosen by the user or system.</param>
    /// <param name="targetSongId">
    /// The id of the existing song to update or attach to.
    /// Required for <see cref="ResolutionChoice.UpdateExisting"/> and <see cref="ResolutionChoice.AttachExternalId"/>;
    /// <c>null</c> for <see cref="ResolutionChoice.CreateNew"/> and <see cref="ResolutionChoice.CreateNewVersion"/>.
    /// </param>
    /// <param name="acceptedFields">
    /// When <paramref name="choice"/> is <see cref="ResolutionChoice.UpdateExisting"/> and the target has manual edits,
    /// lists the field names (e.g. "Title", "Lyrics") whose API values the user accepted.
    /// <c>null</c> means accept all non-empty API fields.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// <c>(true, message, song)</c> on success; <c>(false, reason, null)</c> on failure.
    /// </returns>
    Task<(bool success, string message, Song? song)> CommitAsync(
        SongCandidate candidate,
        ResolutionChoice choice,
        int? targetSongId,
        IReadOnlyCollection<string>? acceptedFields,
        CancellationToken ct = default);
}
