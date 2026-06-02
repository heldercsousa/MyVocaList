namespace MyVocaList.Contracts.DTOs;

public record ReleaseEntry(
    string Version,
    string Date,
    IReadOnlyList<string> Highlights,
    IReadOnlyList<string> Fixes);
