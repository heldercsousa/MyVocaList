using MyVocaList.Domain.Entity;
using MyVocaList.Domain.ReadModels;

namespace MyVocaList.Domain.RepositoryInterface;

public enum ArtistRoleFilter { All, AuthorsOnly, PerformersOnly }

public interface IArtistRepository
{
    /// <summary>Returns a paged list of artists with their catalog counts.</summary>
    Task<(IEnumerable<ArtistListItem> items, int totalCount)> GetPagedAsync(
        int pageNumber, int pageSize, string query,
        ArtistRoleFilter roleFilter = ArtistRoleFilter.All, CancellationToken ct = default);

    /// <summary>Returns artists whose name starts with the query (max results).</summary>
    Task<IEnumerable<ArtistListItem>> SearchByNameAsync(string query, int maxResults, CancellationToken ct);

    Task<Artist> GetByIdAsync(int id, CancellationToken ct);
    Task<Artist> GetByExternalIdAsync(string externalId, string provider, CancellationToken ct);

    /// <summary>
    /// Returns the artist whose name exactly matches <paramref name="name"/> under the configured
    /// case- and accent-insensitive collation, or <c>null</c> if not found.
    /// Used by <c>IArtistResolutionService</c> for exact-local-name matching (AC-3.2).
    /// </summary>
    Task<Artist?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name, CancellationToken ct);
    Task<bool> ExistsByNameAsync(string name, int excludeId, CancellationToken ct);

    /// <summary>
    /// Returns a bounded pool of artists whose name starts with <paramref name="namePrefixToken"/>
    /// under collation (index-friendly prefix query). Used as the pre-filter before in-memory fuzzy scoring.
    /// </summary>
    /// <param name="namePrefixToken">
    /// First whitespace-delimited token of the candidate name, capped at 12 chars.
    /// </param>
    /// <param name="take">Maximum number of rows to return (default pool size: 50).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>At most <paramref name="take"/> artists matching the prefix, unordered.</returns>
    Task<IReadOnlyList<Artist>> GetFuzzyCandidatePoolAsync(string namePrefixToken, int take, CancellationToken ct = default);

    /// <summary>
    /// Returns all artists whose name is collation-equal (case- and accent-insensitive) to any
    /// of the given <paramref name="names"/>, resolved in a single batch query.
    /// Used for remote-suggestion dedup tier (b) — REQ-FORMUX-03.
    /// </summary>
    /// <param name="names">Candidate names to match; blank/duplicate entries are ignored.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Matching artists, unordered; empty when no candidate matches.</returns>
    Task<IReadOnlyList<Artist>> GetByNamesCollatedAsync(IEnumerable<string> names, CancellationToken ct = default);

    Task AddAsync(Artist artist, CancellationToken ct);
    Task UpdateAsync(Artist artist, CancellationToken ct);
    Task DeleteAsync(IEnumerable<int> ids, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct = default);
}
