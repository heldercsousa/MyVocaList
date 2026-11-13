using CommunityToolkit.Mvvm.Messaging;
using MyVocaList.Contracts.DTOs.Suggestions;
using MyVocaList.Domain.ReadModels;
using MyVocaList.Domain.Resolution;
using MyVocaList.Navigation;
using MyVocaList.UI.Services;

namespace MyVocaList.Tests.Unit.ViewModels;

/// <summary>Deezer artwork on the Song form options and the pending images a pick carries to Save (REQ-ART-06…14, 17, 18).</summary>
public class SongFormViewModelImageTests
{
    private const string Cover = "https://img.test/cover.jpg";
    private const string ArtistPic = "https://img.test/artist.jpg";

    private readonly Mock<INavigationService> _navigationMock = new();
    private readonly Mock<ISongSuggestionService> _suggestionsMock = new();
    private readonly List<SongFormSaveRequest> _requests = [];

    private SongFormViewModel CreateSut(
        Mock<ISongService>? songService = null, Mock<IArtistService>? artistService = null)
    {
        var sut = new SongFormViewModel(
            (artistService ?? new Mock<IArtistService>()).Object,
            (songService ?? new Mock<ISongService>()).Object,
            new Mock<ISnackbarComponent>().Object,
            new Mock<ILogger<SongFormViewModel>>().Object,
            new Mock<ISongKaraokeUrlService>().Object,
            new Mock<ISecureStorageWrapper>().Object,
            new Mock<IMessenger>().Object,
            _navigationMock.Object,
            _suggestionsMock.Object);
        sut.CompleteHydration();
        return sut;
    }

    private void SetupSearch(
        IReadOnlyList<SongSuggestionDto> local,
        IReadOnlyList<SongSuggestionDto> promoted,
        IReadOnlyList<SongSuggestionDto> remote)
    {
        _suggestionsMock
            .Setup(s => s.SearchTitleLocalAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(local);
        _suggestionsMock
            .Setup(s => s.SearchTitleRemoteAsync(It.IsAny<string>(), It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SongRemoteSearchResult(promoted, remote, RemoteSearchStatus.Ok));
    }

    private Mock<ISongService> SaveService(Func<SongFormSaveRequest, SongFormSaveOutcome>? decide = null)
    {
        var songService = new Mock<ISongService>();
        songService.Setup(s => s.ValidateTitleInput(It.IsAny<string>())).Returns((true, string.Empty));
        songService.Setup(s => s.ValidateVersionInput(It.IsAny<string>(), It.IsAny<bool>())).Returns((true, string.Empty));
        songService.Setup(s => s.SaveFromFormAsync(It.IsAny<SongFormSaveRequest>(), It.IsAny<CancellationToken>()))
            .Returns((SongFormSaveRequest r, CancellationToken _) =>
            {
                _requests.Add(r);
                return Task.FromResult((decide ?? (_ => Saved()))(r));
            });
        return songService;
    }

    private static SongFormSaveOutcome Saved() =>
        new(SongFormSaveKind.Saved, "Song created", SaveField.None, 5, [], false, []);

    private static SongFormSaveOutcome SongChoice() =>
        new(SongFormSaveKind.SongChoiceNeeded, string.Empty, SaveField.None, null, [], true,
            [new SongMatch(9, "Creep", string.Empty, "Radiohead", 1.0)]);

    private static SongSuggestionDto LocalDto(
        int id, string title = "Creep", string artist = "Radiohead", string version = "",
        string? image = null, string? artistImage = null, string? album = null) =>
        new(id, title, artist, 1, null, null, false, version, artist, image, artistImage, album);

    private static SongSuggestionDto RemoteDto(
        string title = "Creep", string artist = "Radiohead", string version = "",
        string? image = Cover, string? artistImage = ArtistPic, string? album = null,
        int? localArtistId = null, string? localArtistName = null) =>
        new(null, title, artist, localArtistId, "dz-1", "Deezer", true, version, localArtistName, image, artistImage, album);

    private static AutocompleteSuggestion Pick(SongSuggestionDto dto) => new(dto.Title, string.Empty, dto);

    private static async Task<AutocompleteSuggestion> SingleTitleOption(SongFormViewModel sut) =>
        Assert.Single(await sut.TitleOptionSource.SearchAsync("creep", CancellationToken.None));

    private async Task SaveAsync(SongFormViewModel sut)
    {
        await sut.SaveCommand.ExecuteAsync(null);
    }

    // ── Title option ─────────────────────────────────────────────────────

    [Fact]
    public async Task TitleOption_LocalRowWithSongImage_ShowsSongImageWithPlaceholder()
    {
        // [AC] REQ-ART-06: every Title option has a leading image holding its row image.
        SetupSearch([LocalDto(1, image: Cover, artistImage: ArtistPic)], [], []);

        var option = await SingleTitleOption(CreateSut());

        Assert.Equal(AutocompleteLeadingKind.Image, option.LeadingKind);
        Assert.Equal(Cover, option.ImageUrl);
        Assert.Equal("music_note_outlined", option.PlaceholderIcon);
    }

    [Fact]
    public async Task TitleOption_RemoteRowWithoutCover_FallsBackToArtistImage()
    {
        // [AC] REQ-ART-06: the row image falls back to the artist image.
        SetupSearch([], [], [RemoteDto(image: null)]);

        var option = await SingleTitleOption(CreateSut());

        Assert.Equal(AutocompleteLeadingKind.Image, option.LeadingKind);
        Assert.Equal(ArtistPic, option.ImageUrl);
    }

    [Fact]
    public async Task TitleOption_RowWithNoImages_HasNullImageAndStillThePlaceholder()
    {
        // [AC] REQ-ART-06: with no image at all the placeholder takes the image's place.
        SetupSearch([LocalDto(1)], [], []);

        var option = await SingleTitleOption(CreateSut());

        Assert.Equal(AutocompleteLeadingKind.Image, option.LeadingKind);
        Assert.Null(option.ImageUrl);
        Assert.Equal("music_note_outlined", option.PlaceholderIcon);
    }

    [Fact]
    public async Task TitleOption_RemoteRow_SupportingTextIsArtistVersionAlbum()
    {
        // [AC] REQ-ART-07: artist · version · album for the example of the requirement.
        SetupSearch([], [], [RemoteDto("Commando", "Ramones", "1999 Remaster", album: "Greatest Hits")]);

        var option = await SingleTitleOption(CreateSut());

        Assert.Equal("Ramones · 1999 Remaster · Greatest Hits", option.SupportingText);
    }

    [Fact]
    public async Task TitleOption_LocalRowEmptyVersion_SupportingTextIsArtistOnly()
    {
        // [AC] REQ-ART-07: a local song with an empty version shows the artist only.
        SetupSearch([LocalDto(1, "Don't Cry", "Guns N' Roses", "")], [], []);

        var option = await SingleTitleOption(CreateSut());

        Assert.Equal("Guns N' Roses", option.SupportingText);
    }

    [Theory]
    [InlineData("creep")]
    [InlineData("CREEP")]
    public async Task TitleOption_AlbumEqualToTitleIgnoringCase_OmitsAlbum(string album)
    {
        // [AC] REQ-ART-07: the album is omitted when it equals the title ignoring case.
        SetupSearch([], [], [RemoteDto("Creep", "Radiohead", "Live", album: album)]);

        var option = await SingleTitleOption(CreateSut());

        Assert.Equal("Radiohead · Live", option.SupportingText);
    }

    [Fact]
    public async Task TitleOption_AlbumDiffersByAccent_KeepsAlbum()
    {
        // [AC] REQ-ART-07: ordinal comparison, accents count as different.
        SetupSearch([], [], [RemoteDto("Cafe", "Artist", "", album: "Café")]);

        var option = await SingleTitleOption(CreateSut());

        Assert.Equal("Artist · Café", option.SupportingText);
    }

    [Fact]
    public async Task TitleOption_LocalRowCarryingAlbum_IgnoresAlbum()
    {
        // [AC] REQ-ART-07: album is present on Deezer options only (a local row has none).
        SetupSearch([LocalDto(1, "Creep", "Radiohead", "Live", album: "Pablo Honey")], [], []);

        var option = await SingleTitleOption(CreateSut());

        Assert.Equal("Radiohead · Live", option.SupportingText);
    }

    [Fact]
    public async Task TitleOption_RemoteRowBlankAlbum_OmitsAlbum()
    {
        // [AC] REQ-ART-07: only non-empty parts are joined.
        SetupSearch([], [], [RemoteDto("Creep", "Radiohead", "", album: "   ")]);

        var option = await SingleTitleOption(CreateSut());

        Assert.Equal("Radiohead", option.SupportingText);
    }

    [Fact]
    public async Task TitleOption_LocalRow_TrailingTextIsLibraryAndNoSourceWordInSupportingText()
    {
        // [AC] REQ-ART-08: the source is the trailing text, no longer part of the supporting text.
        SetupSearch([LocalDto(1, "Creep", "Radiohead", "Live")], [], []);

        var option = await SingleTitleOption(CreateSut());

        Assert.Equal("Library", option.TrailingText);
        Assert.DoesNotContain("library", option.SupportingText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TitleOption_RemoteRow_TrailingTextIsDeezerAndNoSourceWordInSupportingText()
    {
        // [AC] REQ-ART-08: the Deezer option names its source as trailing text.
        SetupSearch([], [], [RemoteDto("Creep", "Radiohead", "Live")]);

        var option = await SingleTitleOption(CreateSut());

        Assert.Equal("Deezer", option.TrailingText);
        Assert.DoesNotContain("Deezer", option.SupportingText);
    }

    // ── Artist option ────────────────────────────────────────────────────

    [Fact]
    public async Task ArtistOption_WithImage_IsAvatarWithPlaceholderAndCatalogCountText()
    {
        // [AC] REQ-ART-09: every Artist option has a leading avatar holding the artist's image.
        var artistService = new Mock<IArtistService>();
        var item = new ArtistListItem(2, "Ramones", string.Empty, 3, ArtistPic);
        artistService
            .Setup(s => s.SearchArtistsByNameAsync("ram", It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([item]);
        var sut = CreateSut(artistService: artistService);

        var option = Assert.Single(await sut.ArtistOptionSource.SearchAsync("ram", CancellationToken.None));

        Assert.Equal(AutocompleteLeadingKind.Avatar, option.LeadingKind);
        Assert.Equal(ArtistPic, option.ImageUrl);
        Assert.Equal("account_circle_outlined", option.PlaceholderIcon);
        Assert.Null(option.TrailingText);
        // [AC] REQ-ART-10: the supporting text is the catalog count text.
        Assert.Equal(item.CatalogCountText, option.SupportingText);
        Assert.Equal("3 songs in catalog", option.SupportingText);
    }

    [Fact]
    public async Task ArtistOption_WithoutImage_HasNullImageAndStillThePlaceholder()
    {
        // [AC] REQ-ART-09: the placeholder shows when the artist has no image.
        var artistService = new Mock<IArtistService>();
        artistService
            .Setup(s => s.SearchArtistsByNameAsync("ram", It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ArtistListItem(2, "Ramones", string.Empty, 0)]);
        var sut = CreateSut(artistService: artistService);

        var option = Assert.Single(await sut.ArtistOptionSource.SearchAsync("ram", CancellationToken.None));

        Assert.Equal(AutocompleteLeadingKind.Avatar, option.LeadingKind);
        Assert.Null(option.ImageUrl);
        Assert.Equal("account_circle_outlined", option.PlaceholderIcon);
    }

    // ── Pending images ───────────────────────────────────────────────────

    [Fact]
    public async Task Save_AfterDeezerPick_RequestCarriesCoverNotFallbackAndArtistImage()
    {
        // [AC] REQ-ART-11: a Deezer pick sets the pending song image (the cover) and artist image.
        var sut = CreateSut(SaveService());
        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto(image: Cover, artistImage: ArtistPic)));

        await SaveAsync(sut);

        var request = Assert.Single(_requests);
        Assert.Equal(Cover, request.ImageUrl);
        Assert.Equal(ArtistPic, request.ArtistImageUrl);
    }

    [Fact]
    public async Task Save_AfterDeezerPickWithoutCover_SongImageStaysNullEvenThoughRowShowsArtistImage()
    {
        // [AC] REQ-ART-11: the pending song image is the cover, not the row-image fallback.
        var sut = CreateSut(SaveService());
        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto(image: null, artistImage: ArtistPic)));

        await SaveAsync(sut);

        var request = Assert.Single(_requests);
        Assert.Null(request.ImageUrl);
        Assert.Equal(ArtistPic, request.ArtistImageUrl);
    }

    [Fact]
    public async Task Save_AfterTwoDeezerPicks_SecondReplacesBothImages()
    {
        // [AC] REQ-ART-11: a later Deezer pick replaces both pending images.
        var sut = CreateSut(SaveService());
        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto(image: Cover, artistImage: ArtistPic)));
        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto(image: null, artistImage: null)));

        await SaveAsync(sut);

        var request = Assert.Single(_requests);
        Assert.Null(request.ImageUrl);
        Assert.Null(request.ArtistImageUrl);
    }

    [Fact]
    public async Task SelectTitle_RemoteOption_LeavesFeaturedLyricsAndKaraokeUrlsAndWritesNothing()
    {
        // [AC] REQ-ART-12: a Deezer pick leaves Featured artists, Lyrics and karaoke URLs unchanged and writes nothing.
        var songService = new Mock<ISongService>();
        var sut = CreateSut(songService);
        sut.FeaturedArtists = "Thom Yorke";
        sut.Lyrics = "some lyrics";

        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto()));

        Assert.Equal("Thom Yorke", sut.FeaturedArtists);
        Assert.Equal("some lyrics", sut.Lyrics);
        Assert.Empty(sut.KaraokeUrls);
        songService.Verify(s => s.SaveFromFormAsync(It.IsAny<SongFormSaveRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        songService.Verify(s => s.CreateSongAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        songService.Verify(s => s.UpdateSongAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Save_AfterDeezerPickThenXOnTitle_DropsSongImageKeepsArtistImage()
    {
        // [AC] REQ-ART-13: X on Title clears the pending song image and keeps the pending artist image.
        var sut = CreateSut(SaveService());
        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto()));

        sut.ClearTitleCommand.Execute(null);
        sut.SongTitle = "Other";
        await SaveAsync(sut);

        var request = Assert.Single(_requests);
        Assert.Null(request.ImageUrl);
        Assert.Equal(ArtistPic, request.ArtistImageUrl);
    }

    [Fact]
    public async Task Save_AfterDeezerPickThenXOnArtist_DropsBothImages()
    {
        // [AC] REQ-ART-13: X on Artist clears both pending images.
        var sut = CreateSut(SaveService());
        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto()));

        sut.ClearArtistCommand.Execute(null);
        sut.ArtistSearchText = "Someone";
        await SaveAsync(sut);

        var request = Assert.Single(_requests);
        Assert.Null(request.ImageUrl);
        Assert.Null(request.ArtistImageUrl);
    }

    [Fact]
    public async Task Save_DeezerPickThenXOnArtistThenLocalArtistPick_SendsNoImages()
    {
        // [AC] REQ-ART-14: Deezer pick, X on Artist, local artist pick, Save -> no pending image.
        var sut = CreateSut(SaveService());
        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto()));
        sut.ClearArtistCommand.Execute(null);

        sut.SelectArtistCommand.Execute(new AutocompleteSuggestion("Other", string.Empty, new ArtistListItem(8, "Other", string.Empty, 1, "https://img.test/other.jpg")));
        await SaveAsync(sut);

        var request = Assert.Single(_requests);
        Assert.Equal(8, request.ArtistId);
        Assert.Null(request.ImageUrl);
        Assert.Null(request.ArtistImageUrl);
    }

    [Fact]
    public async Task Save_LocalArtistPickWithNoDeezerPick_SendsNoImages()
    {
        // [AC] REQ-ART-14: a local Artist pick sets the Artist only and changes no pending image.
        var sut = CreateSut(SaveService());
        sut.SongTitle = "Creep";

        sut.SelectArtistCommand.Execute(new AutocompleteSuggestion("Radiohead", string.Empty, new ArtistListItem(8, "Radiohead", string.Empty, 1, ArtistPic)));
        await SaveAsync(sut);

        var request = Assert.Single(_requests);
        Assert.Null(request.ImageUrl);
        Assert.Null(request.ArtistImageUrl);
    }

    [Fact]
    public async Task Save_LocalArtistPickAfterDeezerPick_KeepsPendingImages()
    {
        // [AC] REQ-ART-14: a local Artist pick changes no pending image.
        var sut = CreateSut(SaveService());
        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto()));

        sut.SelectArtistCommand.Execute(new AutocompleteSuggestion("Radiohead", string.Empty, new ArtistListItem(8, "Radiohead", string.Empty, 1)));
        await SaveAsync(sut);

        var request = Assert.Single(_requests);
        Assert.Equal(Cover, request.ImageUrl);
        Assert.Equal(ArtistPic, request.ArtistImageUrl);
    }

    [Fact]
    public async Task Save_EditModeAfterDeezerPick_SendsNullImages()
    {
        // [AC] REQ-ART-17: edit mode never sends images.
        var sut = CreateSut(SaveService());
        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto()));
        sut.SongIdRaw = "42";

        await SaveAsync(sut);

        var request = Assert.Single(_requests);
        Assert.Equal(42, request.SongId);
        Assert.Null(request.ImageUrl);
        Assert.Null(request.ArtistImageUrl);
    }

    [Fact]
    public async Task Save_AfterNeedsDecisionOutcome_NextRequestStillCarriesBothImages()
    {
        // [AC] REQ-ART-18: a Save stopped at a sheet keeps the pending images for the next Save.
        var sut = CreateSut(SaveService(r => r.SongDecision == SongDecision.None ? SongChoice() : Saved()));
        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto()));

        await SaveAsync(sut);
        await sut.ConfirmSaveAsNewSongCommand.ExecuteAsync(null);

        Assert.Equal(2, _requests.Count);
        Assert.Equal(Cover, _requests[1].ImageUrl);
        Assert.Equal(ArtistPic, _requests[1].ArtistImageUrl);
        Assert.Equal(SongDecision.SaveAsNewSong, _requests[1].SongDecision);
    }

    [Fact]
    public async Task Save_AfterFailedOutcome_NextSaveStillCarriesBothImages()
    {
        // [AC] REQ-ART-18: a refused Save keeps the pending images.
        var attempt = 0;
        var sut = CreateSut(SaveService(_ => ++attempt == 1
            ? new SongFormSaveOutcome(SongFormSaveKind.Failed, "No", SaveField.None, null, [], false, [])
            : Saved()));
        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto()));

        await SaveAsync(sut);
        await SaveAsync(sut);

        Assert.Equal(2, _requests.Count);
        Assert.Equal(Cover, _requests[1].ImageUrl);
        Assert.Equal(ArtistPic, _requests[1].ArtistImageUrl);
    }
}
