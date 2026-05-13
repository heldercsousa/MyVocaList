using MyVocaList.Contracts.DTOs.List;
using MyVocaList.Domain.Entity;

namespace MyVocaList.Domain.ServicesInterfaces;

public interface IArtistService
{
    /// <summary>Validates the artist name as typed (UI input, pre-truncation).</summary>
    (bool isValid, string message) ValidateNameInput(string name);

    /// <summary>Creates an artist with the given name. Returns the created entity on success.</summary>
    Task<(bool success, string message, Artist? artist)> CreateArtistAsync(string name, CancellationToken ct = default);

    /// <summary>Updates the name of an existing artist.</summary>
    Task<(bool success, string message)> UpdateArtistAsync(int id, string name, CancellationToken ct = default);

    /// <summary>Deletes one or more artists. Returns a failure if any artist has songs and cascade is not confirmed.</summary>
    Task<(bool success, string message)> DeleteArtistsAsync(IEnumerable<int> ids, CancellationToken ct = default);

    /// <summary>Returns a paged list of artists for the list page, optionally filtered by a search query.</summary>
    Task<(IEnumerable<ArtistListItemDto> items, int totalCount)> GetPagedArtistsForListAsync(
        int pageNumber, int pageSize, string query = null, CancellationToken ct = default);

    /// <summary>Returns up to maxResults artists whose name starts with the query (autocomplete).</summary>
    Task<IEnumerable<ArtistListItemDto>> SearchArtistsByNameAsync(string query, int maxResults = 5, CancellationToken ct = default);

    /// <summary>Returns a human-readable delete confirmation message for the given artist IDs.</summary>
    Task<string> GetDeleteConfirmationAsync(IEnumerable<int> ids, CancellationToken ct = default);

    bool ShouldShowCharacterCounter(int currentLength);
    (string text, bool isWarning, bool isError) GetCharacterCounterInfo(int currentLength);
}
