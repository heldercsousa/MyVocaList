namespace MyVocaList.Services;

/// <summary>
/// Typo-tolerant word matching and coverage re-rank for remote song-title rows (REQ-ITS-14..16).
/// Pure logic: it orders and filters rows the provider already returned, it never adds rows.
/// </summary>
public static class SongTitleMatcher
{
    private const int MaxEditDistance = 2;
    private const int MinShorterLength = 3;
    private const int MinLongerLength = 4;

    /// <summary>Two words match when equal, or within <see cref="MaxEditDistance"/> edits with lengths 3+ and 4+ (REQ-ITS-15).</summary>
    public static bool WordsMatch(string left, string right)
    {
        var a = NormalizeWord(left);
        var b = NormalizeWord(right);
        if (a.Length == 0 || b.Length == 0)
            return false;
        if (string.Equals(a, b, StringComparison.Ordinal))
            return true;

        var shorter = Math.Min(a.Length, b.Length);
        var longer = Math.Max(a.Length, b.Length);
        if (shorter < MinShorterLength || longer < MinLongerLength)
            return false;

        return Levenshtein(a, b) <= MaxEditDistance;
    }

    /// <summary>
    /// Moves rows whose text covers every query word ahead of the rest. Both groups keep the input (popularity)
    /// order, so a partial match never moves above a covering row and only covering rows can move up (REQ-ITS-16).
    /// </summary>
    public static IReadOnlyList<T> RerankByCoverage<T>(string query, IEnumerable<T> rows, Func<T, string> textOf)
    {
        var rowList = rows.ToList();
        var queryWords = Tokens(query);
        if (queryWords.Count == 0)
            return rowList;

        var covering = new List<T>();
        var partial = new List<T>();
        foreach (var row in rowList)
        {
            if (Covers(queryWords, textOf(row)))
                covering.Add(row);
            else
                partial.Add(row);
        }

        covering.AddRange(partial);
        return covering;
    }

    /// <summary>Returns the query without its last word, or null when the query has fewer than two words (REQ-ITS-14).</summary>
    public static string? DropLastWord(string query)
    {
        var words = query.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return words.Length < 2 ? null : string.Join(' ', words[..^1]);
    }

    private static bool Covers(IReadOnlyList<string> queryWords, string text)
    {
        var words = Tokens(text);
        return queryWords.All(q => words.Any(t => WordsMatch(q, t)));
    }

    private static List<string> Tokens(string text) =>
        text.Split([' ', '-', '(', ')', ',', '.', '&', '/', '!', '?'], StringSplitOptions.RemoveEmptyEntries)
            .Select(NormalizeWord)
            .Where(w => w.Length > 0)
            .ToList();

    private static string NormalizeWord(string word) =>
        new string(word.Where(c => c != '\'' && c != '’').ToArray()).ToLowerInvariant();

    private static int Levenshtein(string a, string b)
    {
        var previous = new int[b.Length + 1];
        var current = new int[b.Length + 1];
        for (var j = 0; j <= b.Length; j++)
            previous[j] = j;

        for (var i = 1; i <= a.Length; i++)
        {
            current[0] = i;
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + cost);
            }
            (previous, current) = (current, previous);
        }

        return previous[b.Length];
    }
}
