using MyVocaList.Contracts.DTOs.List;
using MyVocaList.Domain.Entity;
using MyVocaList.Domain.ReadModels;

namespace MyVocaList.Domain.RepositoryInterface;

public interface ISongRepository
{
    /// <summary>All songs — global, not scoped to any artist.</summary>
    Task<(IEnumerable<SongListItemDto> items, int totalCount)> GetPagedAsync(
        int pageNumber, int pageSize, string? query, CancellationToken ct);

    Task<Song> GetByIdAsync(int id, CancellationToken ct);
    Task<Song?> GetByExternalIdAsync(string externalId, string provider, CancellationToken ct);
    Task<bool> ExistsByTitleForArtistAsync(int artistId, string title, CancellationToken ct);
    Task<bool> ExistsByTitleForArtistAsync(int artistId, string title, int excludeId, CancellationToken ct);

    /// <summary>
    /// Returns all songs whose title is collation-equal (case- and accent-insensitive) to any
    /// of the given <paramref name="titles"/>, resolved in a single batch query.
    /// Used for remote-suggestion dedup tier (b) — REQ-FORMUX-03.
    /// </summary>
    /// <param name="titles">Candidate titles to match; blank/duplicate entries are ignored.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Matching songs, unordered; empty when no candidate matches.</returns>
    Task<IReadOnlyList<Song>> GetByTitlesCollatedAsync(IEnumerable<string> titles, CancellationToken ct = default);

    /// <summary>
    /// REQ-SFT-42: the id of the collated (artist, title) match, or null. Replaces the
    /// Exists + pool re-match + OrdinalIgnoreCase path (SongResolutionService.cs:96-127).
    /// </summary>
    /// <param name="artistId">Scope: original artist of the song.</param>
    /// <param name="title">Song title to match.</param>
    /// <param name="excludeId">Id of the song being edited — excluded from the match; null when creating.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<int?> FindIdByTitleForArtistAsync(int artistId, string title, int? excludeId, CancellationToken ct);

    /// <summary>
    /// REQ-SFT-26: true when another song holds this external identity.
    /// </summary>
    /// <param name="provider">External provider name (e.g. "MusicBrainz").</param>
    /// <param name="externalId">External id value for that provider.</param>
    /// <param name="excludeId">Id of the song being edited — excluded from the check; null when creating.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<bool> ExistsByExternalIdentityAsync(string provider, string externalId, int? excludeId, CancellationToken ct);

    /// <summary>
    /// Local title-field search (REQ-SFT-02, 30-34). Infra chooses the strategy:
    /// FTS5 trigram OR-match when the query has a word of &gt;= 3 chars, else collated title prefix.
    /// </summary>
    /// <param name="query">Raw user query.</param>
    /// <param name="take">Maximum number of rows to return.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>At most <paramref name="take"/> rows in DB relevance order (bm25, then shorter title).</returns>
    Task<IReadOnlyList<SongSearchHit>> SearchTitleCandidatesAsync(string query, int take, CancellationToken ct);

    /// <summary>
    /// REQ-SFT-23 (BUG-085): same-artist near-duplicate candidates for the Possible duplicates step.
    /// Same strategy as <see cref="SearchTitleCandidatesAsync"/> (FTS5 trigram OR-match over the whole typed
    /// title, else collated title prefix; REQ-SFT-33), restricted to songs of <paramref name="artistId"/>.
    /// Unlike the retired forward-prefix pool, a typed title longer than the stored one still matches.
    /// </summary>
    /// <param name="artistId">Scope: original artist of the song.</param>
    /// <param name="title">Whole typed title (not a prefix token); blank returns an empty list.</param>
    /// <param name="take">Maximum number of rows to return (pool size).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>At most <paramref name="take"/> rows of the artist, in DB relevance order (bm25, then shorter title).</returns>
    Task<IReadOnlyList<SongSearchHit>> SearchTitleCandidatesForArtistAsync(
        int artistId, string title, int take, CancellationToken ct = default);

    /// <summary>REQ-SFT-05(a): songs holding any of the given external ids for one provider.</summary>
    /// <param name="provider">External provider name (e.g. "MusicBrainz").</param>
    /// <param name="externalIds">External ids to look up.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<IReadOnlyList<SongSearchHit>> FindByExternalIdsAsync(string provider, IReadOnlyCollection<string> externalIds, CancellationToken ct);

    /// <summary>
    /// REQ-SFT-05(b): collated (title, artist name) matches.
    /// </summary>
    /// <param name="items">Pairs to match.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<IReadOnlyList<SongSearchHit>> FindByTitleAndArtistNamesAsync(IReadOnlyCollection<(string Title, string ArtistName)> items, CancellationToken ct);

    /// <summary>Every stored <c>local:</c> image reference of a song (REQ-PIC-11 orphan sweep).</summary>
    Task<List<string>> GetLocalImageReferencesAsync(CancellationToken ct = default);

    Task AddAsync(Song song, CancellationToken ct);
    Task UpdateAsync(Song song, CancellationToken ct);
    Task DeleteAsync(IEnumerable<int> ids, CancellationToken ct);

    /// <summary>REQ-CATPROP-10: number of songs the artist authored (catalog covers are not counted).</summary>
    Task<int> CountByArtistAsync(int artistId, CancellationToken ct);
}
