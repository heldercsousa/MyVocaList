using MyVocaList.Contracts.DTOs.List;
using MyVocaList.Domain.Entity;

namespace MyVocaList.Domain.ServicesInterfaces;

public interface ISongService
{
    /// <summary>Validates the song title as typed (UI input, pre-truncation).</summary>
    (bool isValid, string message) ValidateTitleInput(string title);

    /// <summary>
    /// Validates the song version/variant label as typed (UI input). The field is optional in the
    /// main form (empty = canonical version); pass <paramref name="isRequired"/> true only for the
    /// "Save as new version" resolution flow, where an empty value is rejected (AC-1.2).
    /// </summary>
    (bool isValid, string message) ValidateVersionInput(string? version, bool isRequired = false);

    /// <summary>Creates a song. artistId is mandatory — every song has an original/copyright artist.</summary>
    Task<(bool success, string message, Song? song)> CreateSongAsync(
        int artistId, string title, string? featuredArtists = null, string? lyrics = null,
        string? externalId = null, string? externalProvider = null, CancellationToken ct = default);

    /// <summary>
    /// Updates title, featured artists, lyrics, and optionally version and external identity of an
    /// existing song.
    /// </summary>
    /// <param name="id">Id of the song to update.</param>
    /// <param name="title">New song title.</param>
    /// <param name="featuredArtists">Featured-artist names; null clears the field.</param>
    /// <param name="lyrics">Lyrics text; null clears the field.</param>
    /// <param name="hasManualEdits">Whether the song now carries manual edits.</param>
    /// <param name="externalId">External provider song id; null = keep existing value.</param>
    /// <param name="externalProvider">External provider name; null = keep existing value.</param>
    /// <param name="version">Variant label (empty string = canonical); null = keep existing value (BUG-024).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>(true, message)</c> on success; <c>(false, reason)</c> on failure.</returns>
    Task<(bool success, string message)> UpdateSongAsync(
        int id, string title, string? featuredArtists, string? lyrics, bool hasManualEdits,
        string? externalId = null, string? externalProvider = null, string? version = null,
        CancellationToken ct = default);

    /// <summary>
    /// Creates a song and atomically persists the supplied YouTube/streaming URLs in a single transaction.
    /// Both repositories must share the same scoped <c>AppDbContext</c> so a single
    /// <c>SaveChangesAsync</c> commits song and URLs together (AC-6.2).
    /// </summary>
    /// <param name="artistId">Id of the original/copyright artist (mandatory — INV-1).</param>
    /// <param name="title">Song title.</param>
    /// <param name="version">Variant label; empty string = canonical version.</param>
    /// <param name="featuredArtists">Optional featured-artist names.</param>
    /// <param name="lyrics">Optional lyrics text.</param>
    /// <param name="externalId">Optional external provider song id.</param>
    /// <param name="externalProvider">Optional external provider name.</param>
    /// <param name="urls">Zero or more streaming/YouTube URLs to attach atomically.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>(true, message, song)</c> on success; <c>(false, reason, null)</c> on failure.</returns>
    Task<(bool success, string message, Song? song)> CreateSongWithUrlsAsync(
        int artistId, string title, string version, string? featuredArtists, string? lyrics,
        string? externalId, string? externalProvider,
        IEnumerable<string> urls,
        CancellationToken ct = default);

    /// <summary>
    /// Returns the full song entity for the given id, including FeaturedArtists, Lyrics, Version
    /// and external identity — used by edit-mode form hydration (BUG-024).
    /// </summary>
    /// <param name="id">Id of the song to load.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The song when found; <c>null</c> when no song has the given id.</returns>
    Task<Song?> GetSongByIdAsync(int id, CancellationToken ct = default);

    /// <summary>Checks per-artist title uniqueness (a song title must be unique per original artist).</summary>
    Task<bool> ExistsByTitleForArtistAsync(string title, int artistId, int? excludeId = null, CancellationToken ct = default);

    /// <summary>Deletes one or more songs.</summary>
    Task<(bool success, string message)> DeleteSongsAsync(IEnumerable<int> ids, CancellationToken ct = default);

    /// <summary>Returns a global paged list of all songs, optionally filtered by a search query.</summary>
    Task<(IEnumerable<SongListItemDto> items, int totalCount)> GetPagedSongsForListAsync(
        int pageNumber, int pageSize, string? query = null, CancellationToken ct = default);

    bool ShouldShowCharacterCounter(int currentLength);
    (string text, bool isWarning, bool isError) GetCharacterCounterInfo(int currentLength);
}
