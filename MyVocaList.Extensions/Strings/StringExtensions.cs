namespace MyVocaList.Extensions.Strings;

/// <summary>
/// Whitespace-only normalization. Deliberately does NOT case-fold or strip diacritics —
/// that is owned by DB collation (constraints-registry § EF Core/SQLite HARD RULE) and must
/// never be reimplemented here. Do not conflate the two when extending this class.
/// </summary>
public static class StringExtensions
{
    /// <summary>Edge-trim + collapse internal whitespace runs to one space. Null/whitespace → "".</summary>
    public static string NormalizeSearchQuery(this string query)
        => string.IsNullOrWhiteSpace(query) ? string.Empty : Collapse(query);

    /// <summary>Storage form of a required field. Null → null; else edge-trim + internal collapse (D1).</summary>
    public static string TrimForStorage(this string value)
        => value is null ? null : Collapse(value);

    /// <summary>Storage form of an optional field. Empty/whitespace-only result → null.</summary>
    public static string TrimForStorageOrNull(this string value)
    {
        var result = TrimForStorage(value);
        return string.IsNullOrEmpty(result) ? null : result;
    }

    private static string Collapse(string value)
        => string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
