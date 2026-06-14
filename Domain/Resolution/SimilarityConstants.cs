namespace MyVocaList.Domain.Resolution;

/// <summary>
/// Tuning constants for the on-device fuzzy similarity engine.
/// Lives in Domain so that the Services layer can reference them without depending on Infra.
/// </summary>
public static class SimilarityConstants
{
    /// <summary>
    /// Minimum score (0..1) for a candidate to be surfaced as a fuzzy match.
    /// Provisional — may be tuned after Wave 5 smoke test with real accented samples.
    /// </summary>
    public const double DefaultThreshold = 0.82;

    /// <summary>Maximum number of DB rows retrieved in a single candidate pool query.</summary>
    public const int PoolSize = 50;

    /// <summary>Maximum character length of the title/name prefix token sent to the DB LIKE query.</summary>
    public const int PrefixTokenMaxLen = 12;
}
