namespace MyVocaList.Contracts.DTOs;

/// <summary>Input of a lyrics lookup: the picked Deezer title and artist, optionally album and track length (REQ-LYR-01).</summary>
public sealed record LyricsQuery(string Title, string ArtistName, string? AlbumTitle = null, int? DurationSeconds = null);
