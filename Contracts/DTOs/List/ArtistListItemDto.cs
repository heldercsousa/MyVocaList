namespace MyVocaList.Contracts.DTOs.List;

public record ArtistListItemDto(
    int Id,
    string Name,
    string ExternalProvider,
    bool HasManualEdits,
    int SongCount)
{
    public string SongCountText => SongCount == 1 ? "1 song" : $"{SongCount} songs";
    public string ProviderBadgeText => ExternalProvider switch
    {
        "musicbrainz" => "MB",
        "deezer" => "DZ",
        _ => string.Empty
    };
}
