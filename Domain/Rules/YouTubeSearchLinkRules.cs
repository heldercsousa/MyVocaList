namespace MyVocaList.Domain.Rules;

/// <summary>
/// Builds the "Search on YouTube" link (REQ-MSRC-05). The app only opens this link outside the app;
/// it never calls the YouTube Data API.
/// </summary>
public static class YouTubeSearchLinkRules
{
    private const string ResultsBase = "https://www.youtube.com/results?search_query=";
    private const string KaraokeSuffix = "karaoke";

    /// <summary>Returns the results-page URL for <c>"&lt;title&gt; &lt;artist&gt; karaoke"</c>, URL-encoded.</summary>
    public static string BuildResultsUrl(string title, string artist)
        => ResultsBase + Uri.EscapeDataString($"{title.Trim()} {artist.Trim()} {KaraokeSuffix}");
}
