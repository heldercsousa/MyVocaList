using MyVocaList.Domain.Resolution;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Unit.Services;

/// <summary>
/// Level A tests for the picture rules of <c>ISongService.SaveFromFormAsync</c> added by the form-pictures
/// item (REQ-PIC-02, 21, 22, 32, 40): local references, the edit-mode picture under <c>ImageChanged</c>,
/// the duplicate refusal of an artist change and the artist picture of an edit-mode Save.
/// Same Moq harness as <see cref="SongServiceSaveFromFormImageTests"/>; that file is not edited.
/// </summary>
public class SongServiceSaveFromFormPictureTests
{
    private const string LocalSongImage = "local:songs/0123456789abcdef0123456789abcdef";
    private const string OtherLocalSongImage = "local:songs/fedcba9876543210fedcba9876543210";
    private const string StoredSongImage = "https://old.example.com/cover.jpg";
    private const string ArtistImage = "https://cdn.example.com/artist.jpg";
    private const string OtherArtistImage = "https://cdn.example.com/other.jpg";
    private const string ArtistChangeDuplicateMessage = "A song with this title and version already exists for this artist";
    private const int EditedSongId = 40;
    private const int StoredArtistId = 5;
    private const int OtherArtistId = 6;
    private const int NewArtistId = 12;
    private const int CollidingSongId = 3;

    private readonly Mock<ISongRepository> _songRepoMock = new();
    private readonly Mock<IArtistRepository> _artistRepoMock = new();
    private readonly Mock<ISongKaraokeUrlRepository> _urlRepoMock = new();
    private readonly Mock<ISongKaraokeUrlService> _urlServiceMock = new();
    private readonly Mock<IArtistService> _artistServiceMock = new();
    private readonly Mock<IArtistResolutionService> _artistResolutionMock = new();
    private readonly Mock<ISongResolutionService> _songResolutionMock = new();
    private readonly Mock<ILogger<SongService>> _loggerMock = new();

    private readonly Dictionary<int, Artist> _storedArtists = [];
    private Song? AddedSong { get; set; }
    private Artist? CreatedArtist { get; set; }
    private Song StoredSong { get; }

    public SongServiceSaveFromFormPictureTests()
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

        StoredSong = new Song
        {
            Id = EditedSongId, Title = "Don't Cry", Version = string.Empty,
            ArtistId = StoredArtistId, ImageUrl = StoredSongImage
        };
        _songRepoMock
            .Setup(r => r.GetByIdAsync(EditedSongId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(StoredSong);
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
        ArtistDecision? artistDecision = null,
        string? imageUrl = null,
        string? artistImageUrl = null,
        bool imageChanged = false)
        => new(
            songId, "Don't Cry", string.Empty, FeaturedArtists: null, Lyrics: null,
            artistId, artistText, ExternalProvider: null, ExternalId: null,
            PendingUrls: [],
            artistDecision ?? new ArtistDecision.None(),
            SongDecision.None,
            imageUrl,
            artistImageUrl,
            imageChanged);

    private static SongFormSaveRequest EditRequest(
        int? artistId = StoredArtistId,
        string? artistText = null,
        ArtistDecision? artistDecision = null,
        string? imageUrl = null,
        string? artistImageUrl = null,
        bool imageChanged = false)
        => NewRequest(EditedSongId, artistId, artistText, artistDecision, imageUrl, artistImageUrl, imageChanged);

    private void SetupExactArtistMatch(int artistId)
        => _artistResolutionMock
            .Setup(r => r.ResolveAsync(It.IsAny<ArtistCandidate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ArtistResolution(ResolutionKind.ExactLocalMatch, artistId, []));

    private void SetupCollisionFor(int artistId)
        => _songRepoMock
            .Setup(r => r.FindIdByTitleVersionForArtistAsync(
                artistId, It.IsAny<string>(), It.IsAny<string>(), EditedSongId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((int?)CollidingSongId);

    private void VerifySongNeverUpdated()
        => _songRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Song>(), It.IsAny<CancellationToken>()), Times.Never);

    // ── REQ-PIC-02 / REQ-PIC-21: add mode ─────────────────────────────────────

    [Fact]
    public async Task SaveFromForm_AddWithLocalReference_StoresReferenceOnSong()
    {
        // [AC] REQ-PIC-21: in add mode the stored song's reference is the form picture's local reference
        var outcome = await CreateSut().SaveFromFormAsync(NewRequest(imageUrl: LocalSongImage));

        Assert.Equal(SongFormSaveKind.Saved, outcome.Kind);
        Assert.Equal(LocalSongImage, AddedSong!.ImageUrl);
    }

    [Theory]
    [InlineData("local:songs/../../x")]
    [InlineData("local:songs/0123456789ABCDEF0123456789ABCDEF")]
    [InlineData("local:videos/0123456789abcdef0123456789abcdef")]
    [InlineData("file:///data/x.jpg")]
    public async Task SaveFromForm_AddWithMalformedReference_StoresNull(string reference)
    {
        // [AC] REQ-PIC-02: a value that is neither an image URL nor a well-formed local reference is stored as null
        var outcome = await CreateSut().SaveFromFormAsync(NewRequest(imageUrl: reference));

        Assert.Equal(SongFormSaveKind.Saved, outcome.Kind);
        Assert.Null(AddedSong!.ImageUrl);
    }

    [Fact]
    public async Task SaveFromForm_AddWithLocalArtistReference_DoesNotStoreItOnTheArtist()
    {
        // [AC] REQ-PIC-02: the Song form's artist picture stays an image URL only (P19), so a local value is stored as null
        await CreateSut().SaveFromFormAsync(NewRequest(artistImageUrl: "local:artists/0123456789abcdef0123456789abcdef"));

        Assert.Null(CreatedArtist!.ImageUrl);
    }

    // ── REQ-PIC-21: edit mode ─────────────────────────────────────────────────

    [Fact]
    public async Task SaveFromForm_EditImageChanged_ReplacesStoredReference()
    {
        // [AC] REQ-PIC-21: in edit mode an uploaded picture replaces the stored reference
        var outcome = await CreateSut().SaveFromFormAsync(EditRequest(imageUrl: LocalSongImage, imageChanged: true));

        Assert.Equal(SongFormSaveKind.Saved, outcome.Kind);
        Assert.Equal(LocalSongImage, StoredSong.ImageUrl);
        _songRepoMock.Verify(r => r.UpdateAsync(
            It.Is<Song>(s => s.Id == EditedSongId && s.ImageUrl == LocalSongImage), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SaveFromForm_EditImageChangedToNull_RemovesStoredReference()
    {
        // [AC] REQ-PIC-21: in edit mode a removed picture leaves the stored reference null
        var outcome = await CreateSut().SaveFromFormAsync(EditRequest(imageUrl: null, imageChanged: true));

        Assert.Equal(SongFormSaveKind.Saved, outcome.Kind);
        Assert.Null(StoredSong.ImageUrl);
    }

    [Fact]
    public async Task SaveFromForm_EditImageChangedToMalformedReference_StoresNull()
    {
        // [AC] REQ-PIC-02: an edit-mode change to a malformed reference is stored as null
        await CreateSut().SaveFromFormAsync(EditRequest(imageUrl: "local:songs/../../x", imageChanged: true));

        Assert.Null(StoredSong.ImageUrl);
    }

    [Fact]
    public async Task SaveFromForm_EditImageNotChanged_LeavesStoredReferenceEvenWithAValue()
    {
        // [AC] REQ-PIC-21: in edit mode without an upload or a removal the stored reference is unchanged
        var outcome = await CreateSut().SaveFromFormAsync(EditRequest(imageUrl: OtherLocalSongImage, imageChanged: false));

        Assert.Equal(SongFormSaveKind.Saved, outcome.Kind);
        Assert.Equal(StoredSongImage, StoredSong.ImageUrl);
    }

    [Fact]
    public async Task UpdateSongAsync_PublicPath_NeverTouchesTheStoredReference()
    {
        // [AC] REQ-PIC-21: the public update path keeps the stored reference (only a form Save with ImageChanged changes it)
        var (success, _) = await CreateSut().UpdateSongAsync(EditedSongId, "Don't Cry", null, null);

        Assert.True(success);
        Assert.Equal(StoredSongImage, StoredSong.ImageUrl);
    }

    // ── REQ-PIC-22: a refused Save stores nothing ─────────────────────────────

    [Fact]
    public async Task SaveFromForm_EditRefused_KeepsStoredReference()
    {
        // [AC] REQ-PIC-22: a refused edit-mode Save changes no stored reference
        SetupCollisionFor(StoredArtistId);

        var outcome = await CreateSut().SaveFromFormAsync(EditRequest(imageUrl: LocalSongImage, imageChanged: true));

        Assert.Equal(SongFormSaveKind.Failed, outcome.Kind);
        Assert.Equal(StoredSongImage, StoredSong.ImageUrl);
        VerifySongNeverUpdated();
    }

    // ── REQ-PIC-32: no duplicate by changing the artist ──────────────────────

    [Fact]
    public async Task SaveFromForm_EditArtistChangedByIdIntoDuplicate_FailsOnArtistAndChangesNothing()
    {
        // [AC] REQ-PIC-32: moving the song by id onto an artist that already has the same title and version is refused
        SetupCollisionFor(OtherArtistId);

        var outcome = await CreateSut().SaveFromFormAsync(EditRequest(artistId: OtherArtistId));

        Assert.Equal(SongFormSaveKind.Failed, outcome.Kind);
        Assert.Equal(SaveField.Artist, outcome.FailedField);
        Assert.Equal(ArtistChangeDuplicateMessage, outcome.Message);
        Assert.Null(outcome.SavedSongId);
        Assert.Equal(StoredArtistId, StoredSong.ArtistId);
        VerifySongNeverUpdated();
    }

    [Fact]
    public async Task SaveFromForm_EditArtistChangedByTypedNameIntoDuplicate_FailsOnArtistAndChangesNothing()
    {
        // [AC] REQ-PIC-32: a typed artist resolved to an existing artist that already has the song is refused
        SetupExactArtistMatch(OtherArtistId);
        SetupCollisionFor(OtherArtistId);

        var outcome = await CreateSut().SaveFromFormAsync(EditRequest(artistId: null, artistText: "Stored artist 6"));

        Assert.Equal(SongFormSaveKind.Failed, outcome.Kind);
        Assert.Equal(SaveField.Artist, outcome.FailedField);
        Assert.Equal(ArtistChangeDuplicateMessage, outcome.Message);
        Assert.Equal(StoredArtistId, StoredSong.ArtistId);
        VerifySongNeverUpdated();
    }

    [Fact]
    public async Task SaveFromForm_EditArtistChangedWithoutDuplicate_IsSaved()
    {
        // [AC] REQ-PIC-32: an artist change that gives no duplicate is saved in place
        var outcome = await CreateSut().SaveFromFormAsync(EditRequest(artistId: OtherArtistId));

        Assert.Equal(SongFormSaveKind.Saved, outcome.Kind);
        _songRepoMock.Verify(r => r.UpdateAsync(
            It.Is<Song>(s => s.Id == EditedSongId && s.ArtistId == OtherArtistId), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── REQ-PIC-40 (REQ-ART-16 in edit mode): the artist picture ──────────────

    [Fact]
    public async Task SaveFromForm_EditCreatesArtistFromTypedName_NewArtistGetsArtistImage()
    {
        // [AC] REQ-PIC-40: an artist created by an edit-mode Save receives the pending artist picture (REQ-ART-16)
        var outcome = await CreateSut().SaveFromFormAsync(EditRequest(
            artistId: null, artistText: "Brand New Band", artistImageUrl: ArtistImage));

        Assert.Equal(SongFormSaveKind.Saved, outcome.Kind);
        Assert.Equal(ArtistImage, CreatedArtist!.ImageUrl);
        Assert.Equal(NewArtistId, StoredSong.ArtistId);
    }

    [Fact]
    public async Task SaveFromForm_EditChangesToExistingArtistWithoutImage_FillsItWithATrackedWrite()
    {
        // [AC] REQ-PIC-40: an existing artist with no picture reached by an edit-mode artist change is filled (REQ-ART-16)
        var artist = StoredArtist(OtherArtistId);

        var outcome = await CreateSut().SaveFromFormAsync(EditRequest(artistId: OtherArtistId, artistImageUrl: ArtistImage));

        Assert.Equal(SongFormSaveKind.Saved, outcome.Kind);
        Assert.Equal(ArtistImage, artist.ImageUrl);
        _artistRepoMock.Verify(r => r.UpdateAsync(
            It.Is<Artist>(a => a.Id == OtherArtistId && a.ImageUrl == ArtistImage), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SaveFromForm_EditChangesToExistingArtistByExactName_FillsIt()
    {
        // [AC] REQ-PIC-40: an existing artist matched by its typed name in edit mode with no picture is filled (REQ-ART-16)
        var artist = StoredArtist(OtherArtistId);
        SetupExactArtistMatch(OtherArtistId);

        await CreateSut().SaveFromFormAsync(EditRequest(artistId: null, artistText: "Stored artist 6", artistImageUrl: ArtistImage));

        Assert.Equal(ArtistImage, artist.ImageUrl);
    }

    [Fact]
    public async Task SaveFromForm_EditChangesToExistingArtistWithImage_NeverOverwrites()
    {
        // [AC] REQ-PIC-40: an existing artist that already has a picture keeps it in edit mode (REQ-ART-16)
        var artist = StoredArtist(OtherArtistId, OtherArtistImage);

        await CreateSut().SaveFromFormAsync(EditRequest(artistId: OtherArtistId, artistImageUrl: ArtistImage));

        Assert.Equal(OtherArtistImage, artist.ImageUrl);
        _artistRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Artist>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SaveFromForm_EditArtistChosenByUseExisting_IsNeverFilled()
    {
        // [AC] REQ-PIC-40: the artist chosen in the near-duplicate sheet is never filled in edit mode (REQ-ART-16)
        var artist = StoredArtist(OtherArtistId);

        var outcome = await CreateSut().SaveFromFormAsync(EditRequest(
            artistId: null, artistText: "Stored artst 6",
            artistDecision: new ArtistDecision.UseExisting(OtherArtistId), artistImageUrl: ArtistImage));

        Assert.Equal(SongFormSaveKind.Saved, outcome.Kind);
        Assert.Null(artist.ImageUrl);
    }

    [Fact]
    public async Task SaveFromForm_EditRefusedAfterArtistChange_DoesNotFillTheArtist()
    {
        // [AC] REQ-PIC-22: a refused edit-mode Save leaves the artist picture unchanged too
        var artist = StoredArtist(OtherArtistId);
        SetupCollisionFor(OtherArtistId);

        await CreateSut().SaveFromFormAsync(EditRequest(artistId: OtherArtistId, artistImageUrl: ArtistImage));

        Assert.Null(artist.ImageUrl);
        _artistRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Artist>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
