namespace MyVocaList.Contracts.DTOs;

public record MusicSearchResultDto(
    string ExternalId,
    string Provider,
    string ArtistName,
    string? SongTitle,
    string? FeaturedArtists,
    string? Version = null,
    string? ImageUrl = null,
    string? ArtistImageUrl = null,
    string? AlbumTitle = null);
