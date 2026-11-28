using MyVocaList.Contracts.DTOs;
using MyVocaList.Domain.Constants;
using MyVocaList.Domain.Resolution;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Unit.Services;

/// <summary>
/// Level A tests for REQ-MSRC-13: an artist created by a Save from a MusicBrainz pick carries its MusicBrainz id,
/// and its profile photo is fetched at that moment (REQ-MSRC-15 adoption).
/// </summary>
public class SongServiceSaveFromFormArtistProfileTests
{
    private const int NewArtistId = 12;
    private const string ArtistMbid = "0383dadf-2a4e-4d10-a46a-e9e041da8eb3";

    private readonly Mock<ISongRepository> _songRepoMock = new();
    private readonly Mock<ICatalogRepository> _catalogRepoMock = new();
    private readonly Mock<IArtistRepository> _artistRepoMock = new();
    private readonly Mock<ISongKaraokeUrlRepository> _urlRepoMock = new();
    private readonly Mock<ISongKaraokeUrlService> _urlServiceMock = new();
    private readonly Mock<IArtistService> _artistServiceMock = new();
    private readonly Mock<IArtistResolutionService> _artistResolutionMock = new();
    private readonly Mock<ISongResolutionService> _songResolutionMock = new();
    private readonly Mock<IArtistProfileService> _profileMock = new();
    private readonly Mock<ILogger<SongService>> _loggerMock = new();

    private string? CreatedExternalId { get; set; }
    private string? CreatedExternalProvider { get; set; }
    private ArtistCandidate? ResolvedCandidate { get; set; }
    private static readonly ArtistProfileDto Profile = new("https://commons.example/queen.jpg", "credit", "https://license", null, []);

    public SongServiceSaveFromFormArtistProfileTests()
    {
        _artistServiceMock.Setup(a => a.ValidateNameInput(It.IsAny<string>())).Returns((true, string.Empty));
        _artistServiceMock
            .Setup(a => a.CreateArtistAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string name, string? externalId, string? provider, CancellationToken _) =>
            {
                CreatedExternalId = externalId;
                CreatedExternalProvider = provider;
                return (true, string.Empty, (Artist?)new Artist { Id = NewArtistId, Name = name, ExternalId = externalId, ExternalProvider = provider });
            });

        _artistResolutionMock
            .Setup(r => r.ResolveAsync(It.IsAny<ArtistCandidate>(), It.IsAny<CancellationToken>()))
            .Callback<ArtistCandidate, CancellationToken>((c, _) => ResolvedCandidate = c)
            .ReturnsAsync(new ArtistResolution(ResolutionKind.NoMatch, null, []));
        _songResolutionMock
            .Setup(r => r.ResolveAsync(It.IsAny<SongCandidate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SongResolution(ResolutionKind.NoMatch, null, []));

        _songRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Song>(), It.IsAny<CancellationToken>()))
            .Callback<Song, CancellationToken>((song, _) => song.Id = 99)
            .Returns(Task.CompletedTask);

        _profileMock.Setup(p => p.GetAsync(ArtistMbid, It.IsAny<CancellationToken>())).ReturnsAsync(Profile);
    }

    private SongService CreateSut() => new(
        _songRepoMock.Object, _artistRepoMock.Object, _urlRepoMock.Object, _urlServiceMock.Object,
        PassthroughUnitOfWork.Over(
            _songRepoMock, _artistRepoMock, _urlRepoMock, _urlServiceMock, _catalogRepoMock.Object, _artistServiceMock,
            _artistResolutionMock.Object, _songResolutionMock, _profileMock),
        _loggerMock.Object);

    private static SongFormSaveRequest PickRequest(string? artistExternalId = ArtistMbid)
        => new(
            SongId: null, "Bohemian Rhapsody", Lyrics: null,
            ArtistId: null, ArtistText: "Queen", ExternalProvider: ProviderNames.MusicBrainz, ExternalId: "rec-1",
            PendingUrls: [], new ArtistDecision.None(), SongDecision.None,
            ArtistExternalId: artistExternalId);

    // [AC] REQ-MSRC-13: the artist created from a MusicBrainz pick is stored with its MusicBrainz id
    [Fact]
    public async Task SaveFromForm_NewArtistFromPick_CreatesArtistWithMusicBrainzId()
    {
        var outcome = await CreateSut().SaveFromFormAsync(PickRequest());

        Assert.Equal(SongFormSaveKind.Saved, outcome.Kind);
        Assert.Equal(ArtistMbid, CreatedExternalId);
        Assert.Equal(ProviderNames.MusicBrainz, CreatedExternalProvider);
    }

    // [AC] REQ-MSRC-13: the artist is resolved by its MusicBrainz id first, so a known artist is reused, not duplicated
    [Fact]
    public async Task SaveFromForm_NewArtistFromPick_ResolvesByMusicBrainzId()
    {
        await CreateSut().SaveFromFormAsync(PickRequest());

        Assert.Equal(ArtistMbid, ResolvedCandidate!.ExternalId);
        Assert.Equal(ProviderNames.MusicBrainz, ResolvedCandidate.ExternalProvider);
    }

    // [AC] REQ-MSRC-13: the profile data is fetched at that moment and the photo adopted for the new artist
    [Fact]
    public async Task SaveFromForm_NewArtistFromPick_FetchesProfileAndAdoptsPhoto()
    {
        await CreateSut().SaveFromFormAsync(PickRequest());

        _profileMock.Verify(p => p.GetAsync(ArtistMbid, It.IsAny<CancellationToken>()), Times.Once);
        _profileMock.Verify(p => p.AdoptPhotoIfMissingAsync(NewArtistId, Profile, It.IsAny<CancellationToken>()), Times.Once);
    }

    // [AC] REQ-MSRC-13: an artist that already exists locally is not created, so nothing is fetched or adopted
    [Fact]
    public async Task SaveFromForm_ExistingArtistMatched_NoProfileFetch()
    {
        _artistResolutionMock
            .Setup(r => r.ResolveAsync(It.IsAny<ArtistCandidate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ArtistResolution(ResolutionKind.ExactExternalMatch, 5, []));
        _artistRepoMock.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Artist { Id = 5, Name = "Queen" });

        var outcome = await CreateSut().SaveFromFormAsync(PickRequest());

        Assert.Equal(SongFormSaveKind.Saved, outcome.Kind);
        _profileMock.Verify(p => p.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // [AC] REQ-MSRC-13: no MusicBrainz id (typed artist) -> created plain, no fetch
    [Fact]
    public async Task SaveFromForm_NoArtistMusicBrainzId_CreatesPlainArtistWithoutFetch()
    {
        await CreateSut().SaveFromFormAsync(PickRequest(artistExternalId: null));

        Assert.Null(CreatedExternalId);
        _profileMock.Verify(p => p.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // [AC] REQ-MSRC-13: a failed profile lookup never fails the save
    [Fact]
    public async Task SaveFromForm_ProfileLookupFails_StillSaved()
    {
        _profileMock.Setup(p => p.GetAsync(ArtistMbid, It.IsAny<CancellationToken>())).ReturnsAsync((ArtistProfileDto?)null);

        var outcome = await CreateSut().SaveFromFormAsync(PickRequest());

        Assert.Equal(SongFormSaveKind.Saved, outcome.Kind);
        _profileMock.Verify(p => p.AdoptPhotoIfMissingAsync(It.IsAny<int>(), It.IsAny<ArtistProfileDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // [AC] REQ-MSRC-13: an exception from the profile service never fails the save
    [Fact]
    public async Task SaveFromForm_ProfileServiceThrows_StillSaved()
    {
        _profileMock.Setup(p => p.GetAsync(ArtistMbid, It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("boom"));

        var outcome = await CreateSut().SaveFromFormAsync(PickRequest());

        Assert.Equal(SongFormSaveKind.Saved, outcome.Kind);
    }
}
