using MyVocaList.Contracts.DTOs.List;
using MyVocaList.Domain.Entity;

namespace MyVocaList.Domain.ServicesInterfaces;

public interface ISongService
{
    /// <summary>Validates the song title as typed (UI input, pre-truncation).</summary>
    (bool isValid, string message) ValidateTitleInput(string title);

    /// <summary>Creates a song under the given artist. Returns the created entity on success.</summary>
    Task<(bool success, string message, Song song)> CreateSongAsync(int artistId, string title, string featuredArtists = null, CancellationToken ct = default);

    /// <summary>Updates the title and featured artists of an existing song.</summary>
    Task<(bool success, string message)> UpdateSongAsync(int id, string title, string featuredArtists = null, CancellationToken ct = default);

    /// <summary>Deletes one or more songs.</summary>
    Task<(bool success, string message)> DeleteSongsAsync(IEnumerable<int> ids, CancellationToken ct = default);

    /// <summary>Returns a paged list of songs for a given artist, optionally filtered by a search query.</summary>
    Task<(IEnumerable<SongListItemDto> items, int totalCount)> GetPagedSongsForListAsync(
        int artistId, int pageNumber, int pageSize, string query = null, CancellationToken ct = default);

    bool ShouldShowCharacterCounter(int currentLength);
    (string text, bool isWarning, bool isError) GetCharacterCounterInfo(int currentLength);
}
