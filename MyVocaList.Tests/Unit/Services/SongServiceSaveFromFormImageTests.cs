using MyVocaList.Domain.Resolution;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Unit.Services;

/// <summary>
/// Level A tests for the image rules of <c>ISongService.SaveFromFormAsync</c> (REQ-ART-03, 15–18):
/// the song cover is stored on add only, an artist picture only fills an empty slot.
/// </summary>
public class SongServiceSaveFromFormImageTests
{
    private const string SongImage = "https://cdn.example.com/cover.jpg";
    private const string ArtistImage = "https://cdn.example.com/artist.jpg";
    private const string OtherArtistImage = "https://cdn.example.com/other.jpg";
    private const int NewArtistId = 12;

    private readonly Mock<ISongRepository> _songRepoMock = new();
    private readonly Mock<IArtistRepository> _artistRepoMock = new();
    private readonly Mock<ISongKaraokeUrlRepository> _urlRepoMock = new();
    private readonly Mock<ISongKaraokeUrlService> _urlServiceMock = new();
    private readonly Mock<IArtistService> _artistServiceMock = new();
    private readonly Mock<IArtistResolutionService> _artistResolutionMock = new();
    private readonly Mock<ISongResolutionService> _songResolutionMock = new();
    private readonly Mock<ILogger<SongService>> _loggerMock = new();

    private Song? AddedSong { get; set; }
    private Artist? CreatedArtist { get; set; }
    private readonly Dictionary<int, Artist> _storedArtists = [];

    public SongServiceSaveFromFormImageTests()
    {
        _artistServiceMock
            .Setup(a => a.ValidateNameInput(It.IsAny<string>()))
            .Returns((true, string.Empty));
        _artistServiceMock
            .Setup(a => a.CreateArtistAsync(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string name, string? _, string? __, CancellationToken ___) =>
            {
                CreatedArtist = new Artist { Id = NewArtistId, Name = name };
                return (true, string.Empty, (Artist?)CreatedArtist);
            });

        _artistResolutionMock
            .Setup(r => r.ResolveAsync(It.IsAny<ArtistCandidate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ArtistResolution(ResolutionKind.NoMatch, null, []));
        _songResolutionMock
            .Setup(r => r.ResolveAsync(It.IsAny<SongCandidate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SongResolution(ResolutionKind.NoMatch, null, []));

        _artistRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => StoredArtist(id));

        _songRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Song>(), It.IsAny<CancellationToken>()))
            .Callback<Song, CancellationToken>((song, _) =>
            {
                song.Id = 99;
                AddedSong = song;
            })
            .Returns(Task.CompletedTask);
    }

    private Artist StoredArtist(int id, string? imageUrl = null)
    {
        if (!_storedArtists.TryGetValue(id, out var artist))
            _storedArtists[id] = artist = new Artist { Id = id, Name = $"Stored artist {id}", ImageUrl = imageUrl };
        return artist;
    }

    private SongService CreateSut() => new(
        _songRepoMock.Object,
        _artistRepoMock.Object,
        _urlRepoMock.Object,
        _urlServiceMock.Object,
        PassthroughUnitOfWork.Over(
            _songRepoMock, _artistRepoMock, _urlRepoMock, _urlServiceMock, _artistServiceMock,
            _artistResolutionMock.Object, _songResolutionMock),
        _loggerMock.Object);

    private static SongFormSaveRequest NewRequest(
        int? songId = null,
        int? artistId = null,
        string? artistText = "Guns N' Roses",
        string? provider = null,
        string? externalId = null,
        ArtistDecision? artistDecision = null,
        SongDecision songDecision = SongDecision.None,
        string? imageUrl = null,
        string? artistImageUrl = null)
        => new(
            songId, "Don't Cry", string.Empty, FeaturedArtists: null, Lyrics: null,
            artistId, artistText, provider, externalId,
            PendingUrls: [],
            artistDecision ?? new ArtistDecision.None(),
            songDecision,
            imageUrl,
            artistImageUrl);

    private void SetupExactArtistMatch(int artistId)
        => _artistResolutionMock
            .Setup(r => r.ResolveAsync(It.IsAny<ArtistCandidate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ArtistResolution(ResolutionKind.ExactLocalMatch, artistId, []));

    // ── REQ-ART-15: the song image on add ───────────────────────────────────

    [Fact]
    public async Task SaveFromForm_AddWithPendingImage_StoresImageOnSong()
    {
        // [AC] REQ-ART-15: a Save in add mode stores the pending cover on the new song
        var outcome = await CreateSut().SaveFromFormAsync(NewRequest(imageUrl: SongImage));

        Assert.Equal(SongFormSaveKind.Saved, outcome.Kind);
        Assert.Equal(SongImage, AddedSong!.ImageUrl);
    }

    [Fact]
    public async Task SaveFromForm_AddWithoutPendingImage_StoresNull()
    {
        // [AC] REQ-ART-15: no pending cover means the song is stored with a null image
        await CreateSut().SaveFromFormAsync(NewRequest());

        Assert.Null(AddedSong!.ImageUrl);
    }

    [Theory]
    [InlineData(SongDecision.SaveAsNewVersion)]
    [InlineData(SongDecision.SaveAsNewSong)]
    public async Task SaveFromForm_AddWithExplicitSaveAsNew_NullsIdentityButKeepsImage(SongDecision decision)
    {
        // [AC] REQ-ART-15: the cover is kept when the external identity is nulled for save-as-new
        var request = NewRequest(
            provider: "Deezer", externalId: "3135556", songDecision: decision, imageUrl: SongImage)
            with { Version = "Live" };

        var outcome = await CreateSut().SaveFromFormAsync(request);

        Assert.Equal(SongFormSaveKind.Saved, outcome.Kind);
        Assert.Null(AddedSong!.ExternalId);
        Assert.Null(AddedSong.ExternalProvider);
        Assert.Equal(SongImage, AddedSong.ImageUrl);
    }

    // ── REQ-ART-16: the artist image ─────────────────────────────────────────

    [Fact]
    public async Task SaveFromForm_ArtistCreatedByPlainPath_GetsArtistImage()
    {
        // [AC] REQ-ART-16: an artist created by the Save receives the pending artist picture
        var outcome = await CreateSut().SaveFromFormAsync(NewRequest(artistImageUrl: ArtistImage));

        Assert.Equal(SongFormSaveKind.Saved, outcome.Kind);
        Assert.Equal(ArtistImage, CreatedArtist!.ImageUrl);
    }

    [Fact]
    public async Task SaveFromForm_ArtistCreatedByCreateTypedDecision_GetsArtistImage()
    {
        // [AC] REQ-ART-16: the CreateTyped decision creates the artist with the pending picture
        var outcome = await CreateSut().SaveFromFormAsync(NewRequest(
            artistDecision: new ArtistDecision.CreateTyped(), artistImageUrl: ArtistImage));

        Assert.Equal(SongFormSaveKind.Saved, outcome.Kind);
        Assert.Equal(ArtistImage, CreatedArtist!.ImageUrl);
    }

    [Fact]
    public async Task SaveFromForm_ExistingArtistByIdWithoutImage_IsFilled()
    {
        // [AC] REQ-ART-16: an existing artist reached by id with a null image is filled
        var artist = StoredArtist(5);

        await CreateSut().SaveFromFormAsync(NewRequest(artistId: 5, artistText: null, artistImageUrl: ArtistImage));

        Assert.Equal(ArtistImage, artist.ImageUrl);
    }

    [Fact]
    public async Task SaveFromForm_ExistingArtistByExactNameWithoutImage_IsFilled()
    {
        // [AC] REQ-ART-16: an existing artist matched by exact collated name with a null image is filled
        var artist = StoredArtist(6);
        SetupExactArtistMatch(6);

        await CreateSut().SaveFromFormAsync(NewRequest(artistImageUrl: ArtistImage));

        Assert.Equal(ArtistImage, artist.ImageUrl);
    }

    [Fact]
    public async Task SaveFromForm_ExistingArtistWithImage_IsNeverChanged()
    {
        // [AC] REQ-ART-16: an existing artist that already has an image keeps it
        var byId = StoredArtist(5, OtherArtistImage);
        var byName = StoredArtist(6, OtherArtistImage);
        SetupExactArtistMatch(6);

        await CreateSut().SaveFromFormAsync(NewRequest(artistId: 5, artistText: null, artistImageUrl: ArtistImage));
        await CreateSut().SaveFromFormAsync(NewRequest(artistImageUrl: ArtistImage));

        Assert.Equal(OtherArtistImage, byId.ImageUrl);
        Assert.Equal(OtherArtistImage, byName.ImageUrl);
    }

    [Fact]
    public async Task SaveFromForm_ArtistChosenByUseExistingDecision_IsNeverChanged()
    {
        // [AC] REQ-ART-16: the artist chosen in the near-duplicate sheet is left untouched
        var artist = StoredArtist(7);

        var outcome = await CreateSut().SaveFromFormAsync(NewRequest(
            artistDecision: new ArtistDecision.UseExisting(7), artistImageUrl: ArtistImage));

        Assert.Equal(SongFormSaveKind.Saved, outcome.Kind);
        Assert.Null(artist.ImageUrl);
    }

    // ── REQ-ART-17: edit mode ─────────────────────────────────────────────────

    [Fact]
    public async Task SaveFromForm_EditModeWithImages_LeavesStoredImagesUnchanged()
    {
        // [AC] REQ-ART-17: edit mode never assigns Song.ImageUrl and the artist rule does not run
        var stored = new Song
        {
            Id = 40, Title = "Don't Cry", Version = string.Empty, ArtistId = 5, ImageUrl = "https://old.example.com/a.jpg"
        };
        var artist = StoredArtist(5);
        _songRepoMock
            .Setup(r => r.GetByIdAsync(40, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stored);

        await CreateSut().SaveFromFormAsync(NewRequest(
            songId: 40, artistId: 5, artistText: null, imageUrl: SongImage, artistImageUrl: ArtistImage));

        Assert.Equal("https://old.example.com/a.jpg", stored.ImageUrl);
        Assert.Null(artist.ImageUrl);
    }

    // ── REQ-ART-18: refused Save ──────────────────────────────────────────────

    [Fact]
    public async Task SaveFromForm_NeedsDecision_ChangesNoImage()
    {
        // [AC] REQ-ART-18: a Save that returns a needs-decision outcome changes no ImageUrl
        var artist = StoredArtist(5);
        _songResolutionMock
            .Setup(r => r.ResolveAsync(It.IsAny<SongCandidate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SongResolution(ResolutionKind.ExactLocalMatch, 77, []));
        _songRepoMock
            .Setup(r => r.GetByIdAsync(77, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Song { Id = 77, Title = "Don't Cry", ArtistId = 5 });

        var outcome = await CreateSut().SaveFromFormAsync(NewRequest(
            artistId: 5, artistText: null, imageUrl: SongImage, artistImageUrl: ArtistImage));

        Assert.Equal(SongFormSaveKind.SongChoiceNeeded, outcome.Kind);
        Assert.Null(AddedSong);
        Assert.Null(artist.ImageUrl);
    }

    [Fact]
    public async Task SaveFromForm_FailedWrite_ChangesNoImage()
    {
        // [AC] REQ-ART-18: a refused write leaves the existing artist's image and the song unwritten
        var artist = StoredArtist(5);
        _songRepoMock
            .Setup(r => r.ExistsByTitleVersionForArtistAsync(5, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var outcome = await CreateSut().SaveFromFormAsync(NewRequest(
            artistId: 5, artistText: null, imageUrl: SongImage, artistImageUrl: ArtistImage));

        Assert.Equal(SongFormSaveKind.Failed, outcome.Kind);
        Assert.Null(AddedSong);
        Assert.Null(artist.ImageUrl);
    }

    // ── REQ-ART-03: normalisation ─────────────────────────────────────────────

    [Fact]
    public async Task SaveFromForm_InvalidUrls_AreStoredAsNull()
    {
        // [AC] REQ-ART-03: a non-https image URL in the request is stored as null (song and artist)
        await CreateSut().SaveFromFormAsync(NewRequest(
            imageUrl: "http://cdn.example.com/cover.jpg", artistImageUrl: "not a url"));

        Assert.Null(AddedSong!.ImageUrl);
        Assert.Null(CreatedArtist!.ImageUrl);
    }
}
