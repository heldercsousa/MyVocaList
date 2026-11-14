using MyVocaList.Domain.Rules;

namespace MyVocaList.Tests.Unit.Domain;

public class ImageReferenceRulesTests
{
    private const string SongName = "0123456789abcdef0123456789abcdef";
    private const string SongRef = "local:songs/" + SongName;
    private const string ArtistRef = "local:artists/" + SongName;
    private const string DeezerUrl = "https://cdn-images.dzcdn.net/images/cover/abc123/250x250-000000-80-0-0.jpg";
    private static readonly string Root = Path.Combine(Path.GetTempPath(), "images-root");

    [Theory]
    [InlineData(SongRef)]
    [InlineData(ArtistRef)]
    // [AC] REQ-PIC-01: a well-formed local reference is recognised.
    public void IsLocal_WellFormed_ReturnsTrue(string reference)
        => Assert.True(ImageReferenceRules.IsLocal(reference));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("https://e.com/a.jpg")]
    [InlineData("local:")]
    [InlineData("local:songs")]
    [InlineData("local:songs/")]
    [InlineData("local:songs/../../x")]
    [InlineData("local:songs/..\\..\\x")]
    [InlineData("local:../songs/0123456789abcdef0123456789abcdef")]
    [InlineData("local:/songs/0123456789abcdef0123456789abcdef")]
    [InlineData("local:songs/0123456789abcdef0123456789abcde")]
    [InlineData("local:songs/0123456789abcdef0123456789abcdef0")]
    [InlineData("local:songs/0123456789ABCDEF0123456789ABCDEF")]
    [InlineData("local:songs/0123456789abcdef0123456789abcdeg")]
    [InlineData("local:covers/0123456789abcdef0123456789abcdef")]
    [InlineData("local:songs/sub/0123456789abcdef0123456789abcdef")]
    [InlineData("LOCAL:songs/0123456789abcdef0123456789abcdef")]
    [InlineData("local:Songs/0123456789abcdef0123456789abcdef")]
    // [AC] REQ-PIC-02: anything not exactly local:<folder>/<32 lowercase hex> is not a local reference.
    public void IsLocal_Malformed_ReturnsFalse(string? reference)
        => Assert.False(ImageReferenceRules.IsLocal(reference));

    [Theory]
    [InlineData(SongRef)]
    [InlineData(ArtistRef)]
    // [AC] REQ-PIC-02: a well-formed local reference is kept unchanged.
    public void Normalize_LocalReference_IsKept(string reference)
        => Assert.Equal(reference, ImageReferenceRules.Normalize(reference));

    [Fact]
    // [AC] REQ-PIC-02: an https image URL still goes through the URL rules (trimmed).
    public void Normalize_HttpsUrl_DelegatesToImageUrlRules()
        => Assert.Equal(DeezerUrl, ImageReferenceRules.Normalize("  " + DeezerUrl + " "));

    [Theory]
    [InlineData("local:songs/../../x")]
    [InlineData("local:songs/short")]
    [InlineData("http://e.com/a.jpg")]
    [InlineData("")]
    [InlineData(null)]
    // [AC] REQ-PIC-02: a value that is neither an image URL nor a well-formed local reference is stored as null.
    public void Normalize_Invalid_ReturnsNull(string? value)
        => Assert.Null(ImageReferenceRules.Normalize(value));

    [Fact]
    // [AC] REQ-PIC-09: a song reference is local:songs/<id:N>.
    public void CreateLocal_Song_UsesSongsFolderAndLowercaseHex()
    {
        var id = Guid.NewGuid();
        var reference = ImageReferenceRules.CreateLocal(ImageOwnerKind.Song, id);

        Assert.Equal("local:songs/" + id.ToString("N"), reference);
        Assert.True(ImageReferenceRules.IsLocal(reference));
    }

    [Fact]
    // [AC] REQ-PIC-09: an artist reference is local:artists/<id:N>.
    public void CreateLocal_Artist_UsesArtistsFolder()
    {
        var id = Guid.NewGuid();
        var reference = ImageReferenceRules.CreateLocal(ImageOwnerKind.Artist, id);

        Assert.Equal("local:artists/" + id.ToString("N"), reference);
        Assert.True(ImageReferenceRules.IsLocal(reference));
    }

    [Fact]
    // [AC] REQ-PIC-03: a local reference shows the thumbnail file.
    public void ToDisplaySource_LocalThumbnail_IsThumbnailFilePath()
        => Assert.Equal(
            Path.Combine(Root, "songs", SongName + "_t.jpg"),
            ImageReferenceRules.ToDisplaySource(SongRef, ImageSize.Thumbnail, Root));

    [Fact]
    // [AC] REQ-PIC-03: a local reference shows the large file for the large size.
    public void ToDisplaySource_LocalLarge_IsLargeFilePath()
        => Assert.Equal(
            Path.Combine(Root, "artists", SongName + "_l.jpg"),
            ImageReferenceRules.ToDisplaySource(ArtistRef, ImageSize.Large, Root));

    [Fact]
    // [AC] REQ-PIC-03: a Deezer URL is rewritten to /500x500- for the large size.
    public void ToDisplaySource_DeezerLarge_RewritesSizeSegment()
        => Assert.Equal(
            "https://cdn-images.dzcdn.net/images/cover/abc123/500x500-000000-80-0-0.jpg",
            ImageReferenceRules.ToDisplaySource(DeezerUrl, ImageSize.Large, Root));

    [Fact]
    // [AC] REQ-PIC-03: a Deezer URL is unchanged for the thumbnail.
    public void ToDisplaySource_DeezerThumbnail_IsUnchanged()
        => Assert.Equal(DeezerUrl, ImageReferenceRules.ToDisplaySource(DeezerUrl, ImageSize.Thumbnail, Root));

    [Theory]
    [InlineData(ImageSize.Thumbnail)]
    [InlineData(ImageSize.Large)]
    // [AC] REQ-PIC-03: any other image URL is unchanged for both sizes.
    public void ToDisplaySource_OtherUrl_IsUnchanged(ImageSize size)
        => Assert.Equal("https://e.com/pic.jpg", ImageReferenceRules.ToDisplaySource("https://e.com/pic.jpg", size, Root));

    [Fact]
    // [AC] REQ-PIC-03: only the first /250x250- segment is replaced.
    public void ToDisplaySource_DeezerLarge_ReplacesOnlyFirstOccurrence()
        => Assert.Equal(
            "https://e.com/500x500-a/250x250-b.jpg",
            ImageReferenceRules.ToDisplaySource("https://e.com/250x250-a/250x250-b.jpg", ImageSize.Large, Root));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("local:songs/../../x")]
    [InlineData("local:songs/short")]
    [InlineData("http://e.com/a.jpg")]
    // [AC] REQ-PIC-03: null or invalid gives null.
    public void ToDisplaySource_NullOrInvalid_ReturnsNull(string? reference)
        => Assert.Null(ImageReferenceRules.ToDisplaySource(reference, ImageSize.Large, Root));

    [Fact]
    // [AC] REQ-PIC-03: both file paths of a local reference are derived from the images root.
    public void TryGetLocalFiles_WellFormed_ReturnsBothPaths()
    {
        var ok = ImageReferenceRules.TryGetLocalFiles(SongRef, Root, out var thumbnail, out var large);

        Assert.True(ok);
        Assert.Equal(Path.Combine(Root, "songs", SongName + "_t.jpg"), thumbnail);
        Assert.Equal(Path.Combine(Root, "songs", SongName + "_l.jpg"), large);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("https://e.com/a.jpg")]
    [InlineData("local:songs/../../x")]
    [InlineData("local:covers/0123456789abcdef0123456789abcdef")]
    // [AC] REQ-PIC-02: no path is produced for anything but a well-formed local reference (no path traversal).
    public void TryGetLocalFiles_NotLocal_ReturnsFalseAndEmptyPaths(string? reference)
    {
        var ok = ImageReferenceRules.TryGetLocalFiles(reference, Root, out var thumbnail, out var large);

        Assert.False(ok);
        Assert.Equal(string.Empty, thumbnail);
        Assert.Equal(string.Empty, large);
    }

    [Fact]
    // [AC] REQ-PIC-07: target sizes are 256 and 512 px.
    public void Pixels_AreThumbnail256AndLarge512()
    {
        Assert.Equal(256, ImageReferenceRules.ThumbnailPixels);
        Assert.Equal(512, ImageReferenceRules.LargePixels);
    }
}
