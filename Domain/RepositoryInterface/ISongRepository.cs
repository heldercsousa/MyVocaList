using MyVocaList.Contracts.DTOs.List;
using MyVocaList.Domain.Entity;

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
    /// Returns <c>true</c> if a song with the given title and version already exists for the specified artist,
    /// using the configured case- and accent-insensitive collation.
    /// </summary>
    /// <param name="artistId">Scope: original artist of the song.</param>
    /// <param name="title">Song title to match.</param>
    /// <param name="version">Version label to match; pass empty string for canonical versions.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<bool> ExistsByTitleVersionForArtistAsync(int artistId, string title, string version, CancellationToken ct = default);

    /// <summary>
    /// Same as <see cref="ExistsByTitleVersionForArtistAsync(int,string,string,CancellationToken)"/> but
    /// excludes the song with id <paramref name="excludeId"/> (used during edit/update validation).
    /// </summary>
    /// <param name="artistId">Scope: original artist of the song.</param>
    /// <param name="title">Song title to match.</param>
    /// <param name="version">Version label to match.</param>
    /// <param name="excludeId">Id of the song being edited — excluded from the uniqueness check.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<bool> ExistsByTitleVersionForArtistAsync(int artistId, string title, string version, int excludeId, CancellationToken ct = default);

    /// <summary>
    /// Returns a bounded pool of songs for a given artist whose title starts with
    /// <paramref name="titlePrefixToken"/> under collation (index-friendly prefix query).
    /// Used as the pre-filter before in-memory fuzzy scoring.
    /// </summary>
    /// <param name="artistId">Scope: original artist of the song.</param>
    /// <param name="titlePrefixToken">
    /// First whitespace-delimited token of the candidate title, capped at 12 chars.
    /// </param>
    /// <param name="take">Maximum number of rows to return (default pool size: 50).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>At most <paramref name="take"/> songs matching the prefix, unordered.</returns>
    Task<IReadOnlyList<Song>> GetFuzzyCandidatePoolAsync(int artistId, string titlePrefixToken, int take, CancellationToken ct = default);
    Task AddAsync(Song song, CancellationToken ct);
    Task UpdateAsync(Song song, CancellationToken ct);
    Task DeleteAsync(IEnumerable<int> ids, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct = default);
}
