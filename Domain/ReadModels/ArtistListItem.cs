namespace MyVocaList.Domain.ReadModels;

/// <summary>
/// Read model for displaying artists in a paginated list.
/// Returned by <see cref="RepositoryInterface.IArtistRepository.GetPagedAsync"/> and <see cref="RepositoryInterface.IArtistRepository.SearchByNameAsync"/>.
/// </summary>
/// <param name="Id">Unique identifier</param>
/// <param name="Name">Artist display name</param>
/// <param name="ExternalProvider">Source provider (e.g. "musicbrainz")</param>
/// <param name="CatalogCount">Number of songs in the catalog for this artist</param>
/// <param name="ImageUrl">Stored artist image URL, or null (REQ-ART-10).</param>
public record ArtistListItem(int Id, string Name, string ExternalProvider, int CatalogCount, string? ImageUrl = null)
{
    /// <summary>Human-readable catalog count text for UI display.</summary>
    public string CatalogCountText => CatalogCount == 0
        ? "No catalog"
        : CatalogCount == 1 ? "1 song in catalog" : $"{CatalogCount} songs in catalog";

    /// <summary>Short provider badge text for UI display.</summary>
    public string ProviderBadgeText => ExternalProvider switch
    {
        "musicbrainz" => "MB",
        "deezer" => "DZ",
        _ => string.Empty
    };
}
