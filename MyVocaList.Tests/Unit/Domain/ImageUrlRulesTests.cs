using MyVocaList.Domain.Rules;

namespace MyVocaList.Tests.Unit.Domain;

public class ImageUrlRulesTests
{
    [Fact]
    // [AC] REQ-ART-03: an absolute https URL is kept as it is.
    public void Normalize_HttpsUrl_IsKept()
        => Assert.Equal("https://cdn.example.com/cover/abc.jpg", ImageUrlRules.Normalize("https://cdn.example.com/cover/abc.jpg"));

    [Fact]
    // [AC] REQ-ART-03: surrounding spaces are trimmed before the URL is kept.
    public void Normalize_HttpsUrlWithSurroundingSpaces_IsTrimmed()
        => Assert.Equal("https://cdn.example.com/a.jpg", ImageUrlRules.Normalize("  https://cdn.example.com/a.jpg \t"));

    [Theory]
    [InlineData("http://cdn.example.com/a.jpg")]
    [InlineData("/cover/a.jpg")]
    [InlineData("cdn.example.com/a.jpg")]
    [InlineData("ftp://cdn.example.com/a.jpg")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    // [AC] REQ-ART-03: anything that is not an absolute https URL is treated as no image.
    public void Normalize_NotAnHttpsUrl_ReturnsNull(string? value)
        => Assert.Null(ImageUrlRules.Normalize(value));

    [Fact]
    // [AC] REQ-ART-03: a URL of exactly MaxLength characters is kept.
    public void Normalize_UrlOfExactlyMaxLength_IsKept()
    {
        var url = "https://e.com/" + new string('a', ImageUrlRules.MaxLength - "https://e.com/".Length);
        Assert.Equal(500, url.Length);
        Assert.Equal(url, ImageUrlRules.Normalize(url));
    }

    [Fact]
    // [AC] REQ-ART-03: a URL longer than MaxLength characters is treated as no image.
    public void Normalize_UrlOverMaxLength_ReturnsNull()
    {
        var url = "https://e.com/" + new string('a', ImageUrlRules.MaxLength - "https://e.com/".Length + 1);
        Assert.Equal(501, url.Length);
        Assert.Null(ImageUrlRules.Normalize(url));
    }

    [Fact]
    // [AC] REQ-ART-03: MaxLength is 500 characters.
    public void MaxLength_Is500()
        => Assert.Equal(500, ImageUrlRules.MaxLength);
}
