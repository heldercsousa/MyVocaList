namespace MyVocaList.Infra.Collation;

/// <summary>SQLite collation names for this Infra layer.</summary>
public static class CollationConstants
{
    /// <summary>Case- and accent-insensitive collation for name/title search and uniqueness.</summary>
    public const string Default = "NOCASE_NOACCENT";

    /// <summary>Case-insensitive collation for email (accent-insensitive is not meaningful for email).</summary>
    public const string Email = "NOCASE";
}
