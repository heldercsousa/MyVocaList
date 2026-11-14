using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Unit.Services;

/// <summary>
/// Level A tests for the two picture members of <see cref="ArtistService"/> (REQ-PIC-36):
/// <c>GetArtistByIdAsync</c> and <c>SetArtistImageAsync</c>. Same harness as <see cref="ArtistServiceTests"/>;
/// the persistence half (tracked write) is proven on real SQLite in
/// <c>ArtistServicePictureRealStackTests</c>.
/// </summary>
public class ArtistServicePictureTests
{
    private const string LocalArtistImage = "local:artists/0123456789abcdef0123456789abcdef";
    private const string DeezerArtistImage = "https://cdn-images.dzcdn.net/images/artist/def/250x250-000000-80-0-0.jpg";
    private const string StoredArtistImage = "https://old.example.com/artist.jpg";
    private const int ArtistId = 4;

    private readonly Mock<IArtistRepository> _artistRepoMock = new();
    private readonly Mock<ISongRepository> _songRepoMock = new();
    private readonly Mock<ICatalogRepository> _catalogRepoMock = new();
    private readonly Mock<ILogger<ArtistService>> _loggerMock = new();
    private readonly Artist _stored = new()
    {
        Id = ArtistId, Name = "Queen", ImageUrl = StoredArtistImage, UpdatedAt = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc)
    };

    public ArtistServicePictureTests()
    {
        _artistRepoMock
            .Setup(r => r.GetByIdAsync(ArtistId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_stored);
    }

    private ArtistService CreateSut() => new(
        _artistRepoMock.Object, _songRepoMock.Object, _catalogRepoMock.Object,
        PassthroughUnitOfWork.Over(_artistRepoMock, _songRepoMock, _catalogRepoMock),
        _loggerMock.Object);

    // ── GetArtistByIdAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetArtistByIdAsync_ExistingId_ReturnsTheArtistWithItsPicture()
    {
        // [AC] REQ-PIC-36: edit mode reads the stored artist (and its picture) through the service
        var artist = await CreateSut().GetArtistByIdAsync(ArtistId);

        Assert.NotNull(artist);
        Assert.Equal(StoredArtistImage, artist.ImageUrl);
    }

    [Fact]
    public async Task GetArtistByIdAsync_UnknownId_ReturnsNull()
    {
        // [AC] REQ-PIC-36: an unknown artist id reads as null
        Assert.Null(await CreateSut().GetArtistByIdAsync(999));
    }

    // ── SetArtistImageAsync ───────────────────────────────────────────────

    [Theory]
    [InlineData(LocalArtistImage)]
    [InlineData(DeezerArtistImage)]
    public async Task SetArtistImageAsync_ValidReference_IsWrittenWithUpdatedAt(string reference)
    {
        // [AC] REQ-PIC-36: Save stores the artist's form picture reference (local reference or image URL)
        var (success, message) = await CreateSut().SetArtistImageAsync(ArtistId, reference);

        Assert.True(success, message);
        Assert.Equal(reference, _stored.ImageUrl);
        Assert.True(_stored.UpdatedAt > new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        _artistRepoMock.Verify(r => r.UpdateAsync(
            It.Is<Artist>(a => a.Id == ArtistId && a.ImageUrl == reference), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("local:artists/../../x")]
    [InlineData("local:artists/0123")]
    [InlineData("http://insecure.example.com/a.jpg")]
    public async Task SetArtistImageAsync_MalformedReference_StoresNull(string reference)
    {
        // [AC] REQ-PIC-02: a value that is neither an image URL nor a well-formed local reference is stored as null
        var (success, _) = await CreateSut().SetArtistImageAsync(ArtistId, reference);

        Assert.True(success);
        Assert.Null(_stored.ImageUrl);
    }

    [Fact]
    public async Task SetArtistImageAsync_Null_RemovesThePicture()
    {
        // [AC] REQ-PIC-36: a removed picture (edit mode) leaves the artist with no reference
        var (success, _) = await CreateSut().SetArtistImageAsync(ArtistId, null);

        Assert.True(success);
        Assert.Null(_stored.ImageUrl);
    }

    [Fact]
    public async Task SetArtistImageAsync_UnknownId_FailsAndWritesNothing()
    {
        // [AC] REQ-PIC-36: a failed Save stores nothing
        var (success, message) = await CreateSut().SetArtistImageAsync(999, LocalArtistImage);

        Assert.False(success);
        Assert.Equal("Artist not found", message);
        _artistRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Artist>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
