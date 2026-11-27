using System.Text;

namespace MyVocaList.Infra.Repository;

/// <summary>Builds FTS5 MATCH expressions for the trigram search tables (design.md § 6.2).</summary>
public static class FtsQueryBuilder
{
    private const int WindowLength = 3;

    /// <summary>
    /// AND over the whitespace-separated words of length &gt;= 3; each word is the OR of its distinct 3-char windows
    /// (each double-quoted with '"' doubled), e.g. ("don" OR "ont") AND ("cry"). Every word must match.
    /// Returns null when no word has &gt;= 3 chars (caller uses the prefix path).
    /// Query construction only; case/diacritic folding is done by the SQLite tokenizer.
    /// </summary>
    /// <param name="query">Raw user query.</param>
    public static string? BuildTrigramMatch(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return null;

        var groups = new List<string>();

        foreach (var word in query.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var sb = new StringBuilder();
            for (var i = 0; i + WindowLength <= word.Length; i++)
            {
                var window = word.Substring(i, WindowLength);
                if (!seen.Add(window)) continue;

                if (sb.Length > 0) sb.Append(" OR ");
                sb.Append('"').Append(window.Replace("\"", "\"\"")).Append('"');
            }
            if (sb.Length > 0) groups.Add("(" + sb + ")");
        }

        return groups.Count == 0 ? null : string.Join(" AND ", groups);
    }
}
