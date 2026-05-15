using MyVocaList.Contracts.DTOs.List;
using MyVocaList.Domain.Entity;

namespace MyVocaList.Domain.ServicesInterfaces;

public interface ISongService
{
    /// <summary>Validates the song title as typed (UI input, pre-truncation).</summary>
    (bool isValid, string message) ValidateTitleInput(string title);

    /// <summary>Creates a song. artistId is mandatory — every song has an original/copyright artist.</summary>
    Task<(bool success, string message, Song? song)> CreateSongAsync(
        int artistId, string title, string? featuredArtists = null, string? lyrics = null,
        string? externalId = null, string? externalProvider = null, CancellationToken ct = default);

    /// <summary>Updates title, featured artists, and lyrics of an existing song.</summary>
    Task<(bool success, string message)> UpdateSongAsync(
        int id, string title, string? featuredArtists, string? lyrics, bool hasManualEdits,
        CancellationToken ct = default);

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
