namespace MyVocaList.Tests.Unit.Services;

/// <summary>IFrame player page markup (REQ-MSRC-07).</summary>
public class YouTubePlayerHtmlBuilderTests
{
    // [AC] REQ-MSRC-07: official IFrame player, inline playback, id embedded
    [Fact]
    public void Build_ContainsVideoIdAndPlaysInline()
    {
        var html = YouTubePlayerHtmlBuilder.Build("dQw4w9WgXcQ");

        Assert.Contains("dQw4w9WgXcQ", html);
        Assert.Contains("playsinline: 1", html);
        Assert.Contains("https://www.youtube.com/iframe_api", html);
    }

    // [AC] REQ-MSRC-07: only a valid 11-character id may reach the page
    [Theory]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("dQw4w9WgXcQ\"><script>")]
    [InlineData("dQw4w9WgXc!")]
    public void Build_InvalidId_ThrowsArgumentException(string id)
    {
        Assert.Throws<ArgumentException>(() => YouTubePlayerHtmlBuilder.Build(id));
    }
}
