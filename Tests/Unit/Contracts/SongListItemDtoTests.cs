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
}
