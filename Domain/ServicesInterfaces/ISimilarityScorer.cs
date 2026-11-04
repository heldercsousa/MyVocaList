namespace MyVocaList.Domain.ServicesInterfaces;

/// <summary>
/// Computes a normalized similarity score between two strings entirely in memory — no I/O, no DB access.
/// </summary>
public interface ISimilarityScorer
{
    /// <summary>
    /// Returns a score in the range 0.0 (no similarity) to 1.0 (identical).
    /// Pure in-memory computation; never performs I/O.
    /// </summary>
    /// <param name="a">First string to compare.</param>
    /// <param name="b">Second string to compare.</param>
    /// <returns>Similarity score in [0.0, 1.0].</returns>
    double Score(string a, string b);

    /// <summary>
    /// Returns a display-order score in [0.0, 1.0] for a bounded, DB-retrieved candidate list
    /// (design § 6.3). Uses a token-sort ratio on diacritic-stripped, lower-cased strings.
    /// <para>
    /// <b>Ordering only.</b> This is the sole sanctioned C#-side string-similarity use (exception
    /// <c>design.md § 6.4</c>, filed in <c>.claude/exception-registry.md</c> 2026-09-26): it may only
    /// order a candidate list already retrieved from the database and bounded by
    /// <c>SimilarityConstants.PoolSize</c>. It must never filter, deduplicate, or test uniqueness.
    /// </para>
    /// </summary>
    /// <param name="query">The text the user typed.</param>
    /// <param name="candidate">A DB-retrieved candidate string (e.g. title, or title plus artist).</param>
    /// <returns>Rank score in [0.0, 1.0]; 0.0 when either input is null or empty.</returns>
    double RankScore(string query, string candidate);
}
