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
}
