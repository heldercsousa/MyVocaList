using MyVocaList.Domain.Resolution;

namespace MyVocaList.Domain.ServicesInterfaces;

/// <summary>
/// Resolves an <see cref="ArtistCandidate"/> against the local database and commits
/// the user's chosen action. Artist resolution must complete before song resolution (INV-1).
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

    /// <summary>
    /// Applies the user's resolution choice for an artist candidate.
    /// </summary>
    /// <param name="candidate">The artist candidate being committed.</param>
    /// <param name="choice">The action chosen by the user or system.</param>
    /// <param name="targetArtistId">
    /// The id of the existing artist to update or attach to.
    /// Required for <see cref="ResolutionChoice.UpdateExisting"/> and <see cref="ResolutionChoice.AttachExternalId"/>;
    /// <c>null</c> for <see cref="ResolutionChoice.CreateNew"/>.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// <c>(true, message, artistId)</c> on success where <c>artistId</c> is the id of the created or resolved artist;
    /// <c>(false, reason, 0)</c> on failure.
    /// </returns>
    Task<(bool success, string message, int artistId)> CommitAsync(
        ArtistCandidate candidate,
        ResolutionChoice choice,
        int? targetArtistId,
        CancellationToken ct = default);
}
