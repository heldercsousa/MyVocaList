namespace MyVocaList.Domain.Rules;

/// <summary>Identity predicates of a stored song, derived from its music-database provider and id (REQ-SLD-12, 13).</summary>
public static class SongIdentityRules
{
    /// <summary>True when the song has both a provider and a provider id, both non-blank; a local-only song has neither.</summary>
    public static bool IsRemoteSong(string? externalProvider, string? externalId)
        => !string.IsNullOrWhiteSpace(externalProvider) && !string.IsNullOrWhiteSpace(externalId);
}
