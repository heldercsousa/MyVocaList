namespace MyVocaList.Contracts.DTOs;

/// <summary>Runtime artist profile (REQ-MSRC-08, -09). Never persisted.</summary>
public record ArtistProfileDto(
    string? ImageUrl,
    string? ImageCredit,
    string? ImageLicenseUrl,
    string? Description,
    IReadOnlyList<ReleaseGroupDto> Discography);

/// <summary>A MusicBrainz release group of the artist.</summary>
public record ReleaseGroupDto(string Mbid, string Title, string? FirstReleaseYear);
