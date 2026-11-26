using MyVocaList.Domain.Rules;

namespace MyVocaList.Tests.Unit.Domain;

public class YouTubeLinkRulesTests
{
    private const string Id = "dQw4w9WgXcQ";

    // [AC] REQ-MSRC-06: youtu.be/{id} stores only the 11-character id
    [Fact]
    public void TryExtractVideoId_YoutuBe_ReturnsId()
    {
        Assert.True(YouTubeLinkRules.TryExtractVideoId($"https://youtu.be/{Id}", out var id));
        Assert.Equal(Id, id);
    }

    // [AC] REQ-MSRC-06: watch?v= with &t and &list extras still yields the id
    [Fact]
    public void TryExtractVideoId_WatchWithTimeAndList_ReturnsId()
    {
        Assert.True(YouTubeLinkRules.TryExtractVideoId($"https://www.youtube.com/watch?v={Id}&t=42s&list=PL123", out var id));
        Assert.Equal(Id, id);
    }

    // [AC] REQ-MSRC-06: /shorts/{id}
    [Fact]
    public void TryExtractVideoId_Shorts_ReturnsId()
    {
        Assert.True(YouTubeLinkRules.TryExtractVideoId($"https://www.youtube.com/shorts/{Id}", out var id));
        Assert.Equal(Id, id);
    }

    // [AC] REQ-MSRC-06: /embed/{id}
    [Fact]
    public void TryExtractVideoId_Embed_ReturnsId()
    {
        Assert.True(YouTubeLinkRules.TryExtractVideoId($"https://www.youtube.com/embed/{Id}", out var id));
        Assert.Equal(Id, id);
    }

    // [AC] REQ-MSRC-06: m. and music. hosts accepted
    [Theory]
    [InlineData("https://m.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://music.youtube.com/watch?v=dQw4w9WgXcQ")]
    public void TryExtractVideoId_MobileHost_ReturnsId(string url)
    {
        Assert.True(YouTubeLinkRules.TryExtractVideoId(url, out var id));
        Assert.Equal(Id, id);
    }

    // [AC] REQ-MSRC-06: shared text with surrounding words/spaces is parsed
    [Fact]
    public void TryExtractVideoId_SurroundingSpaces_ReturnsId()
    {
        Assert.True(YouTubeLinkRules.TryExtractVideoId($"  Check this https://youtu.be/{Id}?si=abc  ", out var id));
        Assert.Equal(Id, id);
    }

    // [AC] REQ-MSRC-06: playlist-only links are rejected
    [Fact]
    public void TryExtractVideoId_PlaylistOnly_ReturnsFalse()
    {
        Assert.False(YouTubeLinkRules.TryExtractVideoId("https://www.youtube.com/playlist?list=PLabcdefghijk", out var id));
        Assert.Equal(string.Empty, id);
    }

    // [AC] REQ-MSRC-06: non-YouTube hosts and empty input are rejected
    [Theory]
    [InlineData("https://vimeo.com/dQw4w9WgXcQ")]
    [InlineData("https://evil.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("")]
    [InlineData(null)]
    public void TryExtractVideoId_NonYouTube_ReturnsFalse(string? text)
    {
        Assert.False(YouTubeLinkRules.TryExtractVideoId(text, out _));
    }
}
