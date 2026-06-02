namespace MyVocaList.Infra.Collation;

/// <summary>
/// Collation names used across EF Core configurations and repository queries.
/// When adding a new database provider, update these constants and register the
/// equivalent collation in a provider-specific interceptor or DbContext setup.
/// Provider reference:
///   SQLite     — "NOCASE_NOACCENT" (registered via CollationInterceptor)
///   MSSQL      — "SQL_Latin1_General_CP1_CI_AI"
///   MySQL      — "utf8mb4_0900_ai_ci"
///   PostgreSQL — "und-x-icu" (with ICU) or citext extension
/// </summary>
public static class CollationConstants
{
    /// <summary>Case- and accent-insensitive collation for name/title search and uniqueness.</summary>
    public const string Default = "NOCASE_NOACCENT";

    /// <summary>Case-insensitive collation for email (accent-insensitive is not meaningful for email).</summary>
    public const string Email = "NOCASE";
}
