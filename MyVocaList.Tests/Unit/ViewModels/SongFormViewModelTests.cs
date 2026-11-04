using CommunityToolkit.Mvvm.Messaging;
using MyVocaList.Contracts.DTOs;
using MyVocaList.Contracts.DTOs.Suggestions;
using MyVocaList.Domain.ReadModels;
using MyVocaList.Domain.Resolution;
using MyVocaList.Navigation;
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
        Mock<ISongResolutionService>? resolutionService = null,
        Mock<IArtistService>? artistService = null,
        IMessenger? messenger = null)
    {
        return new SongFormViewModel(
            (artistService ?? new Mock<IArtistService>()).Object,
            (songService ?? new Mock<ISongService>()).Object,
            (resolutionService ?? new Mock<ISongResolutionService>()).Object,
            (snackbar ?? new Mock<ISnackbarComponent>()).Object,
            new Mock<ILogger<SongFormViewModel>>().Object,
            (urlService ?? new Mock<ISongKaraokeUrlService>()).Object,
            (secureStorage ?? new Mock<ISecureStorageWrapper>()).Object,
            messenger ?? new Mock<IMessenger>().Object,
            _navigationMock.Object,
            _suggestionsMock.Object);
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

    // [AC] REQ-SFT-04: a local option shows "artist · version · In your library".
    [Fact]
    public async Task TitleOptionSource_LocalHit_SupportingTextHasArtistVersionAndLibraryMarker()
    {
        SetupSearch([LocalDto(1, "Creep", "Radiohead", "Pablo Honey")], [], []);
        var sut = CreateAddFormSut();

        var options = await SearchTitle(sut, "creep");

        var option = Assert.Single(options);
        Assert.Equal("Creep", option.Headline);
        Assert.Equal("Radiohead · Pablo Honey · In your library", option.SupportingText);
    }

    // [AC] REQ-SFT-04: a local option with an empty Version omits the version part.
    [Fact]
    public async Task TitleOptionSource_LocalHitEmptyVersion_OmitsVersionPart()
    {
        SetupSearch([LocalDto(1, "Creep", "Radiohead", "")], [], []);
        var sut = CreateAddFormSut();

        var options = await SearchTitle(sut, "creep");

        Assert.Equal("Radiohead · In your library", Assert.Single(options).SupportingText);
    }

    // [AC] REQ-SFT-04: a remote option shows "artist · version · Deezer" (D21).
    [Fact]
    public async Task TitleOptionSource_RemoteHit_SupportingTextIsArtistVersionAndDeezer()
    {
        SetupSearch([], [], [RemoteDto("Creep", "Radiohead", "Live")]);
        var sut = CreateAddFormSut();

        var options = await SearchTitle(sut, "creep");

        var option = Assert.Single(options);
        Assert.Equal("Creep", option.Headline);
        Assert.Equal("Radiohead · Live · Deezer", option.SupportingText);
    }

    // [AC] REQ-SFT-04: a remote option with an empty Version omits the version part (D21).
    [Fact]
    public async Task TitleOptionSource_RemoteHitEmptyVersion_OmitsVersionPart()
    {
        SetupSearch([], [], [RemoteDto("Creep", "Radiohead", "")]);
        var sut = CreateAddFormSut();

        var options = await SearchTitle(sut, "creep");

        Assert.Equal("Radiohead · Deezer", Assert.Single(options).SupportingText);
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

    // [AC] REQ-SFT-11: after X on Title a pick-filled Version never triggers the discard sheet (Q21).
    [Fact]
    public async Task ClearTitle_AfterRemotePickThenLocalPick_NoDiscardSheetFromPickFilledVersion()
    {
        var sut = CreateAddFormSut();
        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto("Creep", "Radiohead", "Live")));
        sut.ClearArtistCommand.Execute(null);   // the pick also filled Artist; only the Version is under test
        sut.ClearTitleCommand.Execute(null);

        await sut.SelectTitleCommand.ExecuteAsync(Pick(LocalDto(42)));

        Assert.False(sut.IsDiscardSheetVisible);
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
        songService.VerifyNoOtherCalls();
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
        Assert.False(sut.IsDiscardSheetVisible);
    }

    // [AC] REQ-SFT-11: a local pick with user input in Artist, Version, Featured, Lyrics or the YouTube list asks first.
    [Theory]
    [InlineData("Artist")]
    [InlineData("Version")]
    [InlineData("Featured")]
    [InlineData("Lyrics")]
    [InlineData("YouTube")]
    public async Task SelectTitle_LocalOptionWithOtherInput_ShowsDiscardSheetNoNavigation(string field)
    {
        var sut = CreateAddFormSut();
        sut.SongTitle = "cre";
        FillOtherInput(sut, field);

        await sut.SelectTitleCommand.ExecuteAsync(Pick(LocalDto(42)));

        Assert.True(sut.IsDiscardSheetVisible);
        _navigationMock.Verify(n => n.GoToAsync(It.IsAny<string>()), Times.Never);
    }

    private static void FillOtherInput(SongFormViewModel sut, string field)
    {
        switch (field)
        {
            case "Artist": sut.ArtistSearchText = "Radio"; break;
            case "Version": sut.SongVersion = "Live"; break;
            case "Featured": sut.FeaturedArtists = "Someone"; break;
            case "Lyrics": sut.Lyrics = "Some lyrics"; break;
            case "YouTube": sut.KaraokeUrls.Add(MakeDto("vid-1", 0)); break;
            default: throw new ArgumentOutOfRangeException(nameof(field), field, null);
        }
    }

    // [AC] REQ-SFT-11: Open on the discard sheet opens the picked song's edit form.
    [Fact]
    public async Task ConfirmOpenExisting_FromDiscardSheet_Navigates()
    {
        var sut = CreateAddFormSut();
        sut.SongVersion = "Live";
        await sut.SelectTitleCommand.ExecuteAsync(Pick(LocalDto(42)));
        Assert.True(sut.IsDiscardSheetVisible);

        await sut.ConfirmOpenExistingCommand.ExecuteAsync(null);

        _navigationMock.Verify(n => n.GoToAsync($"../{Routes.SongForm}?songId=42"), Times.Once);
        Assert.False(sut.IsDiscardSheetVisible);
    }

    // [AC] REQ-SFT-11: Keep editing closes the sheet and restores the typed query, unlocked; nothing else changes.
    [Fact]
    public async Task KeepEditing_RestoresTypedQueryUnlocked_NothingElseChanges()
    {
        var sut = CreateAddFormSut();
        SetupSearch([LocalDto(42, "Creep")], [], []);
        await SearchTitle(sut, "cre");                  // the typed query
        sut.SongTitle = "Creep";                        // the Field writes the picked headline into Title
        sut.SongVersion = "Live";
        await sut.SelectTitleCommand.ExecuteAsync(Pick(LocalDto(42, "Creep")));
        Assert.True(sut.IsDiscardSheetVisible);

        sut.KeepEditingCommand.Execute(null);

        Assert.False(sut.IsDiscardSheetVisible);
        Assert.Equal("cre", sut.SongTitle);
        Assert.False(sut.IsTitleLocked);
        Assert.Equal("Live", sut.SongVersion);
        _navigationMock.Verify(n => n.GoToAsync(It.IsAny<string>()), Times.Never);
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

    // ── BUG-005: SaveAsync exception catch ────────────────────────────────

    // [AC] AC-B5 — service throws → error snackbar shown, no crash
    [Fact]
    public async Task SaveAsync_ServiceThrows_ShowsErrorSnackbar()
    {
        var songService = new Mock<ISongService>();
        songService.Setup(s => s.ValidateTitleInput(It.IsAny<string>()))
                   .Returns((true, string.Empty));
        songService.Setup(s => s.ValidateVersionInput(It.IsAny<string>(), It.IsAny<bool>()))
                   .Returns((true, string.Empty));
        var resolution = new Mock<ISongResolutionService>();
        resolution.Setup(s => s.ResolveAsync(It.IsAny<SongCandidate>(), It.IsAny<CancellationToken>()))
                  .ThrowsAsync(new InvalidOperationException("DB context error"));
        var snackbar = new Mock<ISnackbarComponent>();

        var sut = CreateSut(snackbar: snackbar, songService: songService, resolutionService: resolution);
        sut.SongTitle = "Test Song";
        sut.SelectedArtistId = 1;
        sut.SelectedArtistName = "Artist";

        await sut.SaveCommand.ExecuteAsync(null);

        snackbar.Verify(s => s.ShowErrorAsync(It.Is<string>(m => m.Contains("Failed to save"))), Times.Once);
        Assert.False(sut.IsBusy); // IsBusy reset in finally
    }

    // [AC] AC-B5 — validation failure (no artist) → does not reach service, no crash
    [Fact]
    public async Task SaveAsync_NoArtist_SetsArtistError_NoServiceCall()
    {
        var songService = new Mock<ISongService>();
        songService.Setup(s => s.ValidateTitleInput(It.IsAny<string>()))
                   .Returns((true, string.Empty));
        songService.Setup(s => s.ValidateVersionInput(It.IsAny<string>(), It.IsAny<bool>()))
                   .Returns((true, string.Empty));
        var sut = CreateSut(songService: songService);
        sut.SongTitle = "Test Song";
        // SelectedArtistId not set

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.True(sut.ArtistHasError);
        songService.Verify(s => s.CreateSongAsync(
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // [AC] AC-B5 — NoMatch path → CreateSongWithUrlsAsync called
    [Fact]
    public async Task SaveAsync_NoMatch_CallsCreateSongWithUrls()
    {
        var songService = new Mock<ISongService>();
        songService.Setup(s => s.ValidateTitleInput(It.IsAny<string>()))
                   .Returns((true, string.Empty));
        songService.Setup(s => s.ValidateVersionInput(It.IsAny<string>(), It.IsAny<bool>()))
                   .Returns((true, string.Empty));
        songService.Setup(s => s.CreateSongWithUrlsAsync(
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "Song created", (Song?)null));

        var resolution = new Mock<ISongResolutionService>();
        resolution.Setup(s => s.ResolveAsync(It.IsAny<SongCandidate>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new SongResolution(ResolutionKind.NoMatch, null, []));

        var snackbar = new Mock<ISnackbarComponent>();

        var sut = CreateSut(snackbar: snackbar, songService: songService, resolutionService: resolution);
        sut.SongTitle = "Test Song";
        sut.SelectedArtistId = 1;
        sut.SelectedArtistName = "Artist";

        await sut.SaveCommand.ExecuteAsync(null);

        songService.Verify(s => s.CreateSongWithUrlsAsync(
            1, "Test Song", It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<IEnumerable<string>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // [AC] AC-1.1/1.2 — resolution returns ExactLocalMatch → resolution sheet shown
    [Fact]
    public async Task SaveAsync_ExactLocalMatch_SetsResolutionSheetVisible()
    {
        var songService = new Mock<ISongService>();
        songService.Setup(s => s.ValidateTitleInput(It.IsAny<string>()))
                   .Returns((true, string.Empty));
        songService.Setup(s => s.ValidateVersionInput(It.IsAny<string>(), It.IsAny<bool>()))
                   .Returns((true, string.Empty));

        var resolution = new Mock<ISongResolutionService>();
        resolution.Setup(s => s.ResolveAsync(It.IsAny<SongCandidate>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new SongResolution(ResolutionKind.ExactLocalMatch, 99, []));

        var sut = CreateSut(songService: songService, resolutionService: resolution);
        sut.SongTitle = "Test Song";
        sut.SelectedArtistId = 1;
        sut.SelectedArtistName = "Artist";

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.True(sut.IsResolutionSheetVisible);
        Assert.Equal(1, sut.ResolutionCandidates.Count);
    }

    // [AC] AC-1.1/1.2 — BUG-023 regression guard: the resolution sheet's Cancel button is
    // bound to DismissResolutionSheetCommand, which the SongFormPage code-behind relies on to
    // close resolutionSheet (see SongFormPage.xaml.cs OnViewModelPropertyChanged). This test
    // proves the VM half of that round trip: the flag the view observes must flip back to false
    // once the sheet is opened and then dismissed. The XAML binding restoration itself is not
    // unit-testable — see BUG-023 task-log for the required manual E2E verification step.
    [Fact]
    public async Task DismissResolutionSheetCommand_AfterExactLocalMatch_SetsIsResolutionSheetVisibleFalse()
    {
        var songService = new Mock<ISongService>();
        songService.Setup(s => s.ValidateTitleInput(It.IsAny<string>()))
                   .Returns((true, string.Empty));
        songService.Setup(s => s.ValidateVersionInput(It.IsAny<string>(), It.IsAny<bool>()))
                   .Returns((true, string.Empty));

        var resolution = new Mock<ISongResolutionService>();
        resolution.Setup(s => s.ResolveAsync(It.IsAny<SongCandidate>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new SongResolution(ResolutionKind.ExactLocalMatch, 99, []));

        var sut = CreateSut(songService: songService, resolutionService: resolution);
        sut.SongTitle = "Test Song";
        sut.SelectedArtistId = 1;
        sut.SelectedArtistName = "Artist";

        await sut.SaveCommand.ExecuteAsync(null);
        Assert.True(sut.IsResolutionSheetVisible); // precondition: sheet was opened by the duplicate-title flow

        sut.DismissResolutionSheetCommand.Execute(null);

        Assert.False(sut.IsResolutionSheetVisible);
    }

    // [AC] AC-1.2 — save as new version with empty Version is blocked
    [Fact]
    public async Task ConfirmSaveAsNewVersion_EmptyVersion_SetsVersionError()
    {
        var songService = new Mock<ISongService>();
        songService.Setup(s => s.ValidateVersionInput(string.Empty, true))
                   .Returns((false, "A version label is required (e.g. Live, Acoustic, Remix)"));

        var sut = CreateSut(songService: songService);
        sut.SongTitle = "Test Song";
        sut.SelectedArtistId = 1;
        sut.SelectedArtistName = "Artist";
        sut.SongVersion = ""; // empty

        await sut.ConfirmSaveAsNewVersionCommand.ExecuteAsync(null);

        Assert.True(sut.VersionHasError);
        Assert.False(string.IsNullOrEmpty(sut.VersionErrorText));
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
    // [AC] BUG-024: edit-mode Save must send the complete current form data — hydrated
    // FeaturedArtists and Lyrics plus the user's edited Version — to UpdateSongAsync.
    // Before the fix it sent empty FeaturedArtists/Lyrics and ignored Version entirely.
    public async Task SaveAsync_EditMode_SendsHydratedFieldsAndEditedVersion()
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
        songService.Setup(s => s.UpdateSongAsync(
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "stop before navigation")); // avoid Shell.Current in unit test
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

        songService.Verify(s => s.UpdateSongAsync(
            42, "Stored Title", "Feat A", "Stored lyrics",
            null, null, "Acoustic",
            It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    // [AC] REQ-ACREATE-16: BUG-067 — after the user unlocks the Artist field and picks a
    // different artist, edit-mode Save must forward the CURRENT SelectedArtistId to the service.
    // Before the fix ExecuteEditSaveAsync could not send the artist at all (no parameter existed),
    // so the change was silently lost.
    public async Task SaveAsync_EditMode_ArtistChanged_SendsNewArtistIdToService()
    {
        var song = new Song
        {
            Id = 42,
            ArtistId = 7,
            Title = "Stored Title",
            OriginalArtist = new Artist { Id = 7, Name = "Queen" }
        };
        var songService = MakeSongServiceWithSong(song);
        songService.Setup(s => s.UpdateSongAsync(
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "stop before navigation")); // avoid Shell.Current in unit test
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

        songService.Verify(s => s.UpdateSongAsync(
            42, "Stored Title", It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            9, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
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
        var tooLong = new string('x', 101); // exceeds 100-char limit; differs from the default "" so
                                            // the property-changed handler actually fires
        var songService = new Mock<ISongService>();
        songService.Setup(s => s.ValidateTitleInput(tooLong))
                   .Returns((false, "Title is too long. Maximum 100 characters."));
        var sut = CreateSut(songService: songService);
        sut.CompleteHydration();
        sut.SongTitle = tooLong;

        sut.ValidateTitleCommand.Execute(null);

        Assert.True(sut.TitleHasError);
        Assert.Equal("Title is too long. Maximum 100 characters.", sut.TitleErrorText);
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
        var tooLong = new string('x', 101); // differs from the default "" so the handler fires
        var songService = new Mock<ISongService>();
        songService.Setup(s => s.ValidateTitleInput(tooLong))
                   .Returns((false, "Title is too long. Maximum 100 characters."));
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
        var tooLong = new string('x', 101); // differs from the default "" so the handler fires
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
    // [AC] BUG-049: a fast double-tap on Save while a save is in flight must not fire the
    // resolution service call twice (which caused a duplicate "GoToAsync("..")" that overshot
    // the nav stack root).
    public async Task SaveCommand_DoubleInvokedWhileSaving_CallsResolveOnlyOnce()
    {
        var songService = new Mock<ISongService>();
        songService.Setup(s => s.ValidateTitleInput("Yesterday")).Returns((true, ""));
        songService.Setup(s => s.ValidateVersionInput(string.Empty, false)).Returns((true, ""));
        var resolutionService = new Mock<ISongResolutionService>();
        var gate = new TaskCompletionSource<SongResolution>();
        resolutionService.Setup(s => s.ResolveAsync(It.IsAny<SongCandidate>(), It.IsAny<CancellationToken>()))
                         .Returns(gate.Task);

        var sut = CreateSut(songService: songService, resolutionService: resolutionService);
        sut.CompleteHydration();
        sut.SongTitle = "Yesterday";
        sut.SelectedArtistId = 5;

        var firstCall = sut.SaveCommand.ExecuteAsync(null);
        var secondCall = sut.SaveCommand.ExecuteAsync(null);   // simulates the second tap of a double-tap

        gate.SetResult(new SongResolution(ResolutionKind.NoMatch, null, []));
        await Task.WhenAll(firstCall, secondCall);

        resolutionService.Verify(
            s => s.ResolveAsync(It.IsAny<SongCandidate>(), It.IsAny<CancellationToken>()), Times.Once);
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

    // [AC] BUG-055: after edit-mode hydration the artist link is preserved — Save passes the
    // artist-required guard without the user re-selecting, so UpdateSongAsync is invoked (the
    // service keeps the loaded song's ArtistId). Before the fix the guard blocked Save.
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
        var songService = MakeSongServiceWithSong(song);
        songService.Setup(s => s.UpdateSongAsync(
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "stop before navigation")); // avoid Shell.Current in unit test
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
        songService.Verify(s => s.UpdateSongAsync(
            42, "Stored Title", It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
