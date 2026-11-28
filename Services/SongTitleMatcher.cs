using System.Text.RegularExpressions;

namespace MyVocaList.Services;

/// <summary>
/// Typo-tolerant word matching and the scored re-rank of remote song-title rows (REQ-ITS-14..16).
/// Pure logic: it orders the rows the provider already returned, it never adds or drops rows.
/// </summary>
public static class SongTitleMatcher
{
    private const int MaxEditDistance = 2;
    private const int MinShorterLength = 3;
    private const int MinLongerLength = 4;
    private const int MinPrefixQueryLength = 3;

    private static readonly Regex BracketedSuffix = new(@"\s*[\(\[][^\)\]]*[\)\]]", RegexOptions.Compiled);
    private static readonly Regex AnyBracket = new(@"[\(\[]", RegexOptions.Compiled);

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

        // impl decision 2026-11-28: a typo keeps the first letter ("crie"→"cry" yes; "dont"→"out" no, distance 2 by accident).
        if (a[0] != b[0])
            return false;

        return Levenshtein(a, b) <= MaxEditDistance;
    }

    /// <summary>
    /// Orders rows by: coverage (query words matched by a title or artist word, most first), then title fit (title words
    /// that no query word matches, fewest first), then a bracketed title suffix last, then the iTunes order (REQ-ITS-16).
    /// </summary>
    public static IReadOnlyList<T> RankRows<T>(string query, IEnumerable<T> rows, Func<T, string> titleOf, Func<T, string> artistOf)
    {
        var rowList = rows.ToList();
        var queryWords = Tokens(query);
        if (queryWords.Count == 0)
            return rowList;

        return rowList
            .Select((row, index) =>
            {
                var title = titleOf(row);
                var coverage = queryWords.Count(q => Tokens($"{title} {artistOf(row)}").Any(w => MatchesQueryWord(q, w)));
                var titleOnly = Tokens(BracketedSuffix.Replace(title, string.Empty));
                var penalty = titleOnly.Count(t => !queryWords.Any(q => MatchesQueryWord(q, t)));
                var bracketed = AnyBracket.IsMatch(title);
                return (row, index, coverage, penalty, bracketed);
            })
            .OrderByDescending(x => x.coverage)
            .ThenBy(x => x.penalty)
            .ThenBy(x => x.bracketed)
            .ThenBy(x => x.index)
            .Select(x => x.row)
            .ToList();
    }

    /// <summary>Returns the query without its last word, or null when the query has fewer than two words (REQ-ITS-14).</summary>
    public static string? DropLastWord(string query)
    {
        var words = query.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return words.Length < 2 ? null : string.Join(' ', words[..^1]);
    }

    /// <summary>A query word matches a text word by typo tolerance (REQ-ITS-15) or as a prefix of 3+ query letters ("gun" → "guns").</summary>
    private static bool MatchesQueryWord(string queryWord, string textWord) =>
        WordsMatch(queryWord, textWord)
        || (queryWord.Length >= MinPrefixQueryLength && textWord.StartsWith(queryWord, StringComparison.Ordinal));

    private static List<string> Tokens(string text) =>
        text.Split([' ', '-', '(', ')', ',', '.', '&', '/', '!', '?', '[', ']'], StringSplitOptions.RemoveEmptyEntries)
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
