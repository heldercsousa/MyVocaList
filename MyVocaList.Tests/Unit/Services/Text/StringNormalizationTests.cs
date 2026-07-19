namespace MyVocaList.Tests.Unit.Services.Text;

using MyVocaList.Services.Text;

public class StringNormalizationTests
{
    // [AC] REQ-TRIM-08: NormalizeSearchQuery — null/whitespace-only → string.Empty
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    [InlineData("\t \n", "")]
    public void NormalizeSearchQuery_NullOrWhitespace_ReturnsEmpty(string input, string expected)
        => Assert.Equal(expected, StringNormalization.NormalizeSearchQuery(input));

    // [AC] REQ-TRIM-01: edge + internal whitespace normalize to single-spaced trimmed query
    [Theory]
    [InlineData("  jo", "jo")]
    [InlineData("jo ", "jo")]
    [InlineData("jo  hn", "jo hn")]
    [InlineData("  jo \t hn  ", "jo hn")]
    [InlineData("jo hn", "jo hn")]
    public void NormalizeSearchQuery_ExtraWhitespace_CollapsesAndTrims(string input, string expected)
        => Assert.Equal(expected, StringNormalization.NormalizeSearchQuery(input));

    // [AC] REQ-TRIM-08: TrimForStorage — null passes through as null
    [Fact]
    public void TrimForStorage_Null_ReturnsNull()
        => Assert.Null(StringNormalization.TrimForStorage(null));

    // [AC] REQ-TRIM-06: internal whitespace runs collapsed on storage (D1 approved)
    [Theory]
    [InlineData(" John  Doe ", "John Doe")]
    [InlineData("John Doe", "John Doe")]
    [InlineData("  ", "")]
    public void TrimForStorage_Whitespace_EdgeTrimsAndCollapses(string input, string expected)
        => Assert.Equal(expected, StringNormalization.TrimForStorage(input));

    // [AC] REQ-TRIM-07: optional fields — empty/whitespace-only persists as null
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TrimForStorageOrNull_NullOrWhitespace_ReturnsNull(string input)
        => Assert.Null(StringNormalization.TrimForStorageOrNull(input));

    // [AC] REQ-TRIM-07: optional fields with content are normalized like required ones
    [Theory]
    [InlineData(" a@b.c ", "a@b.c")]
    [InlineData("x  y", "x y")]
    public void TrimForStorageOrNull_WithContent_Normalizes(string input, string expected)
        => Assert.Equal(expected, StringNormalization.TrimForStorageOrNull(input));

    // [AC] REQ-TRIM-10: no case folding / diacritic changes — content preserved verbatim
    [Fact]
    public void Normalization_NeverAltersCaseOrDiacritics()
        => Assert.Equal("Ça VA", StringNormalization.NormalizeSearchQuery("  Ça  VA "));
}
