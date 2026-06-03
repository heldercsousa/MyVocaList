namespace MyVocaList.Contracts.DTOs;

public record UpdateCheckResult(
    bool IsUpToDate,
    bool IsUpdateAvailable,
    bool IsUpdateRequired,
    string StoreUrl,
    string LatestVersion,
    string UpdateMessage)
{
    /// <summary>Returned when the manifest could not be fetched (fail-open) or the app is up to date.</summary>
    public static readonly UpdateCheckResult UpToDate =
        new(true, false, false, string.Empty, string.Empty, string.Empty);
}
