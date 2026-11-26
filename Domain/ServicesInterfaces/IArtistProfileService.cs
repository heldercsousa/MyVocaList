using MyVocaList.Contracts.DTOs;

namespace MyVocaList.Domain.ServicesInterfaces;

/// <summary>Resolves an artist's photo + credit, description and discography at runtime (REQ-MSRC-08, -09, -10).</summary>
public interface IArtistProfileService
{
    /// <summary>Returns the profile for a MusicBrainz artist id, or null when the lookup fails or the id is invalid.</summary>
    Task<ArtistProfileDto?> GetAsync(string mbid, CancellationToken ct = default);
}
