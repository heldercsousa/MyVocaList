namespace MyVocaList.Contracts.DTOs.List;

public record ArtistListItemDto(
    int Id,
    string Name,
    string? ExternalProvider,
    bool HasManualEdits,
    int CatalogCount)
{
    public string CatalogCountText => CatalogCount == 0
        ? "No catalog"
        : CatalogCount == 1 ? "1 song in catalog" : $"{CatalogCount} songs in catalog";

    public string ProviderBadgeText => ExternalProvider switch
    {
        "musicbrainz" => "MB",
        "deezer" => "DZ",
        _ => string.Empty
    };
}
