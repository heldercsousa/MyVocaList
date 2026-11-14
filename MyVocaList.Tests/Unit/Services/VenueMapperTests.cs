using MyVocaList.Domain.Entity;
using MyVocaList.Services.Mappers;

namespace MyVocaList.Tests.Unit.Services;

public class VenueMapperTests
{
    private const string LocalImage = "local:venues/0123456789abcdef0123456789abcdef";

    private static Venue NewVenue(string? imageUrl = null, string? line1 = null, string? line2 = null,
        string? city = null, string? region = null, string? postalCode = null, string? country = null) => new()
    {
        Id = 7,
        Name = "Bar Brahma",
        ImageUrl = imageUrl,
        AddressLine1 = line1,
        AddressLine2 = line2,
        City = city,
        Region = region,
        PostalCode = postalCode,
        Country = country
    };

    [Fact]
    public void ToListDto_PassesImageReferenceThrough()
    {
        // [AC] REQ-VEN-06: list row carries the venue picture reference
        var dto = VenueMapper.ToListDto(NewVenue(imageUrl: LocalImage), 0);

        Assert.Equal(LocalImage, dto.ImageUrl);
    }

    [Fact]
    public void ToListDto_NullImage_LeavesImageUrlNull()
    {
        // [AC] REQ-VEN-06: a venue without a picture carries no reference (row shows the default image)
        var dto = VenueMapper.ToListDto(NewVenue(imageUrl: null), 0);

        Assert.Null(dto.ImageUrl);
    }

    [Fact]
    public void ToListDto_FullAddress_SummaryIsStreetAndCityOnly()
    {
        // [AC] REQ-VEN-18: supporting text is Street address and City joined by ", "
        var dto = VenueMapper.ToListDto(
            NewVenue(line1: "Main Street 1", line2: "Suite 2", city: "Springfield", region: "IL", postalCode: "62701", country: "USA"), 0);

        Assert.Equal("Main Street 1, Springfield", dto.AddressSummary);
    }

    [Fact]
    public void ToListDto_PartialAddress_SkipsBlankPart()
    {
        // [AC] REQ-VEN-18: a blank part is skipped, the other part is shown alone
        var dto = VenueMapper.ToListDto(NewVenue(line1: null, city: "Springfield", country: "USA"), 0);

        Assert.Equal("Springfield", dto.AddressSummary);
    }

    [Fact]
    public void ToListDto_EmptyAddress_SummaryIsNull()
    {
        // [AC] REQ-VEN-18: a venue with an empty address shows no supporting text
        var dto = VenueMapper.ToListDto(NewVenue(line1: "", line2: "   ", city: " ", region: null), 0);

        Assert.Null(dto.AddressSummary);
    }
}
