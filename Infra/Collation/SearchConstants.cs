namespace MyVocaList.Infra.Collation;

/// <summary>Minimum query-length thresholds for search entry points, before which no query is issued.</summary>
public static class SearchConstants
{
    /// <summary>
    /// Minimum characters required before firing a local SQLite search query.
    /// REQ-UOW-51; see `design.md § 2c` (Minimum query length). A local read costs ~1 ms with no
    /// network or quota involved, but at 1 character it matches an unbounded fraction of rows with no
    /// discriminating value; 2 characters is usefully narrow on an indexed collated column.
    /// </summary>
    public const int MinimumLocalQueryLength = 2;

    /// <summary>
    /// Minimum characters required before firing a remote HTTP provider search query.
    /// REQ-UOW-52; see `design.md § 2c` (Minimum query length). A remote read costs a network round
    /// trip against a third-party rate-limit budget and device battery, and even at 2 characters the
    /// result set against a global catalogue is still very broad, so a higher bar than local is used.
    /// </summary>
    public const int MinimumRemoteQueryLength = 3;
}
