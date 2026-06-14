using System.Globalization;
using FuzzySharp;
using MyVocaList.Domain.ServicesInterfaces;

namespace MyVocaList.Infra.Similarity;

/// <inheritdoc />
public class SimilarityScorer : ISimilarityScorer
{
    /// <inheritdoc />
    public double Score(string a, string b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
            return 0.0;

        var normA = Normalize(a);
        var normB = Normalize(b);

        // Fuzz.TokenSetRatio returns an integer 0..100.
        var ratio = Fuzz.TokenSetRatio(normA, normB);
        return ratio / 100.0;
    }

    /// <summary>
    /// NFD-normalizes, strips non-spacing marks (diacritics), and lower-cases the input.
    /// Required because FuzzySharp compares Unicode code points directly; without this,
    /// accented characters (e.g. "Björk" vs "Biork") score significantly lower than their
    /// semantically equivalent normalized forms.
    /// </summary>
    private static string Normalize(string value)
    {
        var decomposed = value.Normalize(NormalizationForm.FormD);
        var stripped = new System.Text.StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                stripped.Append(c);
        }
        return stripped.ToString().ToLowerInvariant();
    }
}
