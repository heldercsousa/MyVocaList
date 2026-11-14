using CommunityToolkit.Mvvm.Messaging;
using MyVocaList.Contracts.DTOs;
using MyVocaList.Contracts.DTOs.Suggestions;
using MyVocaList.Domain.ReadModels;
using MyVocaList.Domain.Resolution;
using MyVocaList.Navigation;
using MyVocaList.Tests.Infrastructure;
using MyVocaList.UI.Services;

namespace MyVocaList.Tests.Unit.ViewModels;

public class SongFormViewModelTests
{
    private static SongKaraokeUrlDto MakeDto(string videoId = "abc123", int songId = 42) =>
        new(videoId, songId, 0, null, null, DateTime.UtcNow, null, false);

    private readonly Mock<INavigationService> _navigationMock = new();

    private SongFormViewModel CreateSut(
        Mock<ISongKaraokeUrlService>? urlService = null,
        Mock<ISnackbarComponent>? snackbar = null,
        Mock<ISecureStorageWrapper>? secureStorage = null,
        Mock<ISongService>? songService = null,
        Mock<IArtistService>? artistService = null,
        IMessenger? messenger = null)
    {
        return new SongFormViewModel(
            (artistService ?? new Mock<IArtistService>()).Object,
            (songService ?? new Mock<ISongService>()).Object,
            (snackbar ?? new Mock<ISnackbarComponent>()).Object,
            new Mock<ILogger<SongFormViewModel>>().Object,
            (urlService ?? new Mock<ISongKaraokeUrlService>()).Object,
            (secureStorage ?? new Mock<ISecureStorageWrapper>()).Object,
            messenger ?? new Mock<IMessenger>().Object,
            _navigationMock.Object,
            _suggestionsMock.Object,
            TestImageStore.PassThrough().Object,
            new Mock<IPhotoPicker>().Object);
    }

    // ── Resolution sheet: Open existing (Task 5.1) ─────────────────────────

    [Fact]
    // [AC] REQ-SFT-22: Open existing on the resolution sheet opens the stored song's edit form in place of the add form.
    public async Task OpenExistingSong_FromResolutionSheet_NavigatesToEditFormReplacingAddForm()
    {
        var sut = CreateSut();
        var match = new SongMatch(77, "Creep", "", "Radiohead", 1.0);

        await sut.OpenExistingSongCommand.ExecuteAsync(match);

        _navigationMock.Verify(n => n.GoToAsync($"../{Routes.SongForm}?songId=77"), Times.Once);
        Assert.False(sut.IsResolutionSheetVisible);
    }

    // ── Task 5.2: title search, picks, online-search note ──────────────────

    private const string OfflineNoteText = "You're offline — showing songs from your library only.";
    private const string UnavailableNoteText = "Online search isn't available right now — showing your library only.";

    private readonly Mock<ISongSuggestionService> _suggestionsMock = new();

    public SongFormViewModelTests() => SetupSearch([], [], []);

    private static SongSuggestionDto LocalDto(
        int id, string title = "Creep", string artist = "Radiohead", string version = "", int? artistId = 1) =>
        new(id, title, artist, artistId, null, null, false, version, artist);

    private static SongSuggestionDto RemoteDto(
        string title = "Creep", string artist = "Radiohead", string version = "",
        int? localArtistId = null, string? localArtistName = null, string externalId = "dz-1") =>
        new(null, title, artist, localArtistId, externalId, "Deezer", true, version, localArtistName);

    private void SetupSearch(
        IReadOnlyList<SongSuggestionDto> local,
        IReadOnlyList<SongSuggestionDto> promoted,
        IReadOnlyList<SongSuggestionDto> remote,
        RemoteSearchStatus status = RemoteSearchStatus.Ok)
    {
        _suggestionsMock
            .Setup(s => s.SearchTitleLocalAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(local);
        _suggestionsMock
            .Setup(s => s.SearchTitleRemoteAsync(It.IsAny<string>(), It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SongRemoteSearchResult(promoted, remote, status));
    }

    private void SetupRemoteStatus(string query, RemoteSearchStatus status) =>
        _suggestionsMock
            .Setup(s => s.SearchTitleRemoteAsync(query, It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SongRemoteSearchResult([], [], status));

    /// <summary>An add-form VM past hydration (Shell has applied the query properties).</summary>
    private SongFormViewModel CreateAddFormSut(
        Mock<ISongService>? songService = null, Mock<IArtistService>? artistService = null)
    {
        var sut = CreateSut(songService: songService, artistService: artistService);
        sut.CompleteHydration();
        return sut;
    }

    private static Task<IReadOnlyList<AutocompleteSuggestion>> SearchTitle(
        SongFormViewModel sut, string query, CancellationToken ct = default) =>
        sut.TitleOptionSource.SearchAsync(query, ct);

    /// <summary>An option as the Field hands it back to SelectTitleCommand: Data is the SongSuggestionDto.</summary>
    private static AutocompleteSuggestion Pick(SongSuggestionDto dto) =>
        new(dto.Title, string.Empty, dto);

    // [AC] REQ-ART-07/08: a local option shows "artist · version"; the source "Library" is the trailing text.
    [Fact]
    public async Task TitleOptionSource_LocalHit_SupportingTextHasArtistVersionAndLibraryMarker()
    {
        SetupSearch([LocalDto(1, "Creep", "Radiohead", "Pablo Honey")], [], []);
        var sut = CreateAddFormSut();

        var options = await SearchTitle(sut, "creep");

        var option = Assert.Single(options);
        Assert.Equal("Creep", option.Headline);
        Assert.Equal("Radiohead · Pablo Honey", option.SupportingText);
        Assert.Equal("Library", option.TrailingText);
    }

    // [AC] REQ-ART-07/08: a local option with an empty Version omits the version part; the trailing text is "Library".
    [Fact]
    public async Task TitleOptionSource_LocalHitEmptyVersion_OmitsVersionPart()
    {
        SetupSearch([LocalDto(1, "Creep", "Radiohead", "")], [], []);
        var sut = CreateAddFormSut();

        var options = await SearchTitle(sut, "creep");

        var option = Assert.Single(options);
        Assert.Equal("Radiohead", option.SupportingText);
        Assert.Equal("Library", option.TrailingText);
    }

    // [AC] REQ-ART-07/08: a remote option shows "artist · version"; the source "Deezer" is the trailing text.
    [Fact]
    public async Task TitleOptionSource_RemoteHit_SupportingTextIsArtistVersionAndDeezer()
    {
        SetupSearch([], [], [RemoteDto("Creep", "Radiohead", "Live")]);
        var sut = CreateAddFormSut();

        var options = await SearchTitle(sut, "creep");

        var option = Assert.Single(options);
        Assert.Equal("Creep", option.Headline);
        Assert.Equal("Radiohead · Live", option.SupportingText);
        Assert.Equal("Deezer", option.TrailingText);
    }

    // [AC] REQ-ART-07/08: a remote option with an empty Version omits the version part; the trailing text is "Deezer".
    [Fact]
    public async Task TitleOptionSource_RemoteHitEmptyVersion_OmitsVersionPart()
    {
        SetupSearch([], [], [RemoteDto("Creep", "Radiohead", "")]);
        var sut = CreateAddFormSut();

        var options = await SearchTitle(sut, "creep");

        var option = Assert.Single(options);
        Assert.Equal("Radiohead", option.SupportingText);
        Assert.Equal("Deezer", option.TrailingText);
    }

    // [AC] REQ-SFT-05: one row per song; local first, then promoted (local rows), then remote.
    [Fact]
    public async Task TitleOptionSource_OrdersLocalThenPromotedThenRemote()
    {
        SetupSearch(
            [LocalDto(1, "Local One")],
            [LocalDto(2, "Promoted Two")],
            [RemoteDto("Remote Three")]);
        var sut = CreateAddFormSut();

        var options = await SearchTitle(sut, "song");

        Assert.Equal(["Local One", "Promoted Two", "Remote Three"], options.Select(o => o.Headline));
        Assert.Equal(1, Assert.IsType<SongSuggestionDto>(options[0].Data).LocalId);
        Assert.Equal(2, Assert.IsType<SongSuggestionDto>(options[1].Data).LocalId);
        Assert.True(Assert.IsType<SongSuggestionDto>(options[2].Data).IsRemote);
    }

    // [AC] REQ-SFT-09: the remote step receives the shown local ids and the caller's token (latest query wins).
    [Fact]
    public async Task TitleOptionSource_PassesShownLocalIdsAndToken()
    {
        SetupSearch([LocalDto(5), LocalDto(6, "Other")], [], []);
        var sut = CreateAddFormSut();
        using var cts = new CancellationTokenSource();

        await SearchTitle(sut, "creep", cts.Token);

        _suggestionsMock.Verify(s => s.SearchTitleLocalAsync("creep", cts.Token), Times.Once);
        _suggestionsMock.Verify(
            s => s.SearchTitleRemoteAsync("creep", It.Is<IReadOnlyCollection<int>>(ids => ids.SequenceEqual(new[] { 5, 6 })), cts.Token),
            Times.Once);
    }

    // [AC] REQ-SFT-06: Unavailable status shows the unavailable note, exact text (D21).
    [Fact]
    public async Task TitleOptionSource_StatusUnavailable_SetsUnavailableNote()
    {
        SetupSearch([LocalDto(1)], [], [], RemoteSearchStatus.Unavailable);
        var sut = CreateAddFormSut();

        await SearchTitle(sut, "creep");

        Assert.Equal(UnavailableNoteText, sut.TitleSupportingText);
    }

    // [AC] REQ-SFT-53: Offline status shows the offline note, exact text (D21).
    [Fact]
    public async Task TitleOptionSource_StatusOffline_SetsOfflineNote()
    {
        SetupSearch([LocalDto(1)], [], [], RemoteSearchStatus.Offline);
        var sut = CreateAddFormSut();

        await SearchTitle(sut, "creep");

        Assert.Equal(OfflineNoteText, sut.TitleSupportingText);
    }

    // [AC] REQ-SFT-06: a later Ok query clears the offline note (D21).
    [Fact]
    public async Task TitleOptionSource_OfflineThenOk_ClearsNote()
    {
        var sut = CreateAddFormSut();
        SetupSearch([], [], [], RemoteSearchStatus.Offline);
        await SearchTitle(sut, "creep");
        Assert.Equal(OfflineNoteText, sut.TitleSupportingText);

        SetupSearch([], [], [], RemoteSearchStatus.Ok);
        await SearchTitle(sut, "creep");

        Assert.Equal(string.Empty, sut.TitleSupportingText);
    }

    // [AC] REQ-SFT-06: a remote query that succeeds after a failure clears the unavailable note.
    [Fact]
    public async Task TitleOptionSource_RemoteSucceedsAfterFailure_ClearsTitleSupportingText()
    {
        var sut = CreateAddFormSut();
        SetupSearch([], [], [], RemoteSearchStatus.Unavailable);
        await SearchTitle(sut, "creep");
        Assert.Equal(UnavailableNoteText, sut.TitleSupportingText);

        SetupSearch([], [], [RemoteDto()], RemoteSearchStatus.Ok);
        await SearchTitle(sut, "creep");

        Assert.Equal(string.Empty, sut.TitleSupportingText);
    }

    // [AC] REQ-SFT-09: a stale (cancelled) query never touches the note (Review Focus 4).
    [Fact]
    public async Task TitleOptionSource_CancelledQuery_DoesNotChangeTitleSupportingText()
    {
        var sut = CreateAddFormSut();
        SetupSearch([], [], [], RemoteSearchStatus.Offline);
        await SearchTitle(sut, "creep");
        Assert.Equal(OfflineNoteText, sut.TitleSupportingText);

        SetupSearch([], [], [], RemoteSearchStatus.Ok);
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        try { await SearchTitle(sut, "creeps", cts.Token); }
        catch (OperationCanceledException) { /* a stale query may surface as cancellation */ }

        Assert.Equal(OfflineNoteText, sut.TitleSupportingText);
    }

    // [AC] REQ-SFT-06: a query below 3 characters (no remote call ran) leaves the current note as it is.
    [Fact]
    public async Task TitleOptionSource_QueryBelowRemoteThreshold_KeepsExistingNote()
    {
        var sut = CreateAddFormSut();
        SetupRemoteStatus("don", RemoteSearchStatus.Unavailable);
        SetupRemoteStatus("do", RemoteSearchStatus.Ok);   // the service answers Ok, without a call, below 3 characters
        await SearchTitle(sut, "don");
        Assert.Equal(UnavailableNoteText, sut.TitleSupportingText);

        await SearchTitle(sut, "do");

        Assert.Equal(UnavailableNoteText, sut.TitleSupportingText);
    }

    // [AC] REQ-SFT-06: the note is set through the VM's UI-thread helper (observable never set off the UI thread).
    [Fact]
    public async Task TitleOptionSource_SetsNoteThroughUiThreadHelper()
    {
        var provider = new CountingDispatcherProvider();
        Microsoft.Maui.Dispatching.DispatcherProvider.SetCurrent(provider);
        var previous = Microsoft.Maui.Controls.Application.Current;
        Microsoft.Maui.Controls.Application.Current = new Microsoft.Maui.Controls.Application();
        try
        {
            SetupSearch([], [], [], RemoteSearchStatus.Offline);
            var sut = CreateAddFormSut();
            var before = provider.Dispatcher.DispatchCount;

            await SearchTitle(sut, "creep");

            Assert.Equal(OfflineNoteText, sut.TitleSupportingText);
            Assert.Equal(1, provider.Dispatcher.DispatchCount - before);
        }
        finally
        {
            Microsoft.Maui.Controls.Application.Current = previous;
        }
    }

    private sealed class CountingDispatcherProvider : Microsoft.Maui.Dispatching.IDispatcherProvider
    {
        public CountingDispatcher Dispatcher { get; } = new();
        public Microsoft.Maui.Dispatching.IDispatcher GetForCurrentThread() => Dispatcher;
    }

    /// <summary>Reports dispatch as required so every RunOnUiThread call is counted, then runs inline.</summary>
    private sealed class CountingDispatcher : Microsoft.Maui.Dispatching.IDispatcher
    {
        public int DispatchCount { get; private set; }
        public bool IsDispatchRequired => true;
        public bool Dispatch(Action action) { DispatchCount++; action(); return true; }
        public bool DispatchDelayed(TimeSpan delay, Action action) { action(); return true; }
        public Microsoft.Maui.Dispatching.IDispatcherTimer CreateTimer() => throw new NotSupportedException();
    }

    // [AC] REQ-SFT-06: emptying Title by typing clears the note (the Field never queries an empty string).
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SongTitle_EmptiedByTyping_ClearsTitleSupportingText(string emptied)
    {
        var sut = CreateAddFormSut();
        sut.SongTitle = "creep";
        SetupSearch([], [], [], RemoteSearchStatus.Offline);
        await SearchTitle(sut, "creep");
        Assert.Equal(OfflineNoteText, sut.TitleSupportingText);

        sut.SongTitle = emptied;

        Assert.Equal(string.Empty, sut.TitleSupportingText);
    }

    // [AC] REQ-SFT-06: X on Title clears the note.
    [Fact]
    public async Task ClearTitle_EmptiesNote()
    {
        var sut = CreateAddFormSut();
        sut.SongTitle = "creep";
        SetupSearch([], [], [], RemoteSearchStatus.Unavailable);
        await SearchTitle(sut, "creep");
        Assert.Equal(UnavailableNoteText, sut.TitleSupportingText);

        sut.ClearTitleCommand.Execute(null);

        Assert.Equal(string.Empty, sut.TitleSupportingText);
    }

    // [AC] REQ-SFT-14: X on Title clears the pending identity, empties and unlocks Title, keeps Artist.
    [Fact]
    public async Task ClearTitle_AfterRemotePick_ClearsPendingIdentity_KeepsArtist()
    {
        var sut = CreateAddFormSut();
        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto("Creep", "Radiohead", "Live", localArtistId: 7, localArtistName: "Radiohead")));
        Assert.Equal("dz-1", sut.SelectedExternalId);

        sut.ClearTitleCommand.Execute(null);

        Assert.Equal(string.Empty, sut.SelectedExternalId);
        Assert.Equal(string.Empty, sut.SelectedProvider);
        Assert.Equal(string.Empty, sut.SongTitle);
        Assert.False(sut.IsTitleLocked);
        Assert.Equal(7, sut.SelectedArtistId);
        Assert.Equal("Radiohead", sut.ArtistSearchText);
        Assert.True(sut.IsArtistLocked);
    }

    // [AC] REQ-SFT-14: X on Title resets a Version that still holds the pick-written value (Q21).
    [Fact]
    public async Task ClearTitle_AfterRemotePick_ResetsUnchangedPickFilledVersion()
    {
        var sut = CreateAddFormSut();
        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto("Creep", "Radiohead", "Live")));
        Assert.Equal("Live", sut.SongVersion);

        sut.ClearTitleCommand.Execute(null);

        Assert.Equal(string.Empty, sut.SongVersion);
    }

    // [AC] REQ-SFT-14: X on Title keeps a Version the user edited after the pick (Q21).
    [Fact]
    public async Task ClearTitle_AfterRemotePick_KeepsVersionEditedAfterPick()
    {
        var sut = CreateAddFormSut();
        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto("Creep", "Radiohead", "Live")));
        sut.SongVersion = "Live 2011";

        sut.ClearTitleCommand.Execute(null);

        Assert.Equal("Live 2011", sut.SongVersion);
    }

    // [AC] REQ-SFT-10: after X on Title a local pick still navigates to the edit form (Q21).
    [Fact]
    public async Task ClearTitle_AfterRemotePickThenLocalPick_StillNavigates()
    {
        var sut = CreateAddFormSut();
        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto("Creep", "Radiohead", "Live")));
        sut.ClearArtistCommand.Execute(null);   // the pick also filled Artist; only the Version is under test
        sut.ClearTitleCommand.Execute(null);

        await sut.SelectTitleCommand.ExecuteAsync(Pick(LocalDto(42)));

        _navigationMock.Verify(n => n.GoToAsync($"../{Routes.SongForm}?songId=42"), Times.Once);
    }

    // [AC] REQ-SFT-12: a remote pick overwrites Version with the Deezer value, including "" (Q20).
    [Theory]
    [InlineData("Demo", "", "")]
    [InlineData("Demo", "Live", "Live")]
    public async Task SelectTitle_RemoteOption_SetsVersionFromDeezer_OverwritingTypedVersion(
        string typedVersion, string deezerVersion, string expected)
    {
        var sut = CreateAddFormSut();
        sut.SongVersion = typedVersion;

        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto("Creep", "Radiohead", deezerVersion)));

        Assert.Equal(expected, sut.SongVersion);
    }

    // [AC] REQ-SFT-12: a remote pick fills Title, Artist and the pending identity, and writes nothing.
    [Fact]
    public async Task SelectTitle_RemoteOption_FillsTitleAndArtistAndPendingIdentity_WritesNothing()
    {
        var songService = new Mock<ISongService>();
        var artistService = new Mock<IArtistService>();
        var sut = CreateAddFormSut(songService, artistService);

        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto("Creep", "Radiohead", "Live", externalId: "dz-99")));

        Assert.Equal("Creep", sut.SongTitle);
        Assert.True(sut.IsTitleLocked);
        Assert.Equal("Radiohead", sut.ArtistSearchText);
        Assert.True(sut.IsArtistLocked);
        Assert.Equal("dz-99", sut.SelectedExternalId);
        Assert.Equal("Deezer", sut.SelectedProvider);
        // Explicit Never on the song-service writes and lookups; ShouldShowCharacterCounter (fired by
        // setting SongTitle) is incidental UI feedback, not a write, so it is deliberately not verified.
        songService.Verify(s => s.CreateSongAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        songService.Verify(s => s.CreateSongWithUrlsAsync(It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()), Times.Never);
        songService.Verify(s => s.UpdateSongAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        songService.Verify(s => s.DeleteSongsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()), Times.Never);
        songService.Verify(s => s.GetSongByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        songService.Verify(s => s.ExistsByTitleForArtistAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Never);
        songService.Verify(s => s.GetPagedSongsForListAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        artistService.VerifyNoOtherCalls();
        _navigationMock.Verify(n => n.GoToAsync(It.IsAny<string>()), Times.Never);
    }

    // [AC] REQ-SFT-13: a provider artist that is a local artist is picked as that local artist.
    [Fact]
    public async Task SelectTitle_RemoteOptionArtistIsLocal_LocksLocalArtist()
    {
        var sut = CreateAddFormSut();

        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto("Creep", "radiohead", localArtistId: 7, localArtistName: "Radiohead")));

        Assert.Equal(7, sut.SelectedArtistId);
        Assert.Equal("Radiohead", sut.SelectedArtistName);
        Assert.Equal("Radiohead", sut.ArtistSearchText);
        Assert.True(sut.IsArtistLocked);
    }

    // [AC] REQ-SFT-13: a provider artist that is not local is picked by its name with no id (created on Save).
    [Fact]
    public async Task SelectTitle_RemoteOptionArtistNotLocal_LocksTypedArtistWithNoId()
    {
        var sut = CreateAddFormSut();

        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto("Creep", "Radiohead", localArtistId: null, localArtistName: null)));

        Assert.Null(sut.SelectedArtistId);
        Assert.Equal("Radiohead", sut.ArtistSearchText);
        Assert.True(sut.IsArtistLocked);
    }

    // [AC] REQ-SFT-12: a remote pick closes the artist dropdown and empties its suggestions.
    [Fact]
    public async Task RemotePick_ClosesDropdowns()
    {
        var sut = CreateAddFormSut();
        sut.IsArtistDropDownOpen = true;

        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto(localArtistId: 7, localArtistName: "Radiohead")));

        Assert.False(sut.IsArtistDropDownOpen);
        Assert.Empty(sut.ArtistSuggestions);
    }

    // [AC] REQ-SFT-14: X on Artist after a remote pick clears the pending identity and keeps Title and Version (Q21).
    [Fact]
    public async Task ClearArtist_AfterRemotePick_ClearsPendingIdentity_KeepsTitle()
    {
        var sut = CreateAddFormSut();
        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto("Creep", "Radiohead", "Live", localArtistId: 7, localArtistName: "Radiohead")));

        sut.ClearArtistCommand.Execute(null);

        Assert.Equal(string.Empty, sut.SelectedExternalId);
        Assert.Equal(string.Empty, sut.SelectedProvider);
        Assert.Equal("Creep", sut.SongTitle);
        Assert.True(sut.IsTitleLocked);
        Assert.Equal("Live", sut.SongVersion);
        Assert.False(sut.IsArtistLocked);
    }

    // [AC] REQ-SFT-10: a local pick with no other input replaces the add form with the song's edit form.
    [Fact]
    public async Task SelectTitle_LocalOptionNoOtherInput_NavigatesToEditForm()
    {
        var sut = CreateAddFormSut();
        sut.SongTitle = "cre";

        await sut.SelectTitleCommand.ExecuteAsync(Pick(LocalDto(42)));

        _navigationMock.Verify(n => n.GoToAsync($"../{Routes.SongForm}?songId=42"), Times.Once);
    }

    // [AC] REQ-SFT-10: a local pick always navigates, even when Artist, Version or Lyrics hold input (no discard guard).
    [Fact]
    public async Task SelectTitle_LocalOptionWithOtherInput_StillNavigates()
    {
        var sut = CreateAddFormSut();
        sut.SongTitle = "cre";
        sut.ArtistSearchText = "Radio";
        sut.SongVersion = "Live";
        sut.Lyrics = "Some lyrics";

        await sut.SelectTitleCommand.ExecuteAsync(Pick(LocalDto(42)));

        _navigationMock.Verify(n => n.GoToAsync($"../{Routes.SongForm}?songId=42"), Times.Once);
    }

    // [AC] REQ-SFT-16: the Artist option source searches local artists only and never calls Deezer.
    [Fact]
    public async Task ArtistOptionSource_ReturnsLocalArtistsOnly_NoRemoteCall()
    {
        var artistService = new Mock<IArtistService>();
        artistService
            .Setup(s => s.SearchArtistsByNameAsync("queen", It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ArtistListItem(2, "Queen", string.Empty, 3)]);
        var sut = CreateAddFormSut(artistService: artistService);

        var options = await sut.ArtistOptionSource.SearchAsync("queen", CancellationToken.None);

        var option = Assert.Single(options);
        Assert.Equal("Queen", option.Headline);
        Assert.False(option.IsCreateNew);
        _suggestionsMock.Verify(
            s => s.SearchTitleRemoteAsync(It.IsAny<string>(), It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── RemoveUrlAsync (edit mode) ─────────────────────────────────────────

    // [AC] AC-1.5 — URL removal commits to DB immediately (before snackbar is shown)
    [Fact]
    public async Task RemoveUrlAsync_ValidUrl_CommitsDeleteBeforeSnackbar()
    {
        var callOrder = new List<string>();

        var urlService = new Mock<ISongKaraokeUrlService>();
        urlService.Setup(s => s.RemoveUrlAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .Callback<int, string, CancellationToken>((_, _, _) => callOrder.Add("remove"))
                  .ReturnsAsync((true, string.Empty));
        urlService.Setup(s => s.GetUrlsForSongAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync([]);
        var snackbar = new Mock<ISnackbarComponent>();
        snackbar.Setup(s => s.ShowWithUndoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Func<Task>>()))
                .Callback<string, string, Func<Task>>((_, _, _) => callOrder.Add("snackbar"))
                .Returns(Task.CompletedTask);
        var secureStorage = new Mock<ISecureStorageWrapper>();
        secureStorage.Setup(s => s.GetAsync(It.IsAny<string>())).ReturnsAsync((string?)null);

        var sut = CreateSut(urlService: urlService, snackbar: snackbar, secureStorage: secureStorage);
        sut.SongIdRaw = "42";
        await Task.Yield();

        var dto = MakeDto();
        sut.KaraokeUrls.Add(dto);

        await sut.RemoveUrlCommand.ExecuteAsync(dto);

        Assert.Equal(["remove", "snackbar"], callOrder);
    }

    // [AC] AC-1.5 — UNDO re-inserts the URL
    [Fact]
    public async Task RemoveUrlAsync_UndoTapped_ReAddsUrlToList()
    {
        var urlService = new Mock<ISongKaraokeUrlService>();
        urlService.Setup(s => s.RemoveUrlAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((true, string.Empty));
        urlService.Setup(s => s.GetUrlsForSongAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync([]);
        var reAdded = MakeDto();
        urlService.Setup(s => s.AddUrlAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((true, string.Empty, reAdded));
        var snackbar = new Mock<ISnackbarComponent>();
        snackbar.Setup(s => s.ShowWithUndoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Func<Task>>()))
                .Callback<string, string, Func<Task>>((_, _, action) => action().GetAwaiter().GetResult())
                .Returns(Task.CompletedTask);
        var secureStorage = new Mock<ISecureStorageWrapper>();
        secureStorage.Setup(s => s.GetAsync(It.IsAny<string>())).ReturnsAsync((string?)null);

        var sut = CreateSut(urlService: urlService, snackbar: snackbar, secureStorage: secureStorage);
        sut.SongIdRaw = "42";
        await Task.Yield();

        var dto = MakeDto();
        sut.KaraokeUrls.Add(dto);

        await sut.RemoveUrlCommand.ExecuteAsync(dto);

        Assert.Contains(sut.KaraokeUrls, u => u.VideoId == "abc123");
    }

    // [AC] AC-1.5 — Remove fails → show error, list unchanged
    [Fact]
    public async Task RemoveUrlAsync_RemoveFails_ShowsErrorAndKeepsUrl()
    {
        var urlService = new Mock<ISongKaraokeUrlService>();
        urlService.Setup(s => s.RemoveUrlAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((false, "Not found"));
        urlService.Setup(s => s.GetUrlsForSongAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync([]);
        var snackbar = new Mock<ISnackbarComponent>();
        var secureStorage = new Mock<ISecureStorageWrapper>();
        secureStorage.Setup(s => s.GetAsync(It.IsAny<string>())).ReturnsAsync((string?)null);

        var sut = CreateSut(urlService: urlService, snackbar: snackbar, secureStorage: secureStorage);
        sut.SongIdRaw = "42";
        await Task.Yield();

        var dto = MakeDto();
        sut.KaraokeUrls.Add(dto);

        await sut.RemoveUrlCommand.ExecuteAsync(dto);

        snackbar.Verify(s => s.ShowErrorAsync("Not found"), Times.Once);
        Assert.Contains(sut.KaraokeUrls, u => u.VideoId == "abc123");
    }

    // ── Task 5.3: Save maps service outcomes to sheets (REQ-SFT-51) ───────

    private static SongFormSaveOutcome SavedOutcome(int id = 5) =>
        new(SongFormSaveKind.Saved, "Song created", SaveField.None, id, [], false, []);

    private static SongFormSaveOutcome FailedOutcome(SaveField field, string message) =>
        new(SongFormSaveKind.Failed, message, field, null, [], false, []);

    private static SongFormSaveOutcome ArtistChoiceOutcome(string typed, params ArtistMatch[] candidates) =>
        new(SongFormSaveKind.ArtistChoiceNeeded, string.Empty, SaveField.None, null, candidates, false, [])
        {
            TypedName = typed
        };

    private static SongFormSaveOutcome SongChoiceOutcome(bool exact, params SongMatch[] songs) =>
        new(SongFormSaveKind.SongChoiceNeeded, string.Empty, SaveField.None, null, [], exact, songs);

    private static SongMatch SongMatchOf(int id) => new(id, "Creep", string.Empty, "Radiohead", 1.0);

    /// <summary>Records every SaveFromFormAsync request and answers it with <paramref name="decide"/>.</summary>
    private static void SetupSave(
        Mock<ISongService> songService,
        List<SongFormSaveRequest> requests,
        Func<SongFormSaveRequest, SongFormSaveOutcome> decide)
    {
        songService.Setup(s => s.SaveFromFormAsync(It.IsAny<SongFormSaveRequest>(), It.IsAny<CancellationToken>()))
            .Returns((SongFormSaveRequest r, CancellationToken _) =>
            {
                requests.Add(r);
                return Task.FromResult(decide(r));
            });
    }

    /// <summary>Song service with title and version validation passing, so the VM safety net lets Save through.</summary>
    private static Mock<ISongService> SaveService(
        List<SongFormSaveRequest> requests, Func<SongFormSaveRequest, SongFormSaveOutcome> decide)
    {
        var songService = new Mock<ISongService>();
        songService.Setup(s => s.ValidateTitleInput(It.IsAny<string>())).Returns((true, string.Empty));
        songService.Setup(s => s.ValidateVersionInput(It.IsAny<string>(), It.IsAny<bool>())).Returns((true, string.Empty));
        SetupSave(songService, requests, decide);
        return songService;
    }

    private static void FillAddForm(
        SongFormViewModel sut, string title = "Creep", string artist = "Radiohead", string version = "")
    {
        sut.SongTitle = title;
        sut.SongVersion = version;
        sut.ArtistSearchText = artist;
    }

    /// <summary>A user edit of the Artist or Title field (typed text, not a programmatic restore).</summary>
    private static void EditField(SongFormViewModel sut, string field)
    {
        if (field == "artist")
            sut.ArtistSearchText = "Other artist";
        else
            sut.SongTitle = "Other title";
    }

    // [AC] REQ-SFT-51: a Service exception shows the error snackbar and resets IsBusy (old AC-B5 carried over).
    [Fact]
    public async Task SaveCommand_ServiceThrows_ShowsErrorSnackbar()
    {
        var songService = new Mock<ISongService>();
        songService.Setup(s => s.ValidateTitleInput(It.IsAny<string>())).Returns((true, string.Empty));
        songService.Setup(s => s.ValidateVersionInput(It.IsAny<string>(), It.IsAny<bool>())).Returns((true, string.Empty));
        songService.Setup(s => s.SaveFromFormAsync(It.IsAny<SongFormSaveRequest>(), It.IsAny<CancellationToken>()))
                   .ThrowsAsync(new InvalidOperationException("DB context error"));
        var snackbar = new Mock<ISnackbarComponent>();

        var sut = CreateSut(snackbar: snackbar, songService: songService);
        FillAddForm(sut);

        await sut.SaveCommand.ExecuteAsync(null);

        snackbar.Verify(s => s.ShowErrorAsync(It.Is<string>(m => m.Contains("Failed to save"))), Times.Once);
        Assert.False(sut.IsBusy);
    }

    // [AC] REQ-SFT-21: a blank Artist is not blocked by the VM; the Service refuses it with "Artist is required".
    [Fact]
    public async Task SaveCommand_BlankArtist_PassesToServiceAndShowsItsArtistError()
    {
        var requests = new List<SongFormSaveRequest>();
        var songService = SaveService(requests, _ => FailedOutcome(SaveField.Artist, "Artist is required"));
        var sut = CreateSut(songService: songService);
        FillAddForm(sut, artist: "   ");

        await sut.SaveCommand.ExecuteAsync(null);

        var request = Assert.Single(requests);
        Assert.Null(request.ArtistId);
        Assert.True(sut.ArtistHasError);
        Assert.Equal("Artist is required", sut.ArtistErrorText);
    }

    // [AC] REQ-SFT-18: a typed, unpicked Artist is sent by its trimmed name with no ArtistId, and sets no artist error.
    [Fact]
    public async Task SaveCommand_TypedArtist_SendsTrimmedNameWithoutArtistId()
    {
        var requests = new List<SongFormSaveRequest>();
        var songService = SaveService(requests, _ => SavedOutcome());
        var sut = CreateSut(songService: songService);
        FillAddForm(sut, title: "  Creep  ", artist: "  Metalica  ", version: " Live ");

        await sut.SaveCommand.ExecuteAsync(null);

        var request = Assert.Single(requests);
        Assert.Null(request.SongId);
        Assert.Equal("Creep", request.Title);
        Assert.Equal("Live", request.Version);
        Assert.Null(request.ArtistId);
        Assert.Equal("Metalica", request.ArtistText);
        Assert.IsType<ArtistDecision.None>(request.ArtistDecision);
        Assert.Equal(SongDecision.None, request.SongDecision);
        Assert.False(sut.ArtistHasError);
    }

    // [AC] REQ-SFT-25: the picked Deezer identity travels with the save so the song guard can run on it.
    [Fact]
    public async Task SaveCommand_AfterRemotePick_SendsPickedExternalIdentity()
    {
        var requests = new List<SongFormSaveRequest>();
        var songService = SaveService(requests, _ => SavedOutcome());
        var sut = CreateSut(songService: songService);
        sut.SelectTitleCommand.Execute(Pick(RemoteDto(title: "Don't Cry", artist: "Guns N' Roses", externalId: "3135556")));

        await sut.SaveCommand.ExecuteAsync(null);

        var request = Assert.Single(requests);
        Assert.Equal("3135556", request.ExternalId);
        Assert.Equal("Deezer", request.ExternalProvider);
    }

    // [AC] REQ-SFT-46: Save writes only through SaveFromFormAsync; the VM never calls a create or update write.
    [Fact]
    public async Task SaveCommand_NewSong_WritesOnlyThroughSaveFromForm()
    {
        var requests = new List<SongFormSaveRequest>();
        var songService = SaveService(requests, _ => SavedOutcome());
        var sut = CreateSut(songService: songService);
        FillAddForm(sut);

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.Single(requests);
        var writes = songService.Invocations
            .Select(i => i.Method.Name)
            .Where(n => n is "CreateSongAsync" or "CreateSongWithUrlsAsync" or "UpdateSongAsync");
        Assert.Empty(writes);
    }

    // [AC] REQ-SFT-51: add-mode Save calls SaveFromFormAsync once with SongId null and no decisions.
    [Fact]
    public async Task SaveCommand_NewSongNoDecision_CallsSaveFromFormInAddMode()
    {
        var requests = new List<SongFormSaveRequest>();
        var songService = SaveService(requests, _ => SavedOutcome());
        var sut = CreateSut(songService: songService);
        FillAddForm(sut, title: "Test Song", artist: "Artist");

        await sut.SaveCommand.ExecuteAsync(null);

        var request = Assert.Single(requests);
        Assert.Null(request.SongId);
        Assert.Equal("Test Song", request.Title);
        Assert.IsType<ArtistDecision.None>(request.ArtistDecision);
        Assert.Equal(SongDecision.None, request.SongDecision);
    }

    // [AC] REQ-SFT-51: a Saved outcome shows the success snackbar.
    [Fact]
    public async Task SaveCommand_SavedOutcome_ShowsSuccessSnackbar()
    {
        var requests = new List<SongFormSaveRequest>();
        var songService = SaveService(requests, _ => SavedOutcome());
        var snackbar = new Mock<ISnackbarComponent>();
        var sut = CreateSut(snackbar: snackbar, songService: songService);
        FillAddForm(sut);

        await sut.SaveCommand.ExecuteAsync(null);

        snackbar.Verify(s => s.ShowSuccessAsync(It.IsAny<string>()), Times.Once);
    }

    // [AC] REQ-SFT-51: a Saved outcome navigates back through INavigationService (never Shell.Current).
    [Fact]
    public async Task SaveCommand_SavedOutcome_NavigatesBack()
    {
        var requests = new List<SongFormSaveRequest>();
        var songService = SaveService(requests, _ => SavedOutcome());
        var sut = CreateSut(songService: songService);
        FillAddForm(sut);

        await sut.SaveCommand.ExecuteAsync(null);

        _navigationMock.Verify(n => n.GoToAsync(".."), Times.Once);
    }

    // [AC] REQ-SFT-15: a Failed(Title) outcome shows its message on the Title field and does not navigate.
    [Fact]
    public async Task SaveCommand_FailedTitle_ShowsMessageOnTitleField()
    {
        var requests = new List<SongFormSaveRequest>();
        var songService = SaveService(requests, _ => FailedOutcome(SaveField.Title, "Song title is required"));
        var sut = CreateSut(songService: songService);
        FillAddForm(sut);

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.True(sut.TitleHasError);
        Assert.Equal("Song title is required", sut.TitleErrorText);
        _navigationMock.Verify(n => n.GoToAsync(It.IsAny<string>()), Times.Never);
    }

    // [AC] REQ-SFT-22: a Failed(Version) outcome shows its message on the Version field (AC-1.2 text unchanged).
    [Fact]
    public async Task SaveCommand_FailedVersion_ShowsMessageOnVersionField()
    {
        var requests = new List<SongFormSaveRequest>();
        var songService = SaveService(requests,
            _ => FailedOutcome(SaveField.Version, "A version label is required (e.g. Live, Acoustic, Remix)"));
        var sut = CreateSut(songService: songService);
        FillAddForm(sut);

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.True(sut.VersionHasError);
        Assert.Equal("A version label is required (e.g. Live, Acoustic, Remix)", sut.VersionErrorText);
    }

    // [AC] REQ-SFT-20: a Failed(Artist) outcome (created-artist validation) shows the service message on Artist.
    [Fact]
    public async Task SaveCommand_FailedArtist_ShowsServiceMessageOnArtistField()
    {
        var requests = new List<SongFormSaveRequest>();
        var songService = SaveService(requests,
            _ => FailedOutcome(SaveField.Artist, "Artist name must be 60 characters or fewer"));
        var sut = CreateSut(songService: songService);
        FillAddForm(sut, artist: "Metalica");

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.True(sut.ArtistHasError);
        Assert.Equal("Artist name must be 60 characters or fewer", sut.ArtistErrorText);
    }

    // [AC] REQ-SFT-26: a Failed(None) outcome (write refusal) shows its message as a general error and sets no field error.
    [Fact]
    public async Task SaveCommand_FailedNoField_ShowsGeneralErrorAndNoFieldError()
    {
        var requests = new List<SongFormSaveRequest>();
        var songService = SaveService(requests,
            _ => FailedOutcome(SaveField.None, "This music-database song is already in your library"));
        var snackbar = new Mock<ISnackbarComponent>();
        var sut = CreateSut(snackbar: snackbar, songService: songService);
        FillAddForm(sut);

        await sut.SaveCommand.ExecuteAsync(null);

        snackbar.Verify(s => s.ShowErrorAsync("This music-database song is already in your library"), Times.Once);
        Assert.False(sut.TitleHasError);
        Assert.False(sut.VersionHasError);
        Assert.False(sut.ArtistHasError);
    }

    // [AC] REQ-SFT-19: ArtistChoiceNeeded opens the artist-match sheet with the candidates best-first and saves nothing.
    [Fact]
    public async Task SaveCommand_ArtistChoiceNeeded_OpensArtistSheetWithCandidatesInOrder()
    {
        var requests = new List<SongFormSaveRequest>();
        var songService = SaveService(requests, _ => ArtistChoiceOutcome("Metalica",
            new ArtistMatch(3, "Metallica", 0.94),
            new ArtistMatch(8, "Metalic", 0.81),
            new ArtistMatch(11, "Metalika", 0.78)));
        var sut = CreateSut(songService: songService);
        FillAddForm(sut, artist: "Metalica");

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.True(sut.IsArtistMatchSheetVisible);
        Assert.False(sut.IsResolutionSheetVisible);
        Assert.Equal(new[] { 3, 8, 11 }, sut.ArtistMatchCandidates.Select(c => c.ArtistId));
        _navigationMock.Verify(n => n.GoToAsync(It.IsAny<string>()), Times.Never);
    }

    // [AC] REQ-SFT-19: the artist sheet exposes the outcome's TypedName for the "Create '<typed>'" action.
    [Fact]
    public async Task SaveCommand_ArtistChoiceNeeded_ExposesOutcomeTypedNameForCreateAction()
    {
        var requests = new List<SongFormSaveRequest>();
        var songService = SaveService(requests,
            _ => ArtistChoiceOutcome("Metalica", new ArtistMatch(3, "Metallica", 0.94)));
        var sut = CreateSut(songService: songService);
        FillAddForm(sut, artist: "  Metalica ");

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.Equal("Metalica", sut.ArtistMatchTypedName);
    }

    // [AC] REQ-SFT-22: an exact duplicate opens the resolution sheet as the duplicate sheet and saves nothing.
    [Fact]
    public async Task SaveCommand_ExactSongChoice_OpensResolutionSheetAsExactDuplicate()
    {
        var requests = new List<SongFormSaveRequest>();
        var songService = SaveService(requests, _ => SongChoiceOutcome(true, SongMatchOf(9)));
        var sut = CreateSut(songService: songService);
        FillAddForm(sut);

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.True(sut.IsResolutionSheetVisible);
        Assert.True(sut.IsExactDuplicate);
        Assert.Equal(9, Assert.Single(sut.ResolutionCandidates).SongId);
        Assert.False(sut.IsArtistMatchSheetVisible);
    }

    // [AC] REQ-SFT-23: similar titles open the resolution sheet as possible duplicates, not as an exact duplicate.
    [Fact]
    public async Task SaveCommand_FuzzySongChoice_OpensResolutionSheetAsPossibleDuplicates()
    {
        var requests = new List<SongFormSaveRequest>();
        var songService = SaveService(requests,
            _ => SongChoiceOutcome(false, SongMatchOf(9), SongMatchOf(10)));
        var sut = CreateSut(songService: songService);
        FillAddForm(sut);

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.True(sut.IsResolutionSheetVisible);
        Assert.False(sut.IsExactDuplicate);
        Assert.Equal(2, sut.ResolutionCandidates.Count);
    }

    // [AC] REQ-SFT-19: "Use '<existing>'" re-invokes SaveFromFormAsync with UseExisting(id) and closes the sheet.
    [Fact]
    public async Task UseExistingArtistCommand_ReInvokesSaveWithUseExistingDecision()
    {
        var requests = new List<SongFormSaveRequest>();
        var songService = SaveService(requests, r => r.ArtistDecision is ArtistDecision.None
            ? ArtistChoiceOutcome("Metalica", new ArtistMatch(3, "Metallica", 0.94))
            : SavedOutcome());
        var sut = CreateSut(songService: songService);
        FillAddForm(sut, artist: "Metalica");
        await sut.SaveCommand.ExecuteAsync(null);

        await sut.UseExistingArtistCommand.ExecuteAsync(new ArtistMatch(3, "Metallica", 0.94));

        Assert.Equal(2, requests.Count);
        Assert.Equal(new ArtistDecision.UseExisting(3), requests[1].ArtistDecision);
        Assert.False(sut.IsArtistMatchSheetVisible);
    }

    // [AC] REQ-SFT-19: "Create '<typed>'" re-invokes SaveFromFormAsync with CreateTyped and closes the sheet.
    [Fact]
    public async Task CreateTypedArtistCommand_ReInvokesSaveWithCreateTypedDecision()
    {
        var requests = new List<SongFormSaveRequest>();
        var songService = SaveService(requests, r => r.ArtistDecision is ArtistDecision.None
            ? ArtistChoiceOutcome("Metalica", new ArtistMatch(3, "Metallica", 0.94))
            : SavedOutcome());
        var sut = CreateSut(songService: songService);
        FillAddForm(sut, artist: "Metalica");
        await sut.SaveCommand.ExecuteAsync(null);

        await sut.CreateTypedArtistCommand.ExecuteAsync(null);

        Assert.Equal(2, requests.Count);
        Assert.IsType<ArtistDecision.CreateTyped>(requests[1].ArtistDecision);
        Assert.False(sut.IsArtistMatchSheetVisible);
    }

    // [AC] REQ-SFT-19: Back or scrim on the artist sheet saves nothing and navigates nowhere.
    [Fact]
    public async Task DismissArtistMatchSheetCommand_SavesNothingAndHidesSheet()
    {
        var requests = new List<SongFormSaveRequest>();
        var songService = SaveService(requests,
            _ => ArtistChoiceOutcome("Metalica", new ArtistMatch(3, "Metallica", 0.94)));
        var sut = CreateSut(songService: songService);
        FillAddForm(sut, artist: "Metalica");
        await sut.SaveCommand.ExecuteAsync(null);

        sut.DismissArtistMatchSheetCommand.Execute(null);

        Assert.False(sut.IsArtistMatchSheetVisible);
        Assert.Single(requests);
        _navigationMock.Verify(n => n.GoToAsync(It.IsAny<string>()), Times.Never);
    }

    // [AC] REQ-SFT-22: "Save as new version" re-invokes SaveFromFormAsync with SaveAsNewVersion (the Service checks the Version).
    [Fact]
    public async Task ConfirmSaveAsNewVersionCommand_ReInvokesSaveWithSaveAsNewVersionDecision()
    {
        var requests = new List<SongFormSaveRequest>();
        var songService = SaveService(requests, r => r.SongDecision == SongDecision.None
            ? SongChoiceOutcome(true, SongMatchOf(9))
            : SavedOutcome());
        var sut = CreateSut(songService: songService);
        FillAddForm(sut, version: "Live");
        await sut.SaveCommand.ExecuteAsync(null);

        await sut.ConfirmSaveAsNewVersionCommand.ExecuteAsync(null);

        Assert.Equal(2, requests.Count);
        Assert.Equal(SongDecision.SaveAsNewVersion, requests[1].SongDecision);
        Assert.False(sut.IsResolutionSheetVisible);
    }

    // [AC] REQ-SFT-23: "Save as new song" re-invokes SaveFromFormAsync with SaveAsNewSong; no Version is required.
    [Fact]
    public async Task ConfirmSaveAsNewSongCommand_ReInvokesSaveWithSaveAsNewSongDecisionWithoutVersion()
    {
        var requests = new List<SongFormSaveRequest>();
        var songService = SaveService(requests, r => r.SongDecision == SongDecision.None
            ? SongChoiceOutcome(false, SongMatchOf(9))
            : SavedOutcome());
        var sut = CreateSut(songService: songService);
        FillAddForm(sut, version: string.Empty);
        await sut.SaveCommand.ExecuteAsync(null);

        await sut.ConfirmSaveAsNewSongCommand.ExecuteAsync(null);

        Assert.Equal(2, requests.Count);
        Assert.Equal(SongDecision.SaveAsNewSong, requests[1].SongDecision);
        Assert.False(sut.IsResolutionSheetVisible);
    }

    // [AC] REQ-SFT-22: Back or scrim on the duplicate sheet saves nothing and hides the sheet.
    [Fact]
    public async Task DismissResolutionSheetCommand_AfterExactSongChoice_SavesNothing()
    {
        var requests = new List<SongFormSaveRequest>();
        var songService = SaveService(requests, _ => SongChoiceOutcome(true, SongMatchOf(9)));
        var sut = CreateSut(songService: songService);
        FillAddForm(sut);
        await sut.SaveCommand.ExecuteAsync(null);

        sut.DismissResolutionSheetCommand.Execute(null);

        Assert.False(sut.IsResolutionSheetVisible);
        Assert.Single(requests);
    }

    // [AC] REQ-SFT-54: within one Save attempt the VM keeps both decisions and sends them on every re-invocation.
    [Fact]
    public async Task Decisions_PersistAcrossReinvocations_WithinOneSaveAttempt()
    {
        var requests = new List<SongFormSaveRequest>();
        var songService = SaveService(requests, r => r.ArtistDecision is ArtistDecision.None
            ? ArtistChoiceOutcome("Metalica", new ArtistMatch(3, "Metallica", 0.94))
            : r.SongDecision == SongDecision.None
                ? SongChoiceOutcome(true, SongMatchOf(9))
                : SavedOutcome());
        var sut = CreateSut(songService: songService);
        FillAddForm(sut, artist: "Metalica", version: "Live");
        await sut.SaveCommand.ExecuteAsync(null);
        await sut.UseExistingArtistCommand.ExecuteAsync(new ArtistMatch(3, "Metallica", 0.94));
        await sut.ConfirmSaveAsNewVersionCommand.ExecuteAsync(null);

        Assert.Equal(3, requests.Count);
        Assert.Equal(new ArtistDecision.UseExisting(3), requests[2].ArtistDecision);
        Assert.Equal(SongDecision.SaveAsNewVersion, requests[2].SongDecision);
    }

    // [AC] REQ-SFT-54: a user edit of Artist or Title clears the artist decision before the next re-invocation.
    [Theory]
    [InlineData("artist")]
    [InlineData("title")]
    public async Task UserEdit_ClearsArtistDecision(string edit)
    {
        var requests = new List<SongFormSaveRequest>();
        var songService = SaveService(requests, r => r.ArtistDecision is ArtistDecision.None
            ? ArtistChoiceOutcome("Metalica", new ArtistMatch(3, "Metallica", 0.94))
            : r.SongDecision == SongDecision.None
                ? SongChoiceOutcome(true, SongMatchOf(9))
                : SavedOutcome());
        var sut = CreateSut(songService: songService);
        FillAddForm(sut, artist: "Metalica");
        await sut.SaveCommand.ExecuteAsync(null);
        await sut.UseExistingArtistCommand.ExecuteAsync(new ArtistMatch(3, "Metallica", 0.94));

        EditField(sut, edit);
        await sut.ConfirmSaveAsNewSongCommand.ExecuteAsync(null);

        Assert.Equal(3, requests.Count);
        Assert.IsType<ArtistDecision.None>(requests[2].ArtistDecision);
        Assert.Equal(SongDecision.SaveAsNewSong, requests[2].SongDecision);
    }

    // [AC] REQ-SFT-54: a user edit of Artist or Title clears the song decision before the next re-invocation.
    [Theory]
    [InlineData("artist")]
    [InlineData("title")]
    public async Task UserEdit_ClearsSongDecision(string edit)
    {
        var requests = new List<SongFormSaveRequest>();
        var songService = SaveService(requests, r => r.SongDecision == SongDecision.None
            ? SongChoiceOutcome(false, SongMatchOf(9))
            : r.ArtistDecision is ArtistDecision.None
                ? ArtistChoiceOutcome("Metalica", new ArtistMatch(3, "Metallica", 0.94))
                : SavedOutcome());
        var sut = CreateSut(songService: songService);
        FillAddForm(sut, artist: "Metalica");
        await sut.SaveCommand.ExecuteAsync(null);
        await sut.ConfirmSaveAsNewSongCommand.ExecuteAsync(null);

        EditField(sut, edit);
        await sut.UseExistingArtistCommand.ExecuteAsync(new ArtistMatch(3, "Metallica", 0.94));

        Assert.Equal(3, requests.Count);
        Assert.Equal(SongDecision.None, requests[2].SongDecision);
        Assert.Equal(new ArtistDecision.UseExisting(3), requests[2].ArtistDecision);
    }

    // [AC] REQ-SFT-54: a new Save attempt (the user's Save tap) starts with no decisions.
    [Fact]
    public async Task SaveCommand_NewAttempt_StartsWithNoDecisions()
    {
        var requests = new List<SongFormSaveRequest>();
        var songService = SaveService(requests, r => r.ArtistDecision is ArtistDecision.None
            ? ArtistChoiceOutcome("Metalica", new ArtistMatch(3, "Metallica", 0.94))
            : r.SongDecision == SongDecision.None
                ? SongChoiceOutcome(true, SongMatchOf(9))
                : SavedOutcome());
        var sut = CreateSut(songService: songService);
        FillAddForm(sut, artist: "Metalica");
        await sut.SaveCommand.ExecuteAsync(null);
        await sut.UseExistingArtistCommand.ExecuteAsync(new ArtistMatch(3, "Metallica", 0.94));

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.Equal(3, requests.Count);
        Assert.IsType<ArtistDecision.None>(requests[2].ArtistDecision);
        Assert.Equal(SongDecision.None, requests[2].SongDecision);
    }

    // ── BUG-008: artist blur-clear ─────────────────────────────────────────

    // [AC] REQ-SFT-17: when Artist loses focus holding unpicked text, the text stays and no error is shown.
    [Fact]
    public void ArtistBlurredWithoutSelection_NoPriorSelection_RetainsTextAndSetsNoError()
    {
        var sut = CreateSut();
        sut.ArtistSearchText = "partial text";
        // SelectedArtistId not set

        sut.ArtistBlurredWithoutSelectionCommand.Execute(null);

        Assert.Equal("partial text", sut.ArtistSearchText);
        Assert.Null(sut.SelectedArtistId);
        Assert.False(sut.ArtistHasError);
        Assert.True(string.IsNullOrEmpty(sut.ArtistErrorText));
        Assert.False(sut.IsArtistLocked);
    }

    // [AC] REQ-ACREATE-03: empty blur raises no error
    [Fact]
    public void ArtistBlurredWithoutSelection_EmptyText_NoErrorNoLock()
    {
        var sut = CreateSut();
        sut.ArtistSearchText = string.Empty;
        // SelectedArtistId not set

        sut.ArtistBlurredWithoutSelectionCommand.Execute(null);

        Assert.False(sut.ArtistHasError);
        Assert.False(sut.IsArtistLocked);
    }

    // [AC] AC-B8-02 — blur with a prior selection restores the artist name
    [Fact]
    public void ArtistBlurredWithoutSelection_WithPriorSelection_RestoresName()
    {
        var sut = CreateSut();
        sut.SelectedArtistId = 7;
        sut.SelectedArtistName = "Guns N' Roses";
        sut.ArtistSearchText = "partial re-type";

        sut.ArtistBlurredWithoutSelectionCommand.Execute(null);

        Assert.Equal("Guns N' Roses", sut.ArtistSearchText);
        Assert.Equal(7, sut.SelectedArtistId);
    }

    // ── BUG-060 / REQ-ACREATE-15: clearing a locked artist unlocks the field ──────────────

    // [AC] REQ-ACREATE-15: tapping the clear (X) icon on a locked field unlocks it and drops
    // the selection so the field returns to a normal searchable state.
    [Fact]
    public void ClearArtist_WhenLocked_UnlocksAndClearsSelection()
    {
        var sut = CreateSut();
        sut.SelectedArtistId = 7;
        sut.SelectedArtistName = "Guns N' Roses";
        sut.ArtistSearchText = "Guns N' Roses";
        sut.IsArtistLocked = true;

        sut.ClearArtistCommand.Execute(null);

        Assert.False(sut.IsArtistLocked);
        Assert.Null(sut.SelectedArtistId);
        Assert.Null(sut.SelectedArtistName);
        Assert.Equal(string.Empty, sut.ArtistSearchText);
    }

    // [AC] REQ-ACREATE-15 (BUG-065): the clear (X) icon also sets ArtistSearchText programmatically
    // (to empty) and must close the suggestion dropdown, same as every other programmatic assignment.
    [Fact]
    public void ClearArtist_ClosesDropdown()
    {
        var sut = CreateSut();
        sut.SelectedArtistId = 7;
        sut.SelectedArtistName = "Guns N' Roses";
        sut.ArtistSearchText = "Guns N' Roses";
        sut.IsArtistLocked = true;
        sut.IsArtistDropDownOpen = true; // simulate the popup being open before the clear

        sut.ClearArtistCommand.Execute(null);

        Assert.False(sut.IsArtistDropDownOpen);
    }

    // [AC] REQ-ACREATE-15: a deliberate clear must not be silently overwritten by the
    // restore-prior-selection branch on the next blur.
    [Fact]
    public void ArtistBlurredWithoutSelection_AfterDeliberateClear_DoesNotRestorePriorArtist()
    {
        var sut = CreateSut();
        sut.SelectedArtistId = 7;
        sut.SelectedArtistName = "Guns N' Roses";
        sut.ArtistSearchText = "Guns N' Roses";
        sut.IsArtistLocked = true;

        sut.ClearArtistCommand.Execute(null);
        sut.ArtistBlurredWithoutSelectionCommand.Execute(null);

        Assert.Null(sut.SelectedArtistId);
        Assert.Equal(string.Empty, sut.ArtistSearchText);
        Assert.False(sut.ArtistHasError); // empty text is not an "unmatched" state (REQ-ACREATE-03)
    }

    // [AC] AC-B8-03 — InitializeArtistField populates field from query props
    [Fact]
    public void InitializeArtistField_WithArtistId_SetsSearchTextAndSelectedId()
    {
        var sut = CreateSut();
        sut.ArtistIdRaw = "5";
        sut.ArtistName = "Metallica";

        sut.InitializeArtistField();

        Assert.Equal("Metallica", sut.ArtistSearchText);
        Assert.Equal(5, sut.SelectedArtistId);
    }

    // ── BUG-050: selecting a suggestion locks the field ───────────────────

    // [AC] REQ-ACREATE-12 (BUG-050): selecting a suggestion locks the field
    [Fact]
    public void SelectArtist_ExistingSuggestion_LocksField()
    {
        var sut = CreateSut();
        var artist = new ArtistListItem(7, "Queen", string.Empty, 0);
        var suggestion = new AutocompleteSuggestion("Queen", artist.CatalogCountText, artist);
        Assert.False(sut.IsArtistLocked); // precondition

        sut.SelectArtistCommand.Execute(suggestion);

        Assert.True(sut.IsArtistLocked);
        Assert.Equal(7, sut.SelectedArtistId);
    }

    // ── BUG-065/066: dismiss the dropdown on every PROGRAMMATIC ArtistSearchText set ──────
    // Rewritten 2026-08-02 (Helder decision D1) — the removed _suppressNextArtistSearch flag was
    // proven inert by IL evidence (DevExpress's own AsyncItemsSourceProvider.OnEditorTextChanged
    // already ignores non-UserInput text changes; the flag was consumed by the user's NEXT real
    // keystroke instead, causing BUG-065(b)/BUG-066). The real mechanism is the two-way bound
    // IsArtistDropDownOpen property (SongFormPage.xaml: AutoCompleteEdit.IsDropDownOpen) — every
    // site that sets ArtistSearchText programmatically also sets IsArtistDropDownOpen = false.

    // [AC] REQ-ACREATE-12 (BUG-050/065): selecting a suggestion sets ArtistSearchText
    // programmatically and must close the suggestion dropdown.
    [Fact]
    public void SelectArtist_ExistingSuggestion_ClosesDropdown()
    {
        var sut = CreateSut();
        var artist = new ArtistListItem(7, "Queen", string.Empty, 0);
        var suggestion = new AutocompleteSuggestion("Queen", artist.CatalogCountText, artist);
        sut.IsArtistDropDownOpen = true; // simulate the popup being open before the select

        sut.SelectArtistCommand.Execute(suggestion);

        Assert.False(sut.IsArtistDropDownOpen);
    }

    // [AC] REQ-ACREATE-14 (BUG-052/065): edit-mode field hydration (InitializeArtistField) also
    // sets ArtistSearchText programmatically and must close the suggestion dropdown.
    [Fact]
    public void InitializeArtistField_WithArtistId_ClosesDropdown()
    {
        var sut = CreateSut();
        sut.ArtistIdRaw = "5";
        sut.ArtistName = "Metallica";
        sut.IsArtistDropDownOpen = true; // simulate the popup being open before hydration

        sut.InitializeArtistField();

        Assert.False(sut.IsArtistDropDownOpen);
    }

    // [AC] REQ-ACREATE-01 (BUG-065): a fresh ViewModel with no programmatic text assignment must
    // not have forced the dropdown closed (default state, untouched).
    [Fact]
    public void IsArtistDropDownOpen_NoProgrammaticSet_DefaultsFalse()
    {
        var sut = CreateSut();

        Assert.False(sut.IsArtistDropDownOpen);
    }

    // [AC] REQ-ACREATE-03 (BUG-065): blurring without a new selection restores the prior artist
    // name into ArtistSearchText programmatically — this restore must also close the dropdown.
    [Fact]
    public void ArtistBlurredWithoutSelection_RestoresPriorSelection_ClosesDropdown()
    {
        var sut = CreateSut();
        sut.SelectedArtistId = 7;
        sut.SelectedArtistName = "Guns N' Roses";
        sut.ArtistSearchText = "Guns N' Rose"; // user typed-then-blurred without selecting
        sut.IsArtistDropDownOpen = true; // simulate the popup being open before blur

        sut.ArtistBlurredWithoutSelectionCommand.Execute(null);

        Assert.Equal("Guns N' Roses", sut.ArtistSearchText);
        Assert.False(sut.IsArtistDropDownOpen);
    }


    // ── BUG-009: buffered URLs ────────────────────────────────────────────

    // [AC] AC-6.1 — URL buffered in new-song mode (no SongId) without error
    [Fact]
    public async Task AddFromPasteAsync_NewSongMode_BuffersUrlNoError()
    {
        var urlService = new Mock<ISongKaraokeUrlService>();
        urlService.Setup(s => s.ExtractVideoId(It.IsAny<string>())).Returns("vid001");

        var sut = CreateSut(urlService: urlService);
        // No SongId set
        sut.PasteUrlInput = "https://youtu.be/vid001";

        await sut.AddFromPasteCommand.ExecuteAsync(null);

        Assert.False(sut.HasPasteUrlError);
        Assert.Single(sut.KaraokeUrls);
        Assert.Equal("vid001", sut.KaraokeUrls[0].VideoId);
    }

    // [AC] AC-6.1 — Buffered URL count is correct
    [Fact]
    public async Task AddFromPasteAsync_NewSongMode_TwoUrls_BothBuffered()
    {
        var urlService = new Mock<ISongKaraokeUrlService>();
        urlService.SetupSequence(s => s.ExtractVideoId(It.IsAny<string>()))
                  .Returns("vidA")
                  .Returns("vidB");

        var sut = CreateSut(urlService: urlService);

        sut.PasteUrlInput = "https://youtu.be/vidA";
        await sut.AddFromPasteCommand.ExecuteAsync(null);

        sut.PasteUrlInput = "https://youtu.be/vidB";
        await sut.AddFromPasteCommand.ExecuteAsync(null);

        Assert.Equal(2, sut.KaraokeUrls.Count);
    }

    // [AC] AC-6.1 — duplicate URL in new-song mode shows error
    [Fact]
    public async Task AddFromPasteAsync_NewSongMode_DuplicateUrl_ShowsError()
    {
        var urlService = new Mock<ISongKaraokeUrlService>();
        urlService.Setup(s => s.ExtractVideoId(It.IsAny<string>())).Returns("vid001");

        var sut = CreateSut(urlService: urlService);

        sut.PasteUrlInput = "https://youtu.be/vid001";
        await sut.AddFromPasteCommand.ExecuteAsync(null);

        sut.PasteUrlInput = "https://youtu.be/vid001"; // same URL
        await sut.AddFromPasteCommand.ExecuteAsync(null);

        Assert.True(sut.HasPasteUrlError);
        Assert.Single(sut.KaraokeUrls); // still 1, not 2
    }

    // [AC] AC-6.1 — invalid URL in new-song mode shows parse error
    [Fact]
    public async Task AddFromPasteAsync_NewSongMode_InvalidUrl_ShowsParseError()
    {
        var urlService = new Mock<ISongKaraokeUrlService>();
        urlService.Setup(s => s.ExtractVideoId(It.IsAny<string>())).Returns((string?)null);

        var sut = CreateSut(urlService: urlService);
        sut.PasteUrlInput = "not-a-youtube-url";

        await sut.AddFromPasteCommand.ExecuteAsync(null);

        Assert.True(sut.HasPasteUrlError);
        Assert.Empty(sut.KaraokeUrls);
    }

    // [AC] BUG-009 AC-4 — remove pending URL in new-song mode (no DB call)
    [Fact]
    public async Task RemoveUrlAsync_NewSongMode_RemovesFromBufferNoDbCall()
    {
        var urlService = new Mock<ISongKaraokeUrlService>();
        urlService.Setup(s => s.ExtractVideoId(It.IsAny<string>())).Returns("vid001");

        var sut = CreateSut(urlService: urlService);
        sut.PasteUrlInput = "https://youtu.be/vid001";
        await sut.AddFromPasteCommand.ExecuteAsync(null);

        Assert.Single(sut.KaraokeUrls);
        var dto = sut.KaraokeUrls[0];

        await sut.RemoveUrlCommand.ExecuteAsync(dto);

        Assert.Empty(sut.KaraokeUrls);
        urlService.Verify(s => s.RemoveUrlAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── BUG-020: SongFormPage FAB crash — SecureStorage failure in OnAppearing ────

    // [AC] BUG-020: SecureStorage.GetAsync failure (e.g. corrupted Android keystore) must not
    // crash the app when SongFormPage.OnAppearing (async void) awaits RefreshApiKeyFlagAsync.
    [Fact]
    public async Task RefreshApiKeyFlagAsync_SecureStorageThrows_DoesNotThrowAndSetsFalse()
    {
        var secureStorage = new Mock<ISecureStorageWrapper>();
        secureStorage.Setup(s => s.GetAsync(It.IsAny<string>()))
                     .ThrowsAsync(new InvalidOperationException("Keystore error"));

        var sut = CreateSut(secureStorage: secureStorage);

        var exception = await Record.ExceptionAsync(() => sut.RefreshApiKeyFlagAsync());

        Assert.Null(exception);
        Assert.False(sut.HasYouTubeApiKey);
    }

    // ── BUG-024: edit-mode hydration + full-data save (regression) ────────

    private static Mock<ISongService> MakeSongServiceWithSong(Song song)
    {
        var songService = new Mock<ISongService>();
        songService.Setup(s => s.GetSongByIdAsync(song.Id, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(song);
        songService.Setup(s => s.ValidateTitleInput(It.IsAny<string>()))
                   .Returns((true, string.Empty));
        songService.Setup(s => s.ValidateVersionInput(It.IsAny<string>(), It.IsAny<bool>()))
                   .Returns((true, string.Empty));
        return songService;
    }

    [Fact]
    // [AC] BUG-024: navigating to edit mode must hydrate FeaturedArtists, Lyrics and Version
    // from the stored entity — before the fix only title/artist/URLs were loaded, so Save
    // silently wiped these fields.
    public async Task LoadSongForEdit_ExistingSong_HydratesFeaturedArtistsLyricsAndVersion()
    {
        var song = new Song
        {
            Id = 42,
            ArtistId = 1,
            Title = "Stored Title",
            Version = "Live",
            FeaturedArtists = "Feat A",
            Lyrics = "Stored lyrics"
        };
        var songService = MakeSongServiceWithSong(song);
        var secureStorage = new Mock<ISecureStorageWrapper>();
        secureStorage.Setup(s => s.GetAsync(It.IsAny<string>())).ReturnsAsync((string?)null);
        var urlService = new Mock<ISongKaraokeUrlService>();
        urlService.Setup(s => s.GetUrlsForSongAsync(42, It.IsAny<CancellationToken>()))
                  .ReturnsAsync([]);

        var sut = CreateSut(urlService: urlService, secureStorage: secureStorage, songService: songService);
        sut.SongIdRaw = "42";
        await Task.Yield();

        Assert.Equal("Feat A", sut.FeaturedArtists);
        Assert.Equal("Stored lyrics", sut.Lyrics);
        Assert.Equal("Live", sut.SongVersion);
    }

    [Fact]
    // [AC] REQ-SFT-28: the stored artist loads in the picked state
    public async Task LoadSongForEdit_ApiImportedSong_LocksArtistField()
    {
        var song = new Song
        {
            Id = 42,
            ArtistId = 1,
            Title = "Stored Title",
            ExternalId = "dz-99",
            ExternalProvider = "Deezer"
        };
        var songService = MakeSongServiceWithSong(song);
        var secureStorage = new Mock<ISecureStorageWrapper>();
        secureStorage.Setup(s => s.GetAsync(It.IsAny<string>())).ReturnsAsync((string?)null);
        var urlService = new Mock<ISongKaraokeUrlService>();
        urlService.Setup(s => s.GetUrlsForSongAsync(42, It.IsAny<CancellationToken>()))
                  .ReturnsAsync([]);

        var sut = CreateSut(urlService: urlService, secureStorage: secureStorage, songService: songService);
        sut.SongIdRaw = "42";
        await Task.Yield();

        Assert.True(sut.IsArtistLocked);
    }

    [Fact]
    // [AC] REQ-SFT-51: edit-mode Save hands the complete form to SaveFromFormAsync with the song id: hydrated
    // FeaturedArtists and Lyrics plus the user's edited Version (BUG-024 carried over).
    public async Task SaveAsync_EditMode_SendsSongIdAndHydratedFieldsAndEditedVersion()
    {
        var song = new Song
        {
            Id = 42,
            ArtistId = 1,
            Title = "Stored Title",
            Version = "Live",
            FeaturedArtists = "Feat A",
            Lyrics = "Stored lyrics"
        };
        var requests = new List<SongFormSaveRequest>();
        var songService = MakeSongServiceWithSong(song);
        SetupSave(songService, requests, _ => SavedOutcome(42));
        var secureStorage = new Mock<ISecureStorageWrapper>();
        secureStorage.Setup(s => s.GetAsync(It.IsAny<string>())).ReturnsAsync((string?)null);
        var urlService = new Mock<ISongKaraokeUrlService>();
        urlService.Setup(s => s.GetUrlsForSongAsync(42, It.IsAny<CancellationToken>()))
                  .ReturnsAsync([]);

        var sut = CreateSut(urlService: urlService, secureStorage: secureStorage, songService: songService);
        sut.SongIdRaw = "42";
        await Task.Yield();
        sut.CompleteHydration();
        sut.SelectedArtistId = 1;
        sut.SelectedArtistName = "Artist";
        sut.SongVersion = "Acoustic"; // user edits the version label

        await sut.SaveCommand.ExecuteAsync(null);

        var request = Assert.Single(requests);
        Assert.Equal(42, request.SongId);
        Assert.Equal("Stored Title", request.Title);
        Assert.Equal("Feat A", request.FeaturedArtists);
        Assert.Equal("Stored lyrics", request.Lyrics);
        Assert.Equal("Acoustic", request.Version);
    }

    [Fact]
    // [AC] REQ-SFT-28: after X and a new pick in edit mode, Save sends the new artist id to SaveFromFormAsync
    // (REQ-ACREATE-16 carried over).
    public async Task SaveAsync_EditMode_ArtistChanged_SendsNewArtistIdToService()
    {
        var song = new Song
        {
            Id = 42,
            ArtistId = 7,
            Title = "Stored Title",
            OriginalArtist = new Artist { Id = 7, Name = "Queen" }
        };
        var requests = new List<SongFormSaveRequest>();
        var songService = MakeSongServiceWithSong(song);
        SetupSave(songService, requests, _ => SavedOutcome(42));
        var secureStorage = new Mock<ISecureStorageWrapper>();
        secureStorage.Setup(s => s.GetAsync(It.IsAny<string>())).ReturnsAsync((string?)null);
        var urlService = new Mock<ISongKaraokeUrlService>();
        urlService.Setup(s => s.GetUrlsForSongAsync(42, It.IsAny<CancellationToken>()))
                  .ReturnsAsync([]);

        var sut = CreateSut(urlService: urlService, secureStorage: secureStorage, songService: songService);
        sut.SongIdRaw = "42";
        await Task.Yield();
        sut.CompleteHydration();

        // User clears the locked field (REQ-ACREATE-15) and selects a different artist
        sut.ClearArtistCommand.Execute(null);
        sut.SelectedArtistId = 9;
        sut.SelectedArtistName = "David Bowie";

        await sut.SaveCommand.ExecuteAsync(null);

        var request = Assert.Single(requests);
        Assert.Equal(42, request.SongId);
        Assert.Equal(9, request.ArtistId);
    }

    // ── Title field: blur-first validation (Form Validation Standard, Task 04) ────

    [Fact]
    // [AC] R8: A pristine title field the user only tabbed through must not show a blur error.
    public void ValidateTitleCommand_PristineField_DoesNotSetError()
    {
        var songService = new Mock<ISongService>();
        var sut = CreateSut(songService: songService);
        sut.CompleteHydration();

        sut.ValidateTitleCommand.Execute(null);

        Assert.False(sut.TitleHasError);
        Assert.Empty(sut.TitleErrorText);
        songService.Verify(s => s.ValidateTitleInput(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    // [AC] R1: On blur, a dirty invalid title field surfaces an inline error.
    public void ValidateTitleCommand_DirtyInvalidField_SetsError()
    {
        var tooLong = new string('x', 251); // exceeds 250-char limit; differs from the default "" so
                                            // the property-changed handler actually fires
        var songService = new Mock<ISongService>();
        songService.Setup(s => s.ValidateTitleInput(tooLong))
                   .Returns((false, "Title is too long. Maximum 250 characters."));
        var sut = CreateSut(songService: songService);
        sut.CompleteHydration();
        sut.SongTitle = tooLong;

        sut.ValidateTitleCommand.Execute(null);

        Assert.True(sut.TitleHasError);
        Assert.Equal("Title is too long. Maximum 250 characters.", sut.TitleErrorText);
    }

    [Fact]
    // [AC] R1: On blur, a dirty valid title field shows no error.
    public void ValidateTitleCommand_DirtyValidField_NoError()
    {
        var songService = new Mock<ISongService>();
        songService.Setup(s => s.ValidateTitleInput("Bohemian Rhapsody")).Returns((true, ""));
        var sut = CreateSut(songService: songService);
        sut.CompleteHydration();
        sut.SongTitle = "Bohemian Rhapsody";

        sut.ValidateTitleCommand.Execute(null);

        Assert.False(sut.TitleHasError);
        Assert.Empty(sut.TitleErrorText);
    }

    [Fact]
    // [AC] R3: While NOT in error, keystrokes do not run validation (no "impatient teacher").
    public void OnSongTitleChanged_NotInError_DoesNotValidate()
    {
        var songService = new Mock<ISongService>();
        var sut = CreateSut(songService: songService);
        sut.CompleteHydration();

        sut.SongTitle = "x"; // short, but field is not yet in error

        Assert.False(sut.TitleHasError);
        songService.Verify(s => s.ValidateTitleInput(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    // [AC] R2: While in error, a keystroke that makes the field valid clears the error immediately.
    public void OnSongTitleChanged_WhileInError_ClearsErrorWhenValid()
    {
        var tooLong = new string('x', 251); // differs from the default "" so the handler fires
        var songService = new Mock<ISongService>();
        songService.Setup(s => s.ValidateTitleInput(tooLong))
                   .Returns((false, "Title is too long. Maximum 250 characters."));
        var sut = CreateSut(songService: songService);
        sut.CompleteHydration();
        sut.SongTitle = tooLong;
        sut.ValidateTitleCommand.Execute(null);
        Assert.True(sut.TitleHasError);

        songService.Setup(s => s.ValidateTitleInput("Bohemian Rhapsody")).Returns((true, ""));
        sut.SongTitle = "Bohemian Rhapsody";

        Assert.False(sut.TitleHasError);
        Assert.Empty(sut.TitleErrorText);
    }

    [Fact]
    // [AC] Edit-mode dirty-guard: hydration (query-property pre-population) must not mark the title
    // field dirty, so a pre-filled invalid value does not flash an error before the user interacts.
    public void OnSongTitleChanged_DuringHydration_DoesNotDirtyOrValidateOnBlur()
    {
        var tooLong = new string('x', 251); // differs from the default "" so the handler fires
        var songService = new Mock<ISongService>();
        var sut = CreateSut(songService: songService); // _isHydrating still true — CompleteHydration() not called

        sut.SongTitle = tooLong;                // simulates Shell QueryProperty pre-population
        sut.ValidateTitleCommand.Execute(null); // simulates a blur that happens before OnAppearing runs

        Assert.False(sut.TitleHasError);
        Assert.Empty(sut.TitleErrorText);
        songService.Verify(s => s.ValidateTitleInput(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    // [AC] Safety net: Save re-validates the title even if the field was never marked dirty.
    public async Task SaveAsync_TitleNeverDirty_SafetyNetSetsTitleError()
    {
        var songService = new Mock<ISongService>();
        songService.Setup(s => s.ValidateTitleInput(""))
                   .Returns((false, "Song title is required"));
        songService.Setup(s => s.ValidateVersionInput(It.IsAny<string>(), It.IsAny<bool>()))
                   .Returns((true, string.Empty));

        var sut = CreateSut(songService: songService);
        // SongTitle left at its default empty value — never marked dirty, never blurred.

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.True(sut.TitleHasError);
        Assert.Equal("Song title is required", sut.TitleErrorText);
    }

    // ── Version field: blur-first validation (Form Validation Standard, Task 04) ──

    [Fact]
    // [AC] R8: A pristine version field must not show a blur error.
    public void ValidateVersionCommand_PristineField_DoesNotSetError()
    {
        var songService = new Mock<ISongService>();
        var sut = CreateSut(songService: songService);
        sut.CompleteHydration();

        sut.ValidateVersionCommand.Execute(null);

        Assert.False(sut.VersionHasError);
        Assert.Empty(sut.VersionErrorText);
        songService.Verify(s => s.ValidateVersionInput(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    // [AC] R1: On blur, a dirty invalid version field (too long) surfaces an inline error.
    public void ValidateVersionCommand_DirtyInvalidField_SetsError()
    {
        var tooLong = new string('x', 61);
        var songService = new Mock<ISongService>();
        songService.Setup(s => s.ValidateVersionInput(tooLong, false))
                   .Returns((false, "Version is too long. Maximum 60 characters."));
        var sut = CreateSut(songService: songService);
        sut.CompleteHydration();
        sut.SongVersion = tooLong;

        sut.ValidateVersionCommand.Execute(null);

        Assert.True(sut.VersionHasError);
        Assert.Equal("Version is too long. Maximum 60 characters.", sut.VersionErrorText);
    }

    [Fact]
    // [AC] R1: On blur, a dirty valid (empty — optional field) version shows no error.
    public void ValidateVersionCommand_DirtyValidField_NoError()
    {
        var songService = new Mock<ISongService>();
        songService.Setup(s => s.ValidateVersionInput("Live", false)).Returns((true, ""));
        var sut = CreateSut(songService: songService);
        sut.CompleteHydration();
        sut.SongVersion = "Live";

        sut.ValidateVersionCommand.Execute(null);

        Assert.False(sut.VersionHasError);
        Assert.Empty(sut.VersionErrorText);
    }

    [Fact]
    // [AC] R2: While in error, a keystroke that makes the field valid clears the error immediately.
    public void OnSongVersionChanged_WhileInError_ClearsErrorWhenValid()
    {
        var tooLong = new string('x', 61);
        var songService = new Mock<ISongService>();
        songService.Setup(s => s.ValidateVersionInput(tooLong, false))
                   .Returns((false, "Version is too long. Maximum 60 characters."));
        var sut = CreateSut(songService: songService);
        sut.CompleteHydration();
        sut.SongVersion = tooLong;
        sut.ValidateVersionCommand.Execute(null);
        Assert.True(sut.VersionHasError);

        songService.Setup(s => s.ValidateVersionInput("Live", false)).Returns((true, ""));
        sut.SongVersion = "Live";

        Assert.False(sut.VersionHasError);
        Assert.Empty(sut.VersionErrorText);
    }

    [Fact]
    // [AC] R3: While NOT in error, keystrokes do not run validation (no "impatient teacher").
    public void OnSongVersionChanged_NotInError_DoesNotValidate()
    {
        var songService = new Mock<ISongService>();
        var sut = CreateSut(songService: songService);
        sut.CompleteHydration();

        sut.SongVersion = "L"; // field is not yet in error

        Assert.False(sut.VersionHasError);
        songService.Verify(s => s.ValidateVersionInput(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    // ── Character counter — counts the same (trimmed) string the validator checks ─

    [Fact]
    // [AC] Counter threshold alignment: the counter must count the trimmed string, exactly what
    // ValidateTitleInput checks — trailing whitespace must not inflate the count.
    public void OnSongTitleChanged_TrailingWhitespace_CounterUsesTrimmedLength()
    {
        var songService = new Mock<ISongService>();
        var sut = CreateSut(songService: songService);
        sut.CompleteHydration();
        var title = new string('x', 79) + "      ";   // 79 trimmed, 85 untrimmed

        sut.SongTitle = title;

        songService.Verify(s => s.ShouldShowCharacterCounter(79), Times.Once);
        songService.Verify(s => s.ShouldShowCharacterCounter(85), Times.Never);
    }

    // ── Re-entrancy guard (BUG-049) ────────────────────────────────────────

    [Fact]
    // [AC] REQ-SFT-51: a fast double-tap on Save while a save is in flight calls SaveFromFormAsync once
    // (BUG-049 guard carried over).
    public async Task SaveCommand_DoubleInvokedWhileSaving_CallsSaveFromFormOnlyOnce()
    {
        var songService = new Mock<ISongService>();
        songService.Setup(s => s.ValidateTitleInput("Yesterday")).Returns((true, ""));
        songService.Setup(s => s.ValidateVersionInput(string.Empty, false)).Returns((true, ""));
        var gate = new TaskCompletionSource<SongFormSaveOutcome>();
        songService.Setup(s => s.SaveFromFormAsync(It.IsAny<SongFormSaveRequest>(), It.IsAny<CancellationToken>()))
                   .Returns(gate.Task);

        var sut = CreateSut(songService: songService);
        sut.CompleteHydration();
        sut.SongTitle = "Yesterday";
        sut.SelectedArtistId = 5;

        var firstCall = sut.SaveCommand.ExecuteAsync(null);
        var secondCall = sut.SaveCommand.ExecuteAsync(null);   // simulates the second tap of a double-tap

        gate.SetResult(SavedOutcome());
        await Task.WhenAll(firstCall, secondCall);

        songService.Verify(
            s => s.SaveFromFormAsync(It.IsAny<SongFormSaveRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── BUG-051: stale-search race ──────────────────────────────────────────

    // [AC] REQ-ACREATE-13 (BUG-051): latest query wins over a slower earlier one
    [Fact]
    public async Task SearchArtistsAsync_OutOfOrderCompletion_LatestQueryWins()
    {
        var older = new TaskCompletionSource<IEnumerable<ArtistListItem>>();
        var newer = new TaskCompletionSource<IEnumerable<ArtistListItem>>();
        var artistService = new Mock<IArtistService>();
        artistService
            .SetupSequence(s => s.SearchArtistsByNameAsync(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(older.Task)
            .Returns(newer.Task);

        var sut = CreateSut(artistService: artistService);

        var t1 = sut.SearchArtistsCommand.ExecuteAsync("que");   // older request (issued first)
        var t2 = sut.SearchArtistsCommand.ExecuteAsync("queen"); // newer request (issued second)

        newer.SetResult([new ArtistListItem(2, "Queen", string.Empty, 3)]);   // newer completes first
        await t2;
        older.SetResult([new ArtistListItem(9, "Querido", string.Empty, 1)]); // older completes late
        await t1;

        var suggestion = Assert.Single(sut.ArtistSuggestions);
        Assert.Equal("Queen", suggestion.Headline); // older must NOT clobber
    }

    // ── BUG-052: edit-mode hydration must show the locked artist without searching ─────────

    // [AC] REQ-ACREATE-14 (BUG-052): hydration shows locked artist and fires no search
    [Fact]
    public void InitializeArtistField_EditModeHydration_ShowsLockedArtistWithoutSearch()
    {
        var artistService = new Mock<IArtistService>();

        var sut = CreateSut(artistService: artistService);

        // Simulates Shell QueryProperty pre-population (edit mode) followed by OnAppearing's call.
        sut.ArtistIdRaw = "7";
        sut.ArtistName = "Queen";
        sut.InitializeArtistField();

        Assert.Equal("Queen", sut.SelectedArtistName);
        Assert.Equal("Queen", sut.ArtistSearchText);
        Assert.True(sut.IsArtistLocked);
        artistService.Verify(
            s => s.SearchArtistsByNameAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never());
    }

    // ── BUG-056: search returns current-query results directly (no read-before-dispatch gap) ──

    // [AC] REQ-ACREATE-13 (BUG-056): the search must return the CURRENT query's mapped results
    // directly to the caller (the page's AutoCompleteEdit provider) so the provider never has to
    // read ArtistSuggestions before the background UI dispatch that assigns it has landed.
    [Fact]
    public async Task SearchArtistsCoreAsync_ReturnsCurrentQueryResultsDirectly()
    {
        var artistService = new Mock<IArtistService>();
        artistService
            .Setup(s => s.SearchArtistsByNameAsync("queen", It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ArtistListItem(2, "Queen", string.Empty, 3)]);

        var sut = CreateSut(artistService: artistService);

        var results = await sut.SearchArtistsCoreAsync("queen");

        var suggestion = Assert.Single(results);
        Assert.Equal("Queen", suggestion.Headline);
    }

    // ── BUG-055: edit-mode hydration must populate + lock the stored artist ───────────────────

    // [AC] REQ-ACREATE-14 (BUG-055): opening a saved song for edit hydrates its stored artist
    // (id, name, search text) as locked, without firing a suggestion search.
    [Fact]
    public async Task LoadSongForEdit_ExistingSong_HydratesArtistAsLocked()
    {
        var song = new Song
        {
            Id = 42,
            ArtistId = 7,
            Title = "Stored Title",
            OriginalArtist = new Artist { Id = 7, Name = "Queen" }
        };
        var songService = MakeSongServiceWithSong(song);
        var artistService = new Mock<IArtistService>();
        var secureStorage = new Mock<ISecureStorageWrapper>();
        secureStorage.Setup(s => s.GetAsync(It.IsAny<string>())).ReturnsAsync((string?)null);
        var urlService = new Mock<ISongKaraokeUrlService>();
        urlService.Setup(s => s.GetUrlsForSongAsync(42, It.IsAny<CancellationToken>()))
                  .ReturnsAsync([]);

        var sut = CreateSut(urlService: urlService, secureStorage: secureStorage,
            songService: songService, artistService: artistService);
        sut.SongIdRaw = "42";
        await Task.Yield();

        Assert.Equal(7, sut.SelectedArtistId);
        Assert.Equal("Queen", sut.SelectedArtistName);
        Assert.Equal("Queen", sut.ArtistSearchText);
        Assert.True(sut.IsArtistLocked);
        artistService.Verify(
            s => s.SearchArtistsByNameAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never());
    }

    // [AC] REQ-SFT-28: the stored artist loads locked; Save after hydration sends its id, so the artist link is kept
    // (BUG-055 carried over).
    [Fact]
    public async Task SaveAsync_EditMode_AfterHydration_PreservesArtistLink()
    {
        var song = new Song
        {
            Id = 42,
            ArtistId = 7,
            Title = "Stored Title",
            OriginalArtist = new Artist { Id = 7, Name = "Queen" }
        };
        var requests = new List<SongFormSaveRequest>();
        var songService = MakeSongServiceWithSong(song);
        SetupSave(songService, requests, _ => SavedOutcome(42));
        var secureStorage = new Mock<ISecureStorageWrapper>();
        secureStorage.Setup(s => s.GetAsync(It.IsAny<string>())).ReturnsAsync((string?)null);
        var urlService = new Mock<ISongKaraokeUrlService>();
        urlService.Setup(s => s.GetUrlsForSongAsync(42, It.IsAny<CancellationToken>()))
                  .ReturnsAsync([]);

        var sut = CreateSut(urlService: urlService, secureStorage: secureStorage, songService: songService);
        sut.SongIdRaw = "42";
        await Task.Yield();
        sut.CompleteHydration();

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.False(sut.ArtistHasError);
        var request = Assert.Single(requests);
        Assert.Equal(42, request.SongId);
        Assert.Equal(7, request.ArtistId);
    }

    // ── Task 5.4: edit-mode title and hydration (REQ-SFT-27, REQ-SFT-02) ─────────────────────

    /// <summary>A stored song: API-sourced (Deezer identity) or manual (no identity), artist "Queen".</summary>
    private static Song StoredSong(bool apiSourced) => new()
    {
        Id = 42,
        ArtistId = 7,
        Title = "Stored Title",
        ExternalId = apiSourced ? "dz-99" : null,
        ExternalProvider = apiSourced ? "Deezer" : null,
        OriginalArtist = new Artist { Id = 7, Name = "Queen" }
    };

    /// <summary>An edit-form VM hydrated from <paramref name="song"/>: Shell query applied, load awaited, hydration ended.</summary>
    private async Task<SongFormViewModel> CreateEditSutAsync(
        Song song, Mock<ISongService> songService, Mock<IArtistService>? artistService = null)
    {
        var secureStorage = new Mock<ISecureStorageWrapper>();
        secureStorage.Setup(s => s.GetAsync(It.IsAny<string>())).ReturnsAsync((string?)null);
        var urlService = new Mock<ISongKaraokeUrlService>();
        urlService.Setup(s => s.GetUrlsForSongAsync(song.Id, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var sut = CreateSut(urlService: urlService, secureStorage: secureStorage,
            songService: songService, artistService: artistService);
        sut.SongIdRaw = song.Id.ToString();
        await Task.Yield();
        sut.CompleteHydration();
        return sut;
    }

    // [AC] REQ-SFT-27: in edit mode Title is editable text for an API-sourced and for a manual song; the VM never pick-locks it.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task EditMode_TitleIsNotPickLocked_ForApiAndManualSongs(bool apiSourced)
    {
        var song = StoredSong(apiSourced);
        var sut = await CreateEditSutAsync(song, MakeSongServiceWithSong(song));

        Assert.False(sut.IsTitleLocked);
    }

    // [AC] REQ-SFT-27: in edit mode Title has no search (IsTitleSearchEnabled is false) for an API-sourced and for a manual song.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task EditMode_TitleSearchIsDisabled_ForApiAndManualSongs(bool apiSourced)
    {
        var song = StoredSong(apiSourced);
        var sut = await CreateEditSutAsync(song, MakeSongServiceWithSong(song));

        Assert.False(sut.IsTitleSearchEnabled);
    }

    // [AC] REQ-SFT-02: in add mode Title is the search box (IsTitleSearchEnabled is true).
    [Fact]
    public void AddMode_TitleSearchIsEnabled()
    {
        var sut = CreateAddFormSut();

        Assert.True(sut.IsTitleSearchEnabled);
    }

    // [AC] REQ-SFT-27: saving an edited Title sends the new title and leaves the external identity to the Service
    // ("keep existing" = null, design.md § 5 flow F6). Manual-song row: identity is null already.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task EditMode_SaveEditedTitle_SendsNewTitleAndKeepsIdentity(bool apiSourced)
    {
        var song = StoredSong(apiSourced);
        var requests = new List<SongFormSaveRequest>();
        var songService = MakeSongServiceWithSong(song);
        SetupSave(songService, requests, _ => SavedOutcome(42));
        var sut = await CreateEditSutAsync(song, songService);

        sut.SongTitle = "Corrected Title";
        await sut.SaveCommand.ExecuteAsync(null);

        var request = Assert.Single(requests);
        Assert.Equal(42, request.SongId);
        Assert.Equal("Corrected Title", request.Title);
        Assert.Null(request.ExternalId);
        Assert.Null(request.ExternalProvider);
    }

    // [AC] REQ-SFT-27: edit-mode hydration shows the stored Title as editable text and queries no title search.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task EditMode_Hydration_ShowsStoredTitle_AndQueriesNoTitleSearch(bool apiSourced)
    {
        var song = StoredSong(apiSourced);
        var sut = await CreateEditSutAsync(song, MakeSongServiceWithSong(song));

        Assert.Equal("Stored Title", sut.SongTitle);
        _suggestionsMock.Verify(s => s.SearchTitleLocalAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _suggestionsMock.Verify(s => s.SearchTitleRemoteAsync(It.IsAny<string>(), It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // [AC] REQ-SFT-27: editing Title in edit mode (the Field has no search) never calls the title suggestion service.
    [Fact]
    public async Task EditMode_EditingTitle_QueriesNoTitleSearch()
    {
        var song = StoredSong(apiSourced: true);
        var sut = await CreateEditSutAsync(song, MakeSongServiceWithSong(song));

        sut.SongTitle = "Corrected Title";

        _suggestionsMock.Verify(s => s.SearchTitleLocalAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _suggestionsMock.Verify(s => s.SearchTitleRemoteAsync(It.IsAny<string>(), It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
