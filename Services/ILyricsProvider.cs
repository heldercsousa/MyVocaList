namespace MyVocaList.Services;

/// <summary>
/// Placeholder interface for future lyrics API integration.
/// No implementation is registered in DI until a provider is selected via spike task.
/// </summary>
public interface ILyricsProvider
{
    Task<string?> FetchLyricsAsync(string title, string? artistName, CancellationToken ct = default);
}
