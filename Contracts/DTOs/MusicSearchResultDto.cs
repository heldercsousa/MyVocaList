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
    /// <summary>The provider's artist id as text: groups rows, and is stored on an artist a Save creates from the pick (REQ-MSRC-13).</summary>
    string? ExternalArtistId = null);
