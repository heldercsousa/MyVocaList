using MyVocaList.Domain.Entity;
using MyVocaList.Domain.Resolution;

namespace MyVocaList.Domain.ServicesInterfaces;

/// <summary>
/// Resolves a <see cref="SongCandidate"/> against the local database (read-only). The artist must be resolved first (INV-1).
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
}
