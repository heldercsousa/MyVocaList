namespace MyVocaList.Contracts.DTOs;

public record VersionManifest(
    string LatestVersion,
    string MinRequiredVersion,
    Dictionary<string, string> StoreUrls,
    string UpdateMessage);
