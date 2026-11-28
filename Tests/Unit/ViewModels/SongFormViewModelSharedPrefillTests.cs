using MyVocaList.Contracts.DTOs;
using MyVocaList.Contracts.DTOs.Suggestions;
using MyVocaList.Domain.Resolution;
using MyVocaList.Tests.Infrastructure;
using MyVocaList.UI.Services;

namespace MyVocaList.Tests.Unit.ViewModels;

/// <summary>REQ-MSRC-12, -13: a shared YouTube link prefills the add form from the video title.</summary>
[Collection(DispatcherProviderCollection.Name)]
public class SongFormViewModelSharedPrefillTests
{
    private const string VideoId = "dQw4w9WgXcQ";
    private const string Link = "https://youtu.be/" + VideoId;
    private const string ArtistMbid = "0383dadf-2a4e-4d10-a46a-e9e041da8eb3";

    private readonly Mock<ISharedVideoService> _sharedVideo = new();
    private readonly Mock<ISongKaraokeUrlService> _urlService = new();
    private readonly Mock<ISongLyricsService> _lyrics = new();
    private readonly Mock<ISharedLinkInbox> _inbox = new();
    private readonly Mock<ISongService> _songService = new();

    public SongFormViewModelSharedPrefillTests()
    {
        _urlService.Setup(u => u.ExtractVideoId(It.IsAny<string>())).Returns(VideoId);
        _inbox.Setup(i => i.TakePending()).Returns(Link);
        _lyrics.Setup(l => l.LookupLyricsAsync(It.IsAny<LyricsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(LyricsLookupResult.NoAnswer);
        _songService.Setup(s => s.ValidateTitleInput(It.IsAny<string>())).Returns((true, string.Empty));
        _songService.Setup(s => s.SaveFromFormAsync(It.IsAny<SongFormSaveRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SongFormSaveOutcome(SongFormSaveKind.Saved, "Song created", SaveField.None, 5, [], false, []));
    }

    private SongFormViewModel CreateSut()
    {
        var sut = new SongFormViewModel(
            new Mock<IArtistService>().Object,
            _songService.Object,
            new Mock<ISnackbarComponent>().Object,
            new Mock<ILogger<SongFormViewModel>>().Object,
            _urlService.Object,
            _inbox.Object,
            _sharedVideo.Object,
            new Mock<INavigationService>().Object,
            new Mock<ISongSuggestionService>().Object,
            TestImageStore.PassThrough().Object,
            new Mock<IPhotoPicker>().Object,
            _lyrics.Object);
        sut.CompleteHydration();
        return sut;
    }

    private static SongSuggestionDto Match(string title = "Bohemian Rhapsody", string artist = "Queen")
        => new(null, title, artist, null, "rec-1", "MusicBrainz", true, null, null, null, "A Night at the Opera", 354, ArtistMbid);

    private void PrefillReturns(VideoPrefill? prefill)
        => _sharedVideo.Setup(s => s.PrefillAsync(VideoId, It.IsAny<CancellationToken>())).ReturnsAsync(prefill);

    // [AC] REQ-MSRC-11: the shared video is already in the URL list of the add form (unsaved until Save)
    // [AC] REQ-MSRC-12: the best recording fills Title and Artist in the picked state
    [Fact]
    public async Task SharedLink_AddMode_BestRecordingFillsPickedState()
    {
        PrefillReturns(new VideoPrefill("Queen - Bohemian Rhapsody", Match()));
        var sut = CreateSut();

        await sut.ApplyPendingSharedLinkAsync();
        await sut.SharedPrefillTask;

        Assert.Single(sut.KaraokeUrls);
        Assert.Equal("Bohemian Rhapsody", sut.SongTitle);
        Assert.True(sut.IsTitleLocked);
        Assert.Equal("Queen", sut.ArtistSearchText);
        Assert.True(sut.IsArtistLocked);
        Assert.Equal("rec-1", sut.SelectedExternalId);
        Assert.Equal("MusicBrainz", sut.SelectedProvider);
    }

    // [AC] REQ-MSRC-12: lyrics are fetched for the recording through the lyrics service
    [Fact]
    public async Task SharedLink_AddMode_MatchStartsLyricsLookup()
    {
        PrefillReturns(new VideoPrefill("Queen - Bohemian Rhapsody", Match()));
        _lyrics.Setup(l => l.LookupLyricsAsync(
                It.Is<LyricsQuery>(q => q.Title == "Bohemian Rhapsody" && q.ArtistName == "Queen"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(LyricsLookupResult.Found("Is this the real life"));
        var sut = CreateSut();

        await sut.ApplyPendingSharedLinkAsync();
        await sut.SharedPrefillTask;
        await sut.LyricsLookupTask;

        Assert.Equal("Is this the real life", sut.Lyrics);
    }

    // [AC] REQ-MSRC-12: nothing matches -> Title gets the cleaned text and the fields stay editable
    [Fact]
    public async Task SharedLink_AddMode_NoMatch_TitleGetsCleanedTextFieldsEditable()
    {
        PrefillReturns(new VideoPrefill("Unknown Artist - Unknown Song", null));
        var sut = CreateSut();

        await sut.ApplyPendingSharedLinkAsync();
        await sut.SharedPrefillTask;

        Assert.Equal("Unknown Artist - Unknown Song", sut.SongTitle);
        Assert.False(sut.IsTitleLocked);
        Assert.False(sut.IsArtistLocked);
        Assert.Equal(string.Empty, sut.ArtistSearchText);
        Assert.Single(sut.KaraokeUrls);
    }

    // [AC] REQ-MSRC-12: offline or an unreadable title -> the form stays as it is, the link is still attached
    [Fact]
    public async Task SharedLink_AddMode_PrefillUnavailable_LeavesFormUntouched()
    {
        PrefillReturns(null);
        var sut = CreateSut();

        await sut.ApplyPendingSharedLinkAsync();
        await sut.SharedPrefillTask;

        Assert.Equal(string.Empty, sut.SongTitle);
        Assert.Single(sut.KaraokeUrls);
    }

    // [AC] REQ-MSRC-12: a failing prefill never breaks the form
    [Fact]
    public async Task SharedLink_AddMode_PrefillThrows_FormStillUsable()
    {
        _sharedVideo.Setup(s => s.PrefillAsync(VideoId, It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("boom"));
        var sut = CreateSut();

        await sut.ApplyPendingSharedLinkAsync();
        await sut.SharedPrefillTask;

        Assert.Single(sut.KaraokeUrls);
        Assert.Equal(string.Empty, sut.SongTitle);
    }

    // [AC] REQ-MSRC-12: prefill is for add mode; an edit-mode form is never prefilled
    [Fact]
    public async Task SharedLink_EditMode_NoPrefill()
    {
        var sut = CreateSut();
        sut.SongId = 42;
        _urlService.Setup(u => u.AddUrlAsync(42, Link, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, string.Empty, (SongKaraokeUrlDto?)new SongKaraokeUrlDto(VideoId, 42, 0, null, null, DateTime.UtcNow, null, false)));

        await sut.ApplyPendingSharedLinkAsync();
        await sut.SharedPrefillTask;

        _sharedVideo.Verify(s => s.PrefillAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // [AC] REQ-MSRC-12: what the user already typed is never overwritten by a prefill
    [Fact]
    public async Task SharedLink_AddMode_TitleAlreadyTyped_NoPrefill()
    {
        PrefillReturns(new VideoPrefill("Queen - Bohemian Rhapsody", Match()));
        var sut = CreateSut();
        sut.SongTitle = "My own title";

        await sut.ApplyPendingSharedLinkAsync();
        await sut.SharedPrefillTask;

        Assert.Equal("My own title", sut.SongTitle);
        _sharedVideo.Verify(s => s.PrefillAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // [AC] REQ-MSRC-13: Save sends the MusicBrainz artist id of the pick, so the new artist is created with it
    [Fact]
    public async Task Save_AfterSharedPrefill_SendsArtistMusicBrainzId()
    {
        PrefillReturns(new VideoPrefill("Queen - Bohemian Rhapsody", Match()));
        var sut = CreateSut();
        await sut.ApplyPendingSharedLinkAsync();
        await sut.SharedPrefillTask;

        await sut.SaveCommand.ExecuteAsync(null);

        _songService.Verify(s => s.SaveFromFormAsync(
            It.Is<SongFormSaveRequest>(r => r.ArtistExternalId == ArtistMbid && r.ArtistText == "Queen" && r.ExternalId == "rec-1"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // [AC] REQ-MSRC-13: an artist name the user changed after the pick no longer carries the picked artist's id
    [Fact]
    public async Task Save_ArtistTextChangedAfterPick_DoesNotSendArtistMusicBrainzId()
    {
        PrefillReturns(new VideoPrefill("Queen - Bohemian Rhapsody", Match()));
        var sut = CreateSut();
        await sut.ApplyPendingSharedLinkAsync();
        await sut.SharedPrefillTask;
        sut.ArtistSearchText = "Someone Else";

        await sut.SaveCommand.ExecuteAsync(null);

        _songService.Verify(s => s.SaveFromFormAsync(
            It.Is<SongFormSaveRequest>(r => r.ArtistExternalId == null), It.IsAny<CancellationToken>()), Times.Once);
    }
}
