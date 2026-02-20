using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;
using System.Globalization;

namespace MyVocaList.Infra.Interceptor;

/// <summary>
/// Automatically registers custom SQLite collation on every database connection.
/// This ensures migrations and all database operations have access to NOCASE_NOACCENT collation.
///
/// WHY THIS IS NEEDED:
/// - SQLite collations are connection-specific, not database-global
/// - EF Core migrations use separate connections that don't have collations from AppDbContext
/// - This interceptor ensures every connection gets the collation registered automatically
///
/// COLLATION: NOCASE_NOACCENT
/// - Case insensitive: "João" = "joão" = "JOÃO"
/// - Accent insensitive: "João" = "Joao" = "joao"
/// </summary>
public class CollationInterceptor : DbConnectionInterceptor
{
    /// <summary>
    /// Intercepts connection opened event to register collation
    /// </summary>
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        RegisterCollationOnConnection(connection);
        base.ConnectionOpened(connection, eventData);
    }

    /// <summary>
    /// Intercepts asynchronous connection opened event to register collation
    /// </summary>
    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        RegisterCollationOnConnection(connection);
        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
    }

    /// <summary>
    /// Registers NOCASE_NOACCENT collation on the SQLite connection
    /// </summary>
    private void RegisterCollationOnConnection(DbConnection connection)
    {
        if (connection is not SqliteConnection sqliteConnection)
        {
            // Not a SQLite connection - skip collation registration
            return;
        }

        try
        {
            // CRITICAL: Connection must be OPEN before CreateCollation() is called
            // The interceptor is called AFTER connection is opened (ConnectionOpened event)
            // so the connection state should be Open at this point
            if (sqliteConnection.State != System.Data.ConnectionState.Open)
            {
                Console.WriteLine($"⚠️ CollationInterceptor: Connection is not open (state: {sqliteConnection.State}). Collation registration may fail.");
            }

            // Register NOCASE_NOACCENT collation
            // This collation removes accents and converts to lowercase for comparison
            sqliteConnection.CreateCollation("NOCASE_NOACCENT", (x, y) =>
            {
                var normalizedX = NormalizeForCollation(x);
                var normalizedY = NormalizeForCollation(y);
                return string.Compare(normalizedX, normalizedY, StringComparison.OrdinalIgnoreCase);
            });

            Console.WriteLine($"✅ NOCASE_NOACCENT collation registered on connection {sqliteConnection.GetHashCode()}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ CollationInterceptor: Failed to register NOCASE_NOACCENT collation - {ex.Message}");
            // Don't throw - allow the connection to proceed even if collation registration fails
            // This prevents breaking the app if collation registration has issues
        }
    }

    /// <summary>
    /// Normalizes text for collation: removes accents and converts to lowercase.
    /// Supports Portuguese, Spanish, French, and other Latin-based languages.
    ///
    /// Examples:
    /// - "João" → "joao"
    /// - "Café" → "cafe"
    /// - "MÜNCHEN" → "munchen"
    /// </summary>
    private static string NormalizeForCollation(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        // Remove accents using Unicode normalization (FormD = decomposed form)
        // This separates base characters from their accents
        // Example: "é" (U+00E9) → "e" (U+0065) + "´" (U+0301)
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);

            // Keep only characters that are NOT accents (NonSpacingMark)
            // This removes the accent marks while keeping the base characters
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        // Convert back to composed form (FormC) and lowercase
        // This ensures consistent representation of Unicode characters
        return stringBuilder.ToString()
            .Normalize(NormalizationForm.FormC)
            .ToLowerInvariant();
    }
}
