using System.Text;

namespace MyVocaList.Infra.Repository;

/// <summary>Builds FTS5 MATCH expressions for the trigram search tables (design.md § 6.2).</summary>
public static class FtsQueryBuilder
{
    private const int WindowLength = 3;

    /// <summary>
    /// OR of the distinct 3-char windows of every whitespace-separated word of length &gt;= 3,
    /// each double-quoted with '"' doubled. Returns null when no word has &gt;= 3 chars (caller uses the prefix path).
    /// Query construction only; case/diacritic folding is done by the SQLite tokenizer.
    /// </summary>
    /// <param name="query">Raw user query.</param>
    public static string? BuildTrigramMatch(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return null;

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var sb = new StringBuilder();

        foreach (var word in query.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            for (var i = 0; i + WindowLength <= word.Length; i++)
            {
                var window = word.Substring(i, WindowLength);
                if (!seen.Add(window)) continue;

                if (sb.Length > 0) sb.Append(" OR ");
                sb.Append('"').Append(window.Replace("\"", "\"\"")).Append('"');
            }
        }

        return sb.Length == 0 ? null : sb.ToString();
    }
}
