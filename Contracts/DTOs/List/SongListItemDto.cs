namespace MyVocaList.Contracts.DTOs.List;

public record SongListItemDto(
    int Id,
    int ArtistId,
    string Title,
    string ArtistName,
    string FeaturedArtists,
    string ExternalProvider,
    bool HasManualEdits);
