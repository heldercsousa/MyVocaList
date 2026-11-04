using MyVocaList.Domain.Resolution;

namespace MyVocaList.Domain.ServicesInterfaces;

/// <summary>
/// Resolves an <see cref="ArtistCandidate"/> against the local database.
/// Artist resolution must complete before song resolution (INV-1).
/// </summary>
public interface IArtistResolutionService
{
    /// <summary>
    /// Determines whether the candidate matches an existing artist record exactly
    /// (by external id or by name under collation) or fuzzy-matches one or more candidates.
    /// </summary>
    /// <param name="candidate">The artist to resolve.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// An <see cref="ArtistResolution"/> describing the match kind, the exact match id (if any),
    /// and the ranked fuzzy candidates (if any).
    /// </returns>
    Task<ArtistResolution> ResolveAsync(ArtistCandidate candidate, CancellationToken ct = default);
}
