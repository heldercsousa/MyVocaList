using CommunityToolkit.Mvvm.Messaging;
using MyVocaList.Contracts.DTOs;
using MyVocaList.Contracts.DTOs.Suggestions;
using MyVocaList.Domain.Resolution;
using MyVocaList.Navigation;
using MyVocaList.Tests.Infrastructure;
using MyVocaList.UI.Services;

namespace MyVocaList.Tests.Unit.ViewModels;

/// <summary>Lyrics lookup started by a Deezer pick in the Song form (REQ-LYR-01…14).</summary>
public class SongFormViewModelLyricsTests
{
    private const string IdlePlaceholder = "Song lyrics (optional)";
    private const string LookingPlaceholder = "Looking for lyrics…";

    /// <summary>One lookup call of the lyrics service: its query, its token and the answer the test controls.</summary>
    private sealed record Lookup(LyricsQuery Query, CancellationToken Token, TaskCompletionSource<string?> Answer);

    private readonly Mock<INavigationService> _navigationMock = new();
    private readonly Mock<ISongSuggestionService> _suggestionsMock = new();
    private readonly Mock<ISongLyricsService> _lyricsMock = new();
    private readonly List<Lookup> _lookups = [];

    private SongFormViewModel CreateSut(Mock<ISongService>? songService = null)
    {
        var sut = new SongFormViewModel(
            new Mock<IArtistService>().Object,
            (songService ?? new Mock<ISongService>()).Object,
            new Mock<ISnackbarComponent>().Object,
            new Mock<ILogger<SongFormViewModel>>().Object,
            new Mock<ISongKaraokeUrlService>().Object,
            new Mock<ISecureStorageWrapper>().Object,
            new Mock<IMessenger>().Object,
            _navigationMock.Object,
            _suggestionsMock.Object,
            TestImageStore.PassThrough().Object,
            new Mock<IPhotoPicker>().Object,
            _lyricsMock.Object);
        sut.CompleteHydration();
        return sut;
    }

    /// <summary>Every lookup stays pending until the test completes its <see cref="Lookup.Answer"/>.</summary>
    private void AnswerLater() =>
        _lyricsMock.Setup(s => s.FindLyricsAsync(It.IsAny<LyricsQuery>(), It.IsAny<CancellationToken>()))
            .Returns((LyricsQuery query, CancellationToken ct) =>
            {
                var answer = new TaskCompletionSource<string?>();
                _lookups.Add(new Lookup(query, ct, answer));
                return answer.Task;
            });

    /// <summary>Every lookup answers at once with <paramref name="lyrics"/>.</summary>
    private void AnswerNow(string? lyrics) =>
        _lyricsMock.Setup(s => s.FindLyricsAsync(It.IsAny<LyricsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lyrics);

    /// <summary>Save service whose validation passes and whose save succeeds.</summary>
    private static Mock<ISongService> SaveService()
    {
        var songService = new Mock<ISongService>();
        songService.Setup(s => s.ValidateTitleInput(It.IsAny<string>())).Returns((true, string.Empty));
        songService.Setup(s => s.ValidateVersionInput(It.IsAny<string>(), It.IsAny<bool>())).Returns((true, string.Empty));
        songService.Setup(s => s.SaveFromFormAsync(It.IsAny<SongFormSaveRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SongFormSaveOutcome(SongFormSaveKind.Saved, "Song created", SaveField.None, 5, [], false, []));
        return songService;
    }

    private static SongSuggestionDto RemoteDto(
        string title = "Creep", string artist = "Radiohead", string? album = "Pablo Honey", int? duration = 238,
        int? localArtistId = null, string? localArtistName = null) =>
        new(null, title, artist, localArtistId, "dz-1", "Deezer", true, string.Empty, localArtistName,
            "https://img.test/cover.jpg", null, album, duration);

    private static AutocompleteSuggestion Pick(SongSuggestionDto dto) => new(dto.Title, string.Empty, dto);

    private static void AssertNoSave(Mock<ISongService> songService) =>
        Assert.DoesNotContain(songService.Invocations, i => i.Method.Name is
            "SaveFromFormAsync" or "CreateSongAsync" or "CreateSongWithUrlsAsync" or "UpdateSongAsync");

    // ── Start (REQ-LYR-01) ────────────────────────────────────────────────

    [Fact]
    public async Task PickWithoutLocalArtist_StartsOneLookupWithThePicksFields()
    {
        // [AC] REQ-LYR-01: a Deezer pick starts exactly one lyrics lookup with the picked title, artist name, album title and duration.
        AnswerLater();
        var sut = CreateSut();

        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto()));

        _lyricsMock.Verify(s => s.FindLyricsAsync(
            new LyricsQuery("Creep", "Radiohead", "Pablo Honey", 238), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PickWithLocalArtist_StartsOneLookupToo()
    {
        // [AC] REQ-LYR-01: a Deezer pick whose artist exists locally (the early-return branch) also starts exactly one lookup.
        AnswerLater();
        var sut = CreateSut();

        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto(localArtistId: 3, localArtistName: "Radiohead")));

        _lyricsMock.Verify(s => s.FindLyricsAsync(
            new LyricsQuery("Creep", "Radiohead", "Pablo Honey", 238), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── Fill and no match (REQ-LYR-02, 03) ────────────────────────────────

    [Fact]
    public async Task Match_FillsEmptyLyrics()
    {
        // [AC] REQ-LYR-02: a lyrics match fills the blank Lyrics field.
        AnswerNow("Lyrics of the match");
        var sut = CreateSut();

        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto()));
        await sut.LyricsLookupTask;

        Assert.Equal("Lyrics of the match", sut.Lyrics);
    }

    [Fact]
    public async Task NoMatch_LeavesLyricsBlank()
    {
        // [AC] REQ-LYR-03: when the lyrics source has no match the Lyrics field stays blank.
        AnswerNow(null);
        var sut = CreateSut();

        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto()));
        await sut.LyricsLookupTask;

        Assert.True(string.IsNullOrWhiteSpace(sut.Lyrics));
    }

    // ── Typed lyrics are never replaced (REQ-LYR-04) ──────────────────────

    [Fact]
    public async Task NonBlankLyricsAtPickTime_StartsNoLookup()
    {
        // [AC] REQ-LYR-04: given non-blank Lyrics at pick time, no lyrics lookup is started.
        AnswerNow("Unused");
        var sut = CreateSut();
        sut.Lyrics = "Typed before the pick";

        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto()));

        _lyricsMock.Verify(s => s.FindLyricsAsync(It.IsAny<LyricsQuery>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Equal("Typed before the pick", sut.Lyrics);
    }

    [Fact]
    public async Task TypedWhileLookupPending_TypedTextStaysAfterTheAnswer()
    {
        // [AC] REQ-LYR-04: text typed while the lookup runs is kept when the answer arrives.
        AnswerLater();
        var sut = CreateSut();
        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto()));
        var pending = _lookups.Single();

        sut.Lyrics = "My own words";
        pending.Answer.SetResult("Matched lyrics");
        await sut.LyricsLookupTask;

        Assert.Equal("My own words", sut.Lyrics);
    }

    [Fact]
    public async Task TypedThenEmptiedWhileLookupPending_AnswerFillsTheField()
    {
        // [AC] REQ-LYR-04: a field the user emptied again is blank, so the answer fills it.
        AnswerLater();
        var sut = CreateSut();
        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto()));
        var pending = _lookups.Single();

        sut.Lyrics = "My own words";
        sut.Lyrics = string.Empty;
        pending.Answer.SetResult("Matched lyrics");
        await sut.LyricsLookupTask;

        Assert.Equal("Matched lyrics", sut.Lyrics);
    }

    // ── Placeholder hint (REQ-LYR-09) ─────────────────────────────────────

    [Fact]
    public async Task LyricsPlaceholder_ReadsLookingForLyricsWhilePending_IdleOtherwise()
    {
        // [AC] REQ-LYR-09: the Lyrics placeholder reads "Looking for lyrics…" while a lookup runs and "Song lyrics (optional)" otherwise.
        AnswerLater();
        var sut = CreateSut();
        Assert.Equal(IdlePlaceholder, sut.LyricsPlaceholder);

        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto()));
        Assert.Equal(LookingPlaceholder, sut.LyricsPlaceholder);

        _lookups.Single().Answer.SetResult(null);
        await sut.LyricsLookupTask;
        Assert.Equal(IdlePlaceholder, sut.LyricsPlaceholder);
    }

    [Fact]
    public async Task LyricsPlaceholder_RaisesPropertyChangedWhenTheLookupStartsAndEnds()
    {
        // [AC] REQ-LYR-09: LyricsPlaceholder is change-notified when the lookup starts and when it ends.
        AnswerLater();
        var sut = CreateSut();
        var changed = new List<string?>();
        sut.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto()));
        Assert.Contains(nameof(SongFormViewModel.LyricsPlaceholder), changed);

        changed.Clear();
        _lookups.Single().Answer.SetResult(null);
        await sut.LyricsLookupTask;
        Assert.Contains(nameof(SongFormViewModel.LyricsPlaceholder), changed);
    }

    // ── A new pick, X or Save cancels the lookup (REQ-LYR-10) ─────────────

    [Fact]
    public async Task SecondPick_CancelsFirstLookup_AndItsLateAnswerChangesNothing()
    {
        // [AC] REQ-LYR-10: another Deezer pick cancels the running lookup; its late answer changes neither Lyrics nor the hint of the new pick.
        AnswerLater();
        var sut = CreateSut();
        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto()));
        var first = _lookups[0];
        var firstTask = sut.LyricsLookupTask;

        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto(title: "Creep (Live)")));
        Assert.True(first.Token.IsCancellationRequested);

        first.Answer.SetResult("Late lyrics of the first pick");
        await firstTask;

        Assert.True(string.IsNullOrWhiteSpace(sut.Lyrics));
        Assert.Equal(LookingPlaceholder, sut.LyricsPlaceholder);
    }

    [Fact]
    public async Task ClearTitle_CancelsTheLookup_AndLateAnswerIsIgnored()
    {
        // [AC] REQ-LYR-10: X on Title cancels the running lookup; its late answer is ignored.
        AnswerLater();
        var sut = CreateSut();
        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto()));
        var lookup = _lookups.Single();
        var task = sut.LyricsLookupTask;

        sut.ClearTitleCommand.Execute(null);
        Assert.True(lookup.Token.IsCancellationRequested);

        lookup.Answer.SetResult("Late lyrics");
        await task;

        Assert.True(string.IsNullOrWhiteSpace(sut.Lyrics));
        Assert.Equal(IdlePlaceholder, sut.LyricsPlaceholder);
    }

    [Fact]
    public async Task ClearArtist_CancelsTheLookup_AndLateAnswerIsIgnored()
    {
        // [AC] REQ-LYR-10: X on Artist cancels the running lookup; its late answer is ignored.
        AnswerLater();
        var sut = CreateSut();
        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto()));
        var lookup = _lookups.Single();
        var task = sut.LyricsLookupTask;

        sut.ClearArtistCommand.Execute(null);
        Assert.True(lookup.Token.IsCancellationRequested);

        lookup.Answer.SetResult("Late lyrics");
        await task;

        Assert.True(string.IsNullOrWhiteSpace(sut.Lyrics));
        Assert.Equal(IdlePlaceholder, sut.LyricsPlaceholder);
    }

    [Fact]
    public async Task Save_CancelsTheLookup_AndLateAnswerIsIgnored()
    {
        // [AC] REQ-LYR-10: Save starts cancels the running lookup; its late answer is ignored.
        AnswerLater();
        var sut = CreateSut(SaveService());
        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto()));
        var lookup = _lookups.Single();
        var task = sut.LyricsLookupTask;

        await sut.SaveCommand.ExecuteAsync(null);
        Assert.True(lookup.Token.IsCancellationRequested);

        lookup.Answer.SetResult("Late lyrics");
        await task;

        Assert.True(string.IsNullOrWhiteSpace(sut.Lyrics));
    }

    // ── X removes untouched pick-filled lyrics (REQ-LYR-11) ───────────────

    [Fact]
    public async Task ClearTitle_AfterFill_EmptiesLyrics()
    {
        // [AC] REQ-LYR-11: X on Title empties the Lyrics field when it still holds the pick-filled lyrics.
        AnswerNow("Matched lyrics");
        var sut = CreateSut();
        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto()));
        await sut.LyricsLookupTask;

        sut.ClearTitleCommand.Execute(null);

        Assert.True(string.IsNullOrWhiteSpace(sut.Lyrics));
    }

    [Fact]
    public async Task ClearTitle_AfterFillAndEdit_KeepsTheEditedLyrics()
    {
        // [AC] REQ-LYR-11: X on Title keeps lyrics the user edited after the fill.
        AnswerNow("Matched lyrics");
        var sut = CreateSut();
        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto()));
        await sut.LyricsLookupTask;
        sut.Lyrics = "Matched lyrics, edited";

        sut.ClearTitleCommand.Execute(null);

        Assert.Equal("Matched lyrics, edited", sut.Lyrics);
    }

    [Fact]
    public async Task ClearArtist_AfterFill_EmptiesLyrics()
    {
        // [AC] REQ-LYR-11: X on Artist empties the Lyrics field when it still holds the pick-filled lyrics.
        AnswerNow("Matched lyrics");
        var sut = CreateSut();
        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto()));
        await sut.LyricsLookupTask;

        sut.ClearArtistCommand.Execute(null);

        Assert.True(string.IsNullOrWhiteSpace(sut.Lyrics));
    }

    [Fact]
    public async Task ClearArtist_AfterFillAndEdit_KeepsTheEditedLyrics()
    {
        // [AC] REQ-LYR-11: X on Artist keeps lyrics the user edited after the fill.
        AnswerNow("Matched lyrics");
        var sut = CreateSut();
        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto()));
        await sut.LyricsLookupTask;
        sut.Lyrics = "Matched lyrics, edited";

        sut.ClearArtistCommand.Execute(null);

        Assert.Equal("Matched lyrics, edited", sut.Lyrics);
    }

    // ── A second pick replaces untouched pick-filled lyrics (REQ-LYR-12) ──

    [Fact]
    public async Task SecondPick_AfterFill_EmptiesLyricsAndStartsANewLookup()
    {
        // [AC] REQ-LYR-12: a second Deezer pick empties the pick-filled Lyrics and starts a new lookup for the new pick.
        AnswerLater();
        var sut = CreateSut();
        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto()));
        _lookups[0].Answer.SetResult("First pick lyrics");
        await sut.LyricsLookupTask;

        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto(title: "Creep (Live)")));

        Assert.True(string.IsNullOrWhiteSpace(sut.Lyrics));
        Assert.Equal(2, _lookups.Count);
        Assert.Equal("Creep (Live)", _lookups[1].Query.Title);
        Assert.Equal(LookingPlaceholder, sut.LyricsPlaceholder);
    }

    [Fact]
    public async Task SecondPick_AfterFillAndEdit_StartsNoLookupAndKeepsTheText()
    {
        // [AC] REQ-LYR-12: a second Deezer pick after the user edited the pick-filled lyrics starts no lookup and keeps the text.
        AnswerLater();
        var sut = CreateSut();
        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto()));
        _lookups[0].Answer.SetResult("First pick lyrics");
        await sut.LyricsLookupTask;
        sut.Lyrics = "Edited after the fill";

        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto(title: "Creep (Live)")));

        Assert.Equal("Edited after the fill", sut.Lyrics);
        _lyricsMock.Verify(s => s.FindLyricsAsync(It.IsAny<LyricsQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── Nothing is stored before Save (REQ-LYR-13) ────────────────────────

    [Fact]
    public async Task Lookup_CallsNoSaveMethodOfSongService_DuringOrAfter()
    {
        // [AC] REQ-LYR-13: during and after a lyrics lookup no save method of ISongService is called.
        AnswerLater();
        var songService = SaveService();
        var sut = CreateSut(songService);
        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto()));
        AssertNoSave(songService);

        _lookups.Single().Answer.SetResult("Matched lyrics");
        await sut.LyricsLookupTask;

        AssertNoSave(songService);
    }

    // ── Scope of the trigger (REQ-LYR-14) ─────────────────────────────────

    [Fact]
    public async Task LocalPick_NavigatesAndStartsNoLookup()
    {
        // [AC] REQ-LYR-14: a local pick opens the stored song and starts no lyrics lookup.
        AnswerNow("Unused");
        var sut = CreateSut();

        await sut.SelectTitleCommand.ExecuteAsync(Pick(new SongSuggestionDto(7, "Creep", "Radiohead", 1, null, null, false)));

        _lyricsMock.Verify(s => s.FindLyricsAsync(It.IsAny<LyricsQuery>(), It.IsAny<CancellationToken>()), Times.Never);
        _navigationMock.Verify(n => n.GoToAsync(It.Is<string>(route => route.Contains("songId=7"))), Times.Once);
    }
}
