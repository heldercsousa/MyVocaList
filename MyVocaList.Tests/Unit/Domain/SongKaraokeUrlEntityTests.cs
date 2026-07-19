namespace MyVocaList.Tests.Unit.Domain;

public class SongKaraokeUrlEntityTests
{
    [Fact]
    public void SongKaraokeUrl_DefaultPlayCount_IsZero()
    {
        var url = new SongKaraokeUrl { VideoId = "dQw4w9WgXcQ", SongId = 1 };
        Assert.Equal(0, url.PlayCount);
    }
}
