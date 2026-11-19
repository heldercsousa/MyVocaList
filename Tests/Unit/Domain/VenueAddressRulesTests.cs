using MyVocaList.Contracts.DTOs.Venues;
using MyVocaList.Domain.Rules;

namespace MyVocaList.Tests.Unit.Domain;

public class VenueAddressRulesTests
{
    private static VenueAddress Full() => new("Rua Augusta, 100", "Loja 2", "São Paulo", "SP", "01304-000", "Brasil");

    // [AC] REQ-VEN-16: each address field is trimmed on save and a blank field is stored as null.
    [Fact]
    public void Normalize_TrimsEachFieldAndTurnsBlankIntoNull()
    {
        var result = VenueAddressRules.Normalize(new VenueAddress("  Rua A  ", "   ", "", " São Paulo ", null, "\t"));

        Assert.Equal("Rua A", result.Line1);
        Assert.Null(result.Line2);
        Assert.Null(result.City);
        Assert.Equal("São Paulo", result.Region);
        Assert.Null(result.PostalCode);
        Assert.Null(result.Country);
    }

    // [AC] REQ-VEN-16: each address field is trimmed on save and a blank field is stored as null.
    [Fact]
    public void Normalize_NullInput_ReturnsEmptyAddress()
        => Assert.Equal(VenueAddress.Empty, VenueAddressRules.Normalize(null));

    // [AC] REQ-VEN-16: normalising never changes letter case or accents.
    [Fact]
    public void Normalize_KeepsLetterCaseAndAccents()
    {
        var result = VenueAddressRules.Normalize(new VenueAddress("rua AUGUSTA", null, "São Paulo", "sp", null, "brasil"));

        Assert.Equal("rua AUGUSTA", result.Line1);
        Assert.Equal("São Paulo", result.City);
        Assert.Equal("sp", result.Region);
        Assert.Equal("brasil", result.Country);
    }

    // [AC] REQ-VEN-13: a venue saves with an empty address.
    [Fact]
    public void IsEmpty_AllFieldsNull_IsTrue()
        => Assert.True(VenueAddressRules.IsEmpty(VenueAddress.Empty));

    // [AC] REQ-VEN-13: a venue saves with an empty address (white space counts as blank).
    [Fact]
    public void IsEmpty_AllFieldsWhiteSpace_IsTrue()
        => Assert.True(VenueAddressRules.IsEmpty(new VenueAddress(" ", "\t", "  ", "", " ", "   ")));

    // [AC] REQ-VEN-13: a venue saves with an empty address (one filled field makes it non-empty).
    [Fact]
    public void IsEmpty_OneFieldFilled_IsFalse()
        => Assert.False(VenueAddressRules.IsEmpty(VenueAddress.Empty with { Country = "Brasil" }));

    // [AC] REQ-VEN-13: a venue saves with an empty address.
    [Fact]
    public void Validate_EmptyAddress_IsValid()
    {
        var result = VenueAddressRules.Validate(VenueAddress.Empty);

        Assert.Equal((true, "", VenueField.None), result);
    }

    // [AC] REQ-VEN-14: a started address needs a street.
    [Fact]
    public void Validate_OnlyCountry_ReportsStreetAddressRequired()
    {
        var result = VenueAddressRules.Validate(VenueAddress.Empty with { Country = "Brasil" });

        Assert.Equal((false, "Street address is required.", VenueField.AddressLine1), result);
    }

    // [AC] REQ-VEN-14: a started address needs a street (white space is blank).
    [Fact]
    public void Validate_WhiteSpaceLine1_ReportsStreetAddressRequired()
    {
        var result = VenueAddressRules.Validate(new VenueAddress("   ", null, "São Paulo", null, null, null));

        Assert.Equal((false, "Street address is required.", VenueField.AddressLine1), result);
    }

    // [AC] REQ-VEN-14: a started address needs a city.
    [Fact]
    public void Validate_BlankCity_ReportsCityRequired()
    {
        var result = VenueAddressRules.Validate(new VenueAddress("Rua Augusta, 100", null, null, "SP", null, "Brasil"));

        Assert.Equal((false, "City is required.", VenueField.City), result);
    }

    // [AC] REQ-VEN-14: a started address needs street and city; the street is reported first.
    [Fact]
    public void Validate_BlankLine1AndBlankCity_ReportsLine1First()
    {
        var result = VenueAddressRules.Validate(VenueAddress.Empty with { Country = "Brasil", Region = "SP" });

        Assert.Equal((false, "Street address is required.", VenueField.AddressLine1), result);
    }

    // [AC] REQ-VEN-14: a started address with street and city is valid.
    [Fact]
    public void Validate_StreetAndCityOnly_IsValid()
    {
        var result = VenueAddressRules.Validate(new VenueAddress("Rua Augusta, 100", null, "São Paulo", null, null, null));

        Assert.Equal((true, "", VenueField.None), result);
    }

    // [AC] REQ-VEN-15: a value of exactly the maximum length is valid.
    [Fact]
    public void Validate_EveryFieldAtMaximum_IsValid()
    {
        var address = new VenueAddress(
            Text(VenueAddressRules.Line1Max),
            Text(VenueAddressRules.Line2Max),
            Text(VenueAddressRules.CityMax),
            Text(VenueAddressRules.RegionMax),
            Text(VenueAddressRules.PostalCodeMax),
            Text(VenueAddressRules.CountryMax));

        Assert.Equal((true, "", VenueField.None), VenueAddressRules.Validate(address));
    }

    // [AC] REQ-VEN-15: a value longer than the maximum is refused with the field label and the maximum.
    [Theory]
    [InlineData(VenueField.AddressLine1, "Street address", 100)]
    [InlineData(VenueField.AddressLine2, "Address line 2", 100)]
    [InlineData(VenueField.City, "City", 80)]
    [InlineData(VenueField.Region, "State / Province / Region", 80)]
    [InlineData(VenueField.PostalCode, "Postal code", 20)]
    [InlineData(VenueField.Country, "Country", 60)]
    public void Validate_OneCharacterOverMaximum_ReportsTooLong(VenueField field, string label, int max)
    {
        var address = SetField(Full(), field, Text(max + 1));

        Assert.Equal((false, $"{label} is too long. Maximum {max} characters.", field), VenueAddressRules.Validate(address));
    }

    // [AC] REQ-VEN-15: a value longer than the maximum is refused; the first failing field in field order is reported.
    [Fact]
    public void Validate_TwoFieldsTooLong_ReportsTheFirstInFieldOrder()
    {
        var address = Full() with { Region = Text(81), PostalCode = Text(21) };

        Assert.Equal((false, "State / Province / Region is too long. Maximum 80 characters.", VenueField.Region),
            VenueAddressRules.Validate(address));
    }

    // [AC] REQ-VEN-14: a blank street and a too long city: the required check comes before the length checks.
    [Fact]
    public void Validate_BlankLine1AndCityTooLong_ReportsRequiredBeforeLength()
    {
        var address = VenueAddress.Empty with { City = Text(81), Country = Text(61) };

        Assert.Equal((false, "Street address is required.", VenueField.AddressLine1), VenueAddressRules.Validate(address));
    }

    // [AC] REQ-VEN-15: a value of exactly the maximum is valid when checked on its own (no false positive on trimmed text).
    [Fact]
    public void Validate_TrimmedLengthDecidesTheLimit()
    {
        var address = Full() with { PostalCode = "  " + Text(20) + "  " };

        Assert.Equal((true, "", VenueField.None), VenueAddressRules.Validate(address));
    }

    // [AC] REQ-VEN-18: the list row shows Street address and City joined by ", ".
    [Fact]
    public void Summary_StreetAndCity_JoinsThem()
        => Assert.Equal("Rua Augusta, 100, São Paulo", VenueAddressRules.Summary(Full()));

    // [AC] REQ-VEN-18: the list row shows Street address and City; a blank part is skipped.
    [Theory]
    [InlineData("Rua Augusta, 100", null)]
    [InlineData(null, "São Paulo")]
    public void Summary_OnePartMissing_ShowsTheOtherPart(string? line1, string? city)
    {
        var address = new VenueAddress(line1, "Loja 2", city, "SP", null, "Brasil");

        Assert.Equal(line1 ?? city, VenueAddressRules.Summary(address));
    }

    // [AC] REQ-VEN-18: a venue with an empty address shows no supporting text.
    [Fact]
    public void Summary_BothPartsBlank_IsNull()
        => Assert.Null(VenueAddressRules.Summary(VenueAddress.Empty with { Region = "SP", Country = "Brasil" }));

    // [AC] REQ-VEN-24: the search text is the non-blank fields in field order joined by ", ".
    [Fact]
    public void ToSearchText_AllFields_JoinsInFieldOrder()
        => Assert.Equal("Rua Augusta, 100, Loja 2, São Paulo, SP, 01304-000, Brasil", VenueAddressRules.ToSearchText(Full()));

    // [AC] REQ-VEN-24: the search text skips blank fields.
    [Fact]
    public void ToSearchText_SomeFieldsBlank_SkipsThem()
    {
        var address = new VenueAddress("Rua Augusta, 100", " ", "São Paulo", null, null, "Brasil");

        Assert.Equal("Rua Augusta, 100, São Paulo, Brasil", VenueAddressRules.ToSearchText(address));
    }

    // [AC] REQ-VEN-24: the search text is null when the address is empty.
    [Fact]
    public void ToSearchText_EmptyAddress_IsNull()
        => Assert.Null(VenueAddressRules.ToSearchText(VenueAddress.Empty));

    // [AC] REQ-VEN-33: a latitude and longitude pair in range is kept as given, boundaries included.
    [Theory]
    [InlineData(90.0, 180.0)]
    [InlineData(-90.0, -180.0)]
    [InlineData(-23.5505, -46.6333)]
    public void NormalizeCoordinates_InRangeIncludingBoundaries_KeepsBoth(double latitude, double longitude)
        => Assert.Equal(((double?)latitude, (double?)longitude), VenueAddressRules.NormalizeCoordinates(latitude, longitude));

    // [AC] REQ-VEN-33: a latitude or longitude out of range makes both null.
    [Theory]
    [InlineData(90.0001, 0.0)]
    [InlineData(-90.0001, 0.0)]
    [InlineData(0.0, 180.0001)]
    [InlineData(0.0, -180.0001)]
    public void NormalizeCoordinates_OutOfRange_BothNull(double latitude, double longitude)
        => Assert.Equal(((double?)null, (double?)null), VenueAddressRules.NormalizeCoordinates(latitude, longitude));

    // [AC] REQ-VEN-33: a single coordinate is never stored; one null makes both null.
    [Fact]
    public void NormalizeCoordinates_OneValueNull_BothNull()
    {
        Assert.Equal(((double?)null, (double?)null), VenueAddressRules.NormalizeCoordinates(10.0, null));
        Assert.Equal(((double?)null, (double?)null), VenueAddressRules.NormalizeCoordinates(null, 10.0));
    }

    // [AC] REQ-VEN-33: a NaN or infinite coordinate is not stored.
    [Theory]
    [InlineData(double.NaN, 10.0)]
    [InlineData(10.0, double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity, 10.0)]
    public void NormalizeCoordinates_NaNOrInfinite_BothNull(double latitude, double longitude)
        => Assert.Equal(((double?)null, (double?)null), VenueAddressRules.NormalizeCoordinates(latitude, longitude));

    private static string Text(int length) => new('x', length);

    private static VenueAddress SetField(VenueAddress address, VenueField field, string value) => field switch
    {
        VenueField.AddressLine1 => address with { Line1 = value },
        VenueField.AddressLine2 => address with { Line2 = value },
        VenueField.City => address with { City = value },
        VenueField.Region => address with { Region = value },
        VenueField.PostalCode => address with { PostalCode = value },
        VenueField.Country => address with { Country = value },
        _ => throw new ArgumentOutOfRangeException(nameof(field), field, null)
    };
}
