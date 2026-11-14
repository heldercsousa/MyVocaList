using MyVocaList.Domain.Rules;

namespace MyVocaList.Tests.Unit.Domain;

public class ImageReferenceRulesVenueTests
{
    private const string VenueName = "0123456789abcdef0123456789abcdef";
    private const string VenueRef = "local:venues/" + VenueName;
    private const string SongRef = "local:songs/" + VenueName;
    private const string ArtistRef = "local:artists/" + VenueName;
    private static readonly string Root = Path.Combine(Path.GetTempPath(), "images-root");

    [Fact]
    // [AC] REQ-VEN-03: local:venues/<32 lowercase hex> is a well-formed local reference.
    public void IsLocal_VenueReference_ReturnsTrue()
        => Assert.True(ImageReferenceRules.IsLocal(VenueRef));

    [Fact]
    // [AC] REQ-VEN-03: a well-formed venue reference is kept unchanged on save.
    public void Normalize_VenueReference_IsKept()
        => Assert.Equal(VenueRef, ImageReferenceRules.Normalize(VenueRef));

    [Theory]
    [InlineData("local:venues/../../x")]
    [InlineData("local:venues/0123456789ABCDEF0123456789ABCDEF")]
    [InlineData("local:venues/0123456789abcdef0123456789abcde")]
    [InlineData("local:venues/0123456789abcdef0123456789abcdef0")]
    [InlineData("local:people/" + VenueName)]
    // [AC] REQ-VEN-03: a malformed venue reference (traversal, uppercase, wrong length, unknown folder) is stored as null.
    public void Normalize_MalformedVenueReference_ReturnsNull(string value)
        => Assert.Null(ImageReferenceRules.Normalize(value));

    [Fact]
    // [AC] REQ-VEN-03: a venue reference is built as local:venues/<id:N>.
    public void CreateLocal_Venue_UsesVenuesFolderAndLowercaseHex()
    {
        var id = Guid.NewGuid();
        var reference = ImageReferenceRules.CreateLocal(ImageOwnerKind.Venue, id);

        Assert.Equal("local:venues/" + id.ToString("N"), reference);
        Assert.True(ImageReferenceRules.IsLocal(reference));
    }

    [Fact]
    // [AC] REQ-VEN-03: a venue reference shows the thumbnail file in the venues folder.
    public void ToDisplaySource_VenueThumbnail_IsThumbnailFilePath()
        => Assert.Equal(
            Path.Combine(Root, "venues", VenueName + "_t.jpg"),
            ImageReferenceRules.ToDisplaySource(VenueRef, ImageSize.Thumbnail, Root));

    [Fact]
    // [AC] REQ-VEN-03: a venue reference shows the large file in the venues folder for the large size.
    public void ToDisplaySource_VenueLarge_IsLargeFilePath()
        => Assert.Equal(
            Path.Combine(Root, "venues", VenueName + "_l.jpg"),
            ImageReferenceRules.ToDisplaySource(VenueRef, ImageSize.Large, Root));

    [Fact]
    // [AC] REQ-VEN-03: both file paths of a venue reference are derived from the images root.
    public void TryGetLocalFiles_VenueReference_ReturnsBothPaths()
    {
        var ok = ImageReferenceRules.TryGetLocalFiles(VenueRef, Root, out var thumbnail, out var large);

        Assert.True(ok);
        Assert.Equal(Path.Combine(Root, "venues", VenueName + "_t.jpg"), thumbnail);
        Assert.Equal(Path.Combine(Root, "venues", VenueName + "_l.jpg"), large);
    }

    [Fact]
    // [AC] REQ-VEN-03: a song reference is still a well-formed local reference after the venues folder is added.
    public void IsLocal_SongReference_StillReturnsTrue()
        => Assert.True(ImageReferenceRules.IsLocal(SongRef));

    [Fact]
    // [AC] REQ-VEN-03: an artist reference is still a well-formed local reference after the venues folder is added.
    public void IsLocal_ArtistReference_StillReturnsTrue()
        => Assert.True(ImageReferenceRules.IsLocal(ArtistRef));

    [Fact]
    // [AC] REQ-VEN-03: the image store can read the list of valid folders (songs, artists, venues).
    public void Folders_ContainsSongsArtistsAndVenues()
        => Assert.Equal(new[] { "songs", "artists", "venues" }, ImageReferenceRules.Folders);
}
