namespace MyVocaList.Contracts.DTOs;

/// <summary>The answer category of one lyrics lookup. <see cref="NoAnswer"/> is 0 so that a default value reads as "no answer".</summary>
public enum LyricsLookupStatus
{
    /// <summary>No usable answer: offline, timeout, failure or the lookup was cancelled (REQ-SLD-23).</summary>
    NoAnswer = 0,

    /// <summary>The lyrics source answered that it has no usable lyrics (REQ-SLD-22).</summary>
    NoMatch = 1,

    /// <summary>The lyrics source returned lyrics text.</summary>
    Found = 2
}

/// <summary>
/// The outcome of one lyrics lookup. <c>default</c> is <see cref="NoAnswer"/>, so a loose mock that returns the
/// default value reads as "no answer".
/// </summary>
public readonly record struct LyricsLookupResult(LyricsLookupStatus Status, string? Lyrics = null)
{
    /// <summary>A lookup with no answer.</summary>
    public static LyricsLookupResult NoAnswer => new(LyricsLookupStatus.NoAnswer);

    /// <summary>A lookup the lyrics source answered with no usable lyrics.</summary>
    public static LyricsLookupResult NoMatch => new(LyricsLookupStatus.NoMatch);

    /// <summary>A lookup that returned the given lyrics text.</summary>
    /// <param name="lyrics">The lyrics text returned by the lyrics source.</param>
    public static LyricsLookupResult Found(string lyrics) => new(LyricsLookupStatus.Found, lyrics);
}
