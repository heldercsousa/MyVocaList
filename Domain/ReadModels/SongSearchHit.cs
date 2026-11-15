namespace MyVocaList.Domain.ReadModels;

/// <summary>
/// One local song match returned by the title-field search and the dedup lookups (REQ-SFT-02, 05, 30-34).
/// </summary>
/// <param name="SongId">Song identifier.</param>
/// <param name="Title">Song title.</param>
/// <param name="Version">Version label; empty string for the canonical version.</param>
/// <param name="ArtistId">Original artist identifier.</param>
/// <param name="ArtistName">Original artist display name.</param>
/// <param name="ExternalProvider">External provider name, or null.</param>
/// <param name="ExternalId">External id at the provider, or null.</param>
/// <param name="ImageUrl">The stored song image URL, or null (REQ-ART-06).</param>
/// <param name="ArtistImageUrl">The original artist's stored image URL, or null (REQ-ART-06).</param>
public sealed record SongSearchHit(
    int SongId, string Title, string Version, int ArtistId, string ArtistName,
    string? ExternalProvider, string? ExternalId, string? ImageUrl = null, string? ArtistImageUrl = null);
