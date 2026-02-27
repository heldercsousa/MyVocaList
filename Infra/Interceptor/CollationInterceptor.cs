using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;
using System.Globalization;

namespace MyVocaList.Infra.Interceptor;

/// <summary>
/// Automatically registers custom SQLite collation and LIKE function on every database connection.
/// This ensures migrations and all database operations have access to NOCASE_NOACCENT collation
/// and accent-insensitive LIKE matching.
///
/// WHY THIS IS NEEDED:
/// - SQLite collations are connection-specific, not database-global
/// - EF Core migrations use separate connections that don't have collations from AppDbContext
/// - SQLite's built-in LIKE operator ignores registered collations entirely — it uses its own
///   internal matching loop with ASCII-only case folding. Searching "Joao" would never match
///   "João" via LIKE regardless of COLLATE clauses. The custom like() function override fixes this.
///
/// COLLATION: NOCASE_NOACCENT
/// - Case insensitive: "João" = "joão" = "JOÃO"
/// - Accent insensitive: "João" = "Joao" = "joao"
///
/// LIKE OVERRIDE:
/// - Replaces SQLite's built-in like(pattern, text) function
/// - Normalizes both sides before matching, making LIKE accent- and case-insensitive
/// - Supports % (any sequence) and _ (any single character) wildcards
/// </summary>
public class CollationInterceptor : DbConnectionInterceptor
{
    /// <inheritdoc />
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        RegisterOnConnection(connection);
        base.ConnectionOpened(connection, eventData);
    }

    /// <inheritdoc />
    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        RegisterOnConnection(connection);
        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
    }

    private static void RegisterOnConnection(DbConnection connection)
    {
        if (connection is not SqliteConnection sqliteConnection)
            return;

        try
        {
            // Register NOCASE_NOACCENT collation — used for =, <>, ORDER BY comparisons.
            sqliteConnection.CreateCollation("NOCASE_NOACCENT", (x, y) =>
                string.Compare(NormalizeForCollation(x), NormalizeForCollation(y), StringComparison.Ordinal));

            // Override SQLite's built-in like(pattern, text) function.
            // CRITICAL: SQLite LIKE ignores registered collations — it has its own internal
            // ASCII-only matching loop. The only way to make LIKE accent-insensitive is to
            // replace the like() function itself. "x LIKE y" in SQL is equivalent to
            // like(y, x) as a function call, so registering like(string, string) overrides
            // the LIKE operator globally on this connection.
            sqliteConnection.CreateFunction(
                "like",
                (string pattern, string value) =>
                    LikeMatch(NormalizeForCollation(value), NormalizeForCollation(pattern)),
                isDeterministic: true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"CollationInterceptor: registration failed — {ex.Message}");
            // Don't throw — allow the connection to proceed
        }
    }

    /// <summary>
    /// Normalizes text for accent- and case-insensitive comparison.
    /// Uses Unicode FormD decomposition to separate base characters from accent combining marks,
    /// strips NonSpacingMark characters (the accents), then returns lowercase.
    /// Examples: "João" → "joao", "Café" → "cafe", "MÜNCHEN" → "munchen"
    /// </summary>
    internal static string NormalizeForCollation(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        var decomposed = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);

        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        return sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }

    /// <summary>
    /// Implements SQLite LIKE pattern matching.
    /// % matches any sequence of characters (including empty).
    /// _ matches exactly one character.
    /// All other characters match literally.
    /// Both <paramref name="text"/> and <paramref name="pattern"/> must already be normalized
    /// (accent-stripped, lowercased) before calling this method.
    /// </summary>
    private static bool LikeMatch(string text, string pattern)
        => LikeMatchCore(text.AsSpan(), pattern.AsSpan());

    private static bool LikeMatchCore(ReadOnlySpan<char> text, ReadOnlySpan<char> pattern)
    {
        while (true)
        {
            if (pattern.IsEmpty)
                return text.IsEmpty;

            if (pattern[0] == '%')
            {
                // Consume consecutive % characters
                var rest = pattern[1..];
                while (!rest.IsEmpty && rest[0] == '%')
                    rest = rest[1..];

                if (rest.IsEmpty)
                    return true; // trailing % matches everything

                // Try matching rest of pattern against every suffix of text
                for (var i = 0; i <= text.Length; i++)
                {
                    if (LikeMatchCore(text[i..], rest))
                        return true;
                }

                return false;
            }

            if (text.IsEmpty)
                return false;

            if (pattern[0] == '_' || pattern[0] == text[0])
            {
                text = text[1..];
                pattern = pattern[1..];
                continue;
            }

            return false;
        }
    }
}
