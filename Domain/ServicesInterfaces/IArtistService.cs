using MyVocaList.Domain.Constants;
using MyVocaList.Domain.Entity;
using MyVocaList.Domain.ReadModels;
using MyVocaList.Domain.RepositoryInterface;

namespace MyVocaList.Domain.ServicesInterfaces;

public interface IArtistService
{
    /// <summary>Validates the artist name as typed (UI input, pre-truncation).</summary>
    (bool isValid, string message) ValidateNameInput(string name);

    /// <summary>Creates an artist with the given name and optional external identity
    /// (REQ-FORMUX-07 — persisted when a remote suggestion pick supplied them).</summary>
    Task<(bool success, string message, Artist? artist)> CreateArtistAsync(
        string name, string? externalId = null, string? externalProvider = null, CancellationToken ct = default);

    /// <summary>Updates the name of an existing artist.</summary>
    Task<(bool success, string message)> UpdateArtistAsync(int id, string name, CancellationToken ct = default);

    /// <summary>Reads one artist without tracking; <c>null</c> when it does not exist (REQ-PIC-36: the edit form's stored picture).</summary>
    Task<Artist?> GetArtistByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Stores the artist's picture reference (REQ-PIC-36). The value is normalised by
    /// <c>ImageReferenceRules.Normalize</c>: an image URL or a well-formed local reference is kept, anything
    /// else is stored as <c>null</c> (REQ-PIC-02); <c>null</c> removes the picture. Fails, writing nothing,
    /// when the artist does not exist.
    /// </summary>
    Task<(bool success, string message)> SetArtistImageAsync(int id, string? imageReference, CancellationToken ct = default);

    /// <summary>Deletes one or more artists. Returns a failure if any artist has songs and cascade is not confirmed.</summary>
    Task<(bool success, string message)> DeleteArtistsAsync(IEnumerable<int> ids, CancellationToken ct = default);

    /// <summary>Returns a paged list of artists for the list page, optionally filtered by a search query.</summary>
    Task<(IEnumerable<ArtistListItem> items, int totalCount)> GetPagedArtistsForListAsync(
        int pageNumber, int pageSize, string query = null,
        ArtistRoleFilter roleFilter = ArtistRoleFilter.All, CancellationToken ct = default);

    /// <summary>Returns up to maxResults artists whose name starts with the query (autocomplete).</summary>
    Task<IEnumerable<ArtistListItem>> SearchArtistsByNameAsync(string query, int maxResults = SearchConstants.MaxSearchResults, CancellationToken ct = default);

    /// <summary>Returns a human-readable delete confirmation message for the given artist IDs.</summary>
    Task<string> GetDeleteConfirmationAsync(IEnumerable<int> ids, CancellationToken ct = default);

    bool ShouldShowCharacterCounter(int currentLength);
    (string text, bool isWarning, bool isError) GetCharacterCounterInfo(int currentLength);
}
