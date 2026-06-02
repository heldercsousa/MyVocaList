namespace MyVocaList.Contracts.DTOs;

public record MusicSearchResultDto(
    string ExternalId,
    string Provider,
    string ArtistName,
    string? SongTitle,
    string? FeaturedArtists);
