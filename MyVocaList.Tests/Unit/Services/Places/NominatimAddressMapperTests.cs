using MyVocaList.Services.Places;

namespace MyVocaList.Tests.Unit.Services.Places;

/// <summary>
/// V7b — <see cref="NominatimAddressMapper"/>: raw geocoding record to venue place (REQ-VEN-26, 30, 32).
/// Pure mapping; no I/O.
/// </summary>
public class NominatimAddressMapperTests
{
    private const double Latitude = -23.5614;
    private const double Longitude = -46.6559;

    private static GeocodingRecord Record(
        string? name = null,
        string? category = null,
        string? displayName = null,
        params (string Key, string Value)[] address)
    {
        var dictionary = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (key, value) in address)
            dictionary[key] = value;

        return new GeocodingRecord(Latitude, Longitude, name, category, displayName, dictionary);
    }

    // [AC] REQ-VEN-32: street and house number of a country that puts the number first is "{number} {road}" (us)
    [Fact]
    public void ToPlace_NumberFirstCountry_BuildsNumberThenRoad()
    {
        var place = NominatimAddressMapper.ToPlace(Record(address: [("road", "5th Avenue"), ("house_number", "350"), ("country_code", "us")]));

        Assert.Equal("350 5th Avenue", place.Address.Line1);
    }

    // [AC] REQ-VEN-32: street and house number of a Brazilian record is "{road}, {number}" (the example of the spec)
    [Fact]
    public void ToPlace_CommaCountryBrazil_BuildsRoadCommaNumber()
    {
        var place = NominatimAddressMapper.ToPlace(Record(address: [("road", "Rua Augusta"), ("house_number", "100"), ("country_code", "br")]));

        Assert.Equal("Rua Augusta, 100", place.Address.Line1);
    }

    // [AC] REQ-VEN-32: street and house number of any other country is "{road} {number}" (de)
    [Fact]
    public void ToPlace_OtherCountry_BuildsRoadSpaceNumber()
    {
        var place = NominatimAddressMapper.ToPlace(Record(address: [("road", "Hauptstraße"), ("house_number", "5"), ("country_code", "de")]));

        Assert.Equal("Hauptstraße 5", place.Address.Line1);
    }

    // [AC] REQ-VEN-32: a record without a country code uses the "{road} {number}" order
    [Fact]
    public void ToPlace_MissingCountryCode_BuildsRoadSpaceNumber()
    {
        var place = NominatimAddressMapper.ToPlace(Record(address: [("road", "Hauptstraße"), ("house_number", "5")]));

        Assert.Equal("Hauptstraße 5", place.Address.Line1);
    }

    // [AC] REQ-VEN-32: the country code is compared case-insensitively ("BR" uses the comma order)
    [Fact]
    public void ToPlace_UpperCaseCountryCode_IsMatchedCaseInsensitively()
    {
        var place = NominatimAddressMapper.ToPlace(Record(address: [("road", "Rua Augusta"), ("house_number", "100"), ("country_code", "BR")]));

        Assert.Equal("Rua Augusta, 100", place.Address.Line1);
    }

    // [AC] REQ-VEN-32: a road without a house number gives the road alone
    [Fact]
    public void ToPlace_RoadWithoutNumber_GivesRoadAlone()
    {
        var place = NominatimAddressMapper.ToPlace(Record(address: [("road", "Rua Augusta"), ("country_code", "br")]));

        Assert.Equal("Rua Augusta", place.Address.Line1);
    }

    // [AC] REQ-VEN-32: a house number without a road gives no street address
    [Fact]
    public void ToPlace_NumberWithoutRoad_GivesNullLine1()
    {
        var place = NominatimAddressMapper.ToPlace(Record(address: [("house_number", "350"), ("country_code", "us")]));

        Assert.Null(place.Address.Line1);
    }

    // [AC] REQ-VEN-32: "pedestrian" is the road when "road" is missing (a blank road counts as missing)
    [Fact]
    public void ToPlace_PedestrianUsedWhenRoadMissing()
    {
        var place = NominatimAddressMapper.ToPlace(Record(address: [("road", "   "), ("pedestrian", "Calçadão"), ("house_number", "10"), ("country_code", "br")]));

        Assert.Equal("Calçadão, 10", place.Address.Line1);
    }

    // [AC] REQ-VEN-32: "footway" is the road when "road" and "pedestrian" are missing
    [Fact]
    public void ToPlace_FootwayUsedWhenRoadAndPedestrianMissing()
    {
        var place = NominatimAddressMapper.ToPlace(Record(address: [("footway", "Trilha Verde"), ("house_number", "3"), ("country_code", "us")]));

        Assert.Equal("3 Trilha Verde", place.Address.Line1);
    }

    // [AC] REQ-VEN-32: address line 2 takes the first existing key (suburb before neighbourhood)
    [Fact]
    public void ToPlace_Line2_FirstKeyWins()
    {
        var place = NominatimAddressMapper.ToPlace(Record(address: [("suburb", "Consolação"), ("neighbourhood", "Vila Madalena")]));

        Assert.Equal("Consolação", place.Address.Line2);
    }

    // [AC] REQ-VEN-32: address line 2 falls through blank keys to the last listed key (city_district)
    [Fact]
    public void ToPlace_Line2_LastKeyUsedWhenEarlierAreBlank()
    {
        var place = NominatimAddressMapper.ToPlace(Record(address:
            [("suburb", "  "), ("neighbourhood", ""), ("quarter", " "), ("city_district", "Centro")]));

        Assert.Equal("Centro", place.Address.Line2);
    }

    // [AC] REQ-VEN-32: city takes the first existing key (city before town)
    [Fact]
    public void ToPlace_City_FirstKeyWins()
    {
        var place = NominatimAddressMapper.ToPlace(Record(address: [("city", "São Paulo"), ("town", "Osasco")]));

        Assert.Equal("São Paulo", place.Address.City);
    }

    // [AC] REQ-VEN-32: city falls through blank keys to the last listed key (county)
    [Fact]
    public void ToPlace_City_LastKeyUsedWhenEarlierAreBlank()
    {
        var place = NominatimAddressMapper.ToPlace(Record(address:
            [("city", " "), ("town", ""), ("village", " "), ("municipality", ""), ("hamlet", " "), ("county", "Condado")]));

        Assert.Equal("Condado", place.Address.City);
    }

    // [AC] REQ-VEN-32: region takes the first existing key (state before province)
    [Fact]
    public void ToPlace_Region_FirstKeyWins()
    {
        var place = NominatimAddressMapper.ToPlace(Record(address: [("state", "São Paulo"), ("province", "Ontário")]));

        Assert.Equal("São Paulo", place.Address.Region);
    }

    // [AC] REQ-VEN-32: region falls through blank keys to the last listed key (province)
    [Fact]
    public void ToPlace_Region_LastKeyUsedWhenEarlierAreBlank()
    {
        var place = NominatimAddressMapper.ToPlace(Record(address:
            [("state", ""), ("region", " "), ("state_district", "  "), ("province", "Ontário")]));

        Assert.Equal("Ontário", place.Address.Region);
    }

    // [AC] REQ-VEN-32: postal code and country are copied from "postcode" and "country", trimmed
    [Fact]
    public void ToPlace_PostalCodeAndCountry_CopiedAndTrimmed()
    {
        var place = NominatimAddressMapper.ToPlace(Record(address: [("postcode", " 01305-000 "), ("country", " Brasil ")]));

        Assert.Equal("01305-000", place.Address.PostalCode);
        Assert.Equal("Brasil", place.Address.Country);
    }

    // [AC] REQ-VEN-26: the place name is kept when the category is one of the seven venue categories
    [Theory]
    [InlineData("amenity")]
    [InlineData("leisure")]
    [InlineData("tourism")]
    [InlineData("shop")]
    [InlineData("club")]
    [InlineData("craft")]
    [InlineData("office")]
    public void ToPlace_VenueCategory_KeepsName(string category)
    {
        var place = NominatimAddressMapper.ToPlace(Record(name: "Bar Brahma", category: category));

        Assert.Equal("Bar Brahma", place.Name);
    }

    // [AC] REQ-VEN-26: the category is compared case-insensitively ("AMENITY" keeps the name)
    [Fact]
    public void ToPlace_CategoryUpperCase_KeepsName()
    {
        var place = NominatimAddressMapper.ToPlace(Record(name: "Bar Brahma", category: "AMENITY"));

        Assert.Equal("Bar Brahma", place.Name);
    }

    // [AC] REQ-VEN-26: a road or a building is not a known place, so no name (highway, building, null)
    [Theory]
    [InlineData("highway")]
    [InlineData("building")]
    [InlineData(null)]
    public void ToPlace_NonVenueCategory_GivesNullName(string? category)
    {
        var place = NominatimAddressMapper.ToPlace(Record(name: "Rua Augusta", category: category));

        Assert.Null(place.Name);
    }

    // [AC] REQ-VEN-26: a blank name gives no place name, even for a venue category
    [Fact]
    public void ToPlace_BlankName_GivesNullName()
    {
        var place = NominatimAddressMapper.ToPlace(Record(name: "   ", category: "amenity"));

        Assert.Null(place.Name);
    }

    // [AC] REQ-VEN-30: a place name longer than 60 characters arrives cut to 60
    [Fact]
    public void ToPlace_LongName_CutTo60Characters()
    {
        var place = NominatimAddressMapper.ToPlace(Record(name: new string('N', 70), category: "amenity"));

        Assert.Equal(new string('N', 60), place.Name);
    }

    // [AC] REQ-VEN-32: a street longer than 100 characters is cut to 100
    [Fact]
    public void ToPlace_LongRoad_CutTo100Characters()
    {
        var place = NominatimAddressMapper.ToPlace(Record(address: [("road", new string('R', 120))]));

        Assert.Equal(100, place.Address.Line1!.Length);
    }

    // [AC] REQ-VEN-32: a postal code longer than 20 characters is cut to 20
    [Fact]
    public void ToPlace_LongPostcode_CutTo20Characters()
    {
        var place = NominatimAddressMapper.ToPlace(Record(address: [("postcode", new string('9', 30))]));

        Assert.Equal(20, place.Address.PostalCode!.Length);
    }

    // [AC] REQ-VEN-32: a value is cut to its maximum first, then trimmed again (no trailing space left)
    [Fact]
    public void ToPlace_CutThenTrimmed_NoTrailingSpace()
    {
        var road = new string('a', 99) + " bcdef";

        var place = NominatimAddressMapper.ToPlace(Record(address: [("road", road)]));

        Assert.Equal(new string('a', 99), place.Address.Line1);
    }

    // [AC] REQ-VEN-26: the one-line address is street, city, region and country joined by ", "
    [Fact]
    public void ToPlace_AllFourParts_JoinedInOrder()
    {
        var place = NominatimAddressMapper.ToPlace(Record(
            displayName: "ignored display name",
            address: [("road", "Rua Augusta"), ("house_number", "100"), ("country_code", "br"),
                      ("city", "São Paulo"), ("state", "São Paulo"), ("country", "Brasil")]));

        Assert.Equal("Rua Augusta, 100, São Paulo, São Paulo, Brasil", place.AddressLine);
    }

    // [AC] REQ-VEN-26: blank parts of the one-line address are skipped
    [Fact]
    public void ToPlace_MissingParts_SkippedInOneLineAddress()
    {
        var place = NominatimAddressMapper.ToPlace(Record(address:
            [("road", "Rua Augusta"), ("house_number", "100"), ("country_code", "br"), ("country", "Brasil")]));

        Assert.Equal("Rua Augusta, 100, Brasil", place.AddressLine);
    }

    // [AC] REQ-VEN-26: with no mapped street, city, region or country the one-line address is the display name, trimmed
    [Fact]
    public void ToPlace_NoAddressParts_UsesTrimmedDisplayName()
    {
        var place = NominatimAddressMapper.ToPlace(Record(displayName: "  Bar Brahma, Brasil  "));

        Assert.Equal("Bar Brahma, Brasil", place.AddressLine);
    }

    // [AC] REQ-VEN-26: only a line 2 value is not a one-line address part, so the display name is used
    [Fact]
    public void ToPlace_OnlyLine2_UsesDisplayName()
    {
        var place = NominatimAddressMapper.ToPlace(Record(displayName: "Bar Brahma, Consolação, Brasil", address: [("suburb", "Consolação")]));

        Assert.Equal("Bar Brahma, Consolação, Brasil", place.AddressLine);
    }

    // [AC] REQ-VEN-26: no address parts and no display name gives an empty one-line address, never null
    [Fact]
    public void ToPlace_NoAddressPartsAndNullDisplayName_GivesEmptyLine()
    {
        var place = NominatimAddressMapper.ToPlace(Record());

        Assert.Equal(string.Empty, place.AddressLine);
    }

    // [AC] REQ-VEN-32: an empty address gives six null fields; the name still follows the category rule; coordinates copied
    [Fact]
    public void ToPlace_EmptyAddress_SixNullFieldsNameAndCoordinatesKept()
    {
        var place = NominatimAddressMapper.ToPlace(Record(name: "Bar Brahma", category: "amenity"));

        Assert.Null(place.Address.Line1);
        Assert.Null(place.Address.Line2);
        Assert.Null(place.Address.City);
        Assert.Null(place.Address.Region);
        Assert.Null(place.Address.PostalCode);
        Assert.Null(place.Address.Country);
        Assert.Equal("Bar Brahma", place.Name);
        Assert.Equal(Latitude, place.Latitude);
        Assert.Equal(Longitude, place.Longitude);
    }

    // [AC] REQ-VEN-32: accents and letter case of values are kept as the service returns them
    [Fact]
    public void ToPlace_AccentsAndCaseKept()
    {
        var place = NominatimAddressMapper.ToPlace(Record(address:
            [("road", "RUA AUGUSTA"), ("house_number", "100"), ("country_code", "br"), ("city", "São Paulo"), ("state", "São Paulo")]));

        Assert.Equal("RUA AUGUSTA, 100", place.Address.Line1);
        Assert.Equal("São Paulo", place.Address.City);
        Assert.Equal("São Paulo", place.Address.Region);
    }
}
