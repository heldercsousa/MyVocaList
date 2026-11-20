namespace MyVocaList.Contracts.DTOs;

public record MusicSearchResultDto(
    string ExternalId,
    string Provider,
    string ArtistName,
    string? SongTitle,
    string? ImageUrl = null,
    string? ArtistImageUrl = null,
    string? AlbumTitle = null,
    int? DurationSeconds = null,
    /// <summary>The provider's artist id as text, used only to group rows; never stored.</summary>
    string? ExternalArtistId = null);
