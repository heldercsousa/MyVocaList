using MyVocaList.Contracts.DTOs.List;

namespace MyVocaList.Tests.Unit.Contracts;

public class SongListItemDtoTests
{
    // [AC] REQ-ART-20: artist only
    [Fact]
    public void SupportingText_ArtistOnly_IsArtistName()
    {
        var dto = new SongListItemDto(1, "Creep", 2, "Radiohead", null, null);

        Assert.Equal("Radiohead", dto.SupportingText);
    }

    // [AC] REQ-ART-20: artist and version, joined by " · "
    [Fact]
    public void SupportingText_ArtistAndVersion_IsArtistDotVersion()
    {
        var dto = new SongListItemDto(1, "Creep", 2, "Radiohead", null, null, Version: "Live");

        Assert.Equal("Radiohead · Live", dto.SupportingText);
    }

    // [AC] REQ-ART-20: artist and featured artists, the featured part carries the "feat. " prefix
    [Fact]
    public void SupportingText_ArtistAndFeatured_IsArtistDotFeat()
    {
        var dto = new SongListItemDto(1, "Creep", 2, "Radiohead", "Thom Yorke", null);

        Assert.Equal("Radiohead · feat. Thom Yorke", dto.SupportingText);
    }

    // [AC] REQ-ART-20: all three parts in the order artist, version, featured
    [Fact]
    public void SupportingText_AllThree_IsInOrder()
    {
        var dto = new SongListItemDto(1, "Creep", 2, "Radiohead", "Thom Yorke", null, Version: "Live");

        Assert.Equal("Radiohead · Live · feat. Thom Yorke", dto.SupportingText);
    }

    // [AC] REQ-ART-20: an empty artist name is omitted, the other parts remain
    [Fact]
    public void SupportingText_EmptyArtistName_OmitsArtistPart()
    {
        var dto = new SongListItemDto(1, "Creep", 2, "", "Thom Yorke", null, Version: "Live");

        Assert.Equal("Live · feat. Thom Yorke", dto.SupportingText);
    }

    // [AC] REQ-ART-20: whitespace-only version and featured artists count as empty
    [Fact]
    public void SupportingText_WhitespaceParts_AreOmitted()
    {
        var dto = new SongListItemDto(1, "Creep", 2, "Radiohead", "   ", null, Version: "  ");

        Assert.Equal("Radiohead", dto.SupportingText);
    }
}
