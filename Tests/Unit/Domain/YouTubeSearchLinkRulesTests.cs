using MyVocaList.Domain.Rules;

namespace MyVocaList.Tests.Unit.Domain;

public class YouTubeSearchLinkRulesTests
{
    // [AC] REQ-MSRC-05: "Search on YouTube" opens youtube.com/results?search_query=<title> <artist> karaoke
    [Fact]
    public void BuildResultsUrl_BuildsResultsUrlWithKaraokeSuffix()
    {
        var url = YouTubeSearchLinkRules.BuildResultsUrl("Bohemian Rhapsody", "Queen");

        Assert.Equal("https://www.youtube.com/results?search_query=Bohemian%20Rhapsody%20Queen%20karaoke", url);
    }

    // [AC] REQ-MSRC-05: values are trimmed and URL-encoded
    [Fact]
    public void BuildResultsUrl_TrimsAndEncodesSpecialCharacters()
    {
        var url = YouTubeSearchLinkRules.BuildResultsUrl("  Rock & Roll ", " AC/DC ");

        Assert.Equal("https://www.youtube.com/results?search_query=Rock%20%26%20Roll%20AC%2FDC%20karaoke", url);
    }

    // [AC] REQ-MSRC-04: no Data API host is ever produced
    [Fact]
    public void BuildResultsUrl_NeverUsesDataApiHost()
    {
        var url = YouTubeSearchLinkRules.BuildResultsUrl("a", "b");

        Assert.DoesNotContain("googleapis.com", url);
    }
}
