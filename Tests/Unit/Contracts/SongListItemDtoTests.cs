using MyVocaList.Contracts.DTOs.List;

namespace MyVocaList.Tests.Unit.Contracts;

public class SongListItemDtoTests
{
    // [AC] REQ-RMV-09: the supporting text of a Songs-list row is the artist name only
    [Fact]
    public void SupportingText_ArtistOnly_IsArtistName()
    {
        var dto = new SongListItemDto(1, "Creep", 2, "Radiohead", null);

        Assert.Equal("Radiohead", dto.SupportingText);
    }

    // [AC] REQ-RMV-09: the artist name is trimmed; the title and the provider never appear in the supporting text
    [Fact]
    public void SupportingText_TrimsArtistName_AndIgnoresTitleAndProvider()
    {
        var dto = new SongListItemDto(1, "Creep (Live)", 2, "  Radiohead  ", "Deezer");

        Assert.Equal("Radiohead", dto.SupportingText);
    }

    // [AC] REQ-RMV-09: a missing artist name gives an empty supporting text
    [Fact]
    public void SupportingText_MissingArtistName_IsEmpty()
    {
        var dto = new SongListItemDto(1, "Creep", 2, null, null);

        Assert.Equal(string.Empty, dto.SupportingText);
    }

    // [AC] REQ-MSRC-16: song picture, then album cover from the release MBID, then artist picture, then none
    [Fact]
    public void ImageUrl_FollowsSongCoverArtistPriority()
    {
        const string mbid = "76df3287-6cda-33eb-8e9a-044b5e15ffdd";
        const string cover = "https://coverartarchive.org/release/76df3287-6cda-33eb-8e9a-044b5e15ffdd/front-250";

        Assert.Equal("s", new SongListItemDto(1, "T", 2, "A", null, "s", "a", mbid).ImageUrl);
        Assert.Equal(cover, new SongListItemDto(1, "T", 2, "A", null, null, "a", mbid).ImageUrl);
        Assert.Equal("a", new SongListItemDto(1, "T", 2, "A", null, null, "a", null).ImageUrl);
        Assert.Null(new SongListItemDto(1, "T", 2, "A", null).ImageUrl);
    }
}
