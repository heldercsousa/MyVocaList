namespace MyVocaList.Contracts.DTOs.List;

public record SongListItemDto(
    int Id,
    string Title,
    int OriginalArtistId,
    string? OriginalArtistName,
    string? FeaturedArtists,
    string? ExternalProvider,
    bool HasManualEdits);
