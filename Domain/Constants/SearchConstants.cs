namespace MyVocaList.Domain.Constants;

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

    /// <summary>
    /// Default maximum number of results returned by a local autocomplete/search entry point.
    /// REQ-UOW-51; Helder's ruling 2026-09-21: "every CRUD search must follow the very same
    /// pattern. No magic numbers anywhere." Five results fills a typical autocomplete dropdown
    /// without overwhelming it, and every local search default now shares this single value.
    /// </summary>
    public const int MaxSearchResults = 5;

    /// <summary>
    /// Debounce delay, in milliseconds, before an autocomplete field requests options after the last keystroke.
    /// REQ-ACF-14 (shared autocomplete field); makes REQ-DXAC-07's ≈300 ms concrete. One value for every autocomplete
    /// field (Helder 2026-09-23/24).
    /// </summary>
    public const int AutocompleteDebounceMilliseconds = 300;

    /// <summary>
    /// Upper bound, in milliseconds, on a remote music-provider search call. REQ-SFT-08: a call not
    /// completed within this bound is abandoned so local options wait at most 3,000 ms (design § 0.3 D16).
    /// </summary>
    public const int RemoteSearchTimeoutMilliseconds = 3000;

    /// <summary>
    /// Upper bound, in milliseconds, on one lyrics lookup, covering both attempts together (REQ-LYR-06).
    /// A lookup not finished within this bound ends with no match.
    /// </summary>
    public const int LyricsLookupTimeoutMilliseconds = 5000;

    /// <summary>
    /// Maximum length, in characters, of lyrics after trimming (REQ-LYR-05). Longer text is treated as no
    /// match, because a cut text would be stored as if it were complete. Exactly this length is kept.
    /// </summary>
    public const int LyricsMaxLength = 10000;
}
