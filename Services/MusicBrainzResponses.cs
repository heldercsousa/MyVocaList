using System.Text.Json.Serialization;

namespace MyVocaList.Services;

internal sealed record MbArtistSearchResponse(
    [property: JsonPropertyName("artists")] List<MbArtist>? Artists);

internal sealed record MbRecordingSearchResponse(
    [property: JsonPropertyName("recordings")] List<MbRecording>? Recordings);

internal sealed record MbArtist(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name);

internal sealed record MbArtistCredit(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("artist")] MbArtist? Artist);

internal sealed record MbRelease(
    [property: JsonPropertyName("title")] string? Title);

internal sealed record MbRecording(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("length")] int? Length,
    [property: JsonPropertyName("artist-credit")] List<MbArtistCredit>? ArtistCredit,
    [property: JsonPropertyName("releases")] List<MbRelease>? Releases);
