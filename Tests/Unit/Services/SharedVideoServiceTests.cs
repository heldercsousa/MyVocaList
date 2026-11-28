using MyVocaList.Contracts.DTOs;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Unit.Services;

public class SharedVideoServiceTests
{
    private const string VideoId = "dQw4w9WgXcQ";

    private readonly Mock<IYouTubeOEmbedClient> _oEmbed = new();
    private readonly Mock<ISongKaraokeUrlRepository> _urls = new();
    private readonly Mock<IMusicMetadataProvider> _provider = new();

    public SharedVideoServiceTests()
    {
        _provider.SetupGet(p => p.ProviderName).Returns(MyVocaList.Domain.Constants.ProviderNames.MusicBrainz);
        _provider.Setup(p => p.SearchSongsAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<MusicSearchResultDto>());
    }

    private SharedVideoService CreateSut() => new(
        _oEmbed.Object, [_provider.Object], _urls.Object, PassthroughUnitOfWork.Over(_urls),
        new Mock<ILogger<SharedVideoService>>().Object);

    private static MusicSearchResultDto Recording(string title, string artist, string id = "rec-1", string? artistId = "artist-1")
        => new(id, "MusicBrainz", artist, title, ExternalArtistId: artistId);

    private void OEmbedReturns(string title, string? channel = null)
        => _oEmbed.Setup(o => o.GetVideoInfoAsync(VideoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new YouTubeVideoInfo(title, "https://thumb", channel));

    private void SearchReturns(string title, string? artistHint, params MusicSearchResultDto[] results)
        => _provider.Setup(p => p.SearchSongsAsync(title, artistHint, It.IsAny<CancellationToken>())).ReturnsAsync(results);

    // ── ResolveTargetAsync (REQ-MSRC-11) ──────────────────────────────────

    // [AC] REQ-MSRC-11: a video id held by a stored song opens that song (edit mode)
    [Fact]
    public async Task ResolveTargetAsync_StoredVideo_ReturnsSongId()
    {
        _urls.Setup(u => u.FindSongIdByVideoIdAsync(VideoId, It.IsAny<CancellationToken>())).ReturnsAsync(42);

        var target = await CreateSut().ResolveTargetAsync($"Karaoke https://youtu.be/{VideoId}");

        Assert.Equal(new SharedVideoTarget(VideoId, 42), target);
    }

    // [AC] REQ-MSRC-11: an unknown video id opens the form in add mode
    [Fact]
    public async Task ResolveTargetAsync_UnknownVideo_ReturnsNullSongId()
    {
        _urls.Setup(u => u.FindSongIdByVideoIdAsync(VideoId, It.IsAny<CancellationToken>())).ReturnsAsync((int?)null);

        var target = await CreateSut().ResolveTargetAsync($"https://youtu.be/{VideoId}");

        Assert.Equal(new SharedVideoTarget(VideoId, null), target);
    }

    // [AC] REQ-MSRC-11: text without a YouTube link is not a share target
    [Fact]
    public async Task ResolveTargetAsync_NoYouTubeLink_ReturnsNull()
        => Assert.Null(await CreateSut().ResolveTargetAsync("https://example.com/x"));

    // ── PrefillAsync (REQ-MSRC-12) ────────────────────────────────────────

    // [AC] REQ-MSRC-12: oEmbed unavailable -> nothing to prefill
    [Fact]
    public async Task PrefillAsync_OEmbedFails_ReturnsNull()
    {
        _oEmbed.Setup(o => o.GetVideoInfoAsync(VideoId, It.IsAny<CancellationToken>())).ReturnsAsync((YouTubeVideoInfo?)null);

        Assert.Null(await CreateSut().PrefillAsync(VideoId));
    }

    // [AC] REQ-MSRC-12: the cleaned title is the MusicBrainz query; "Artist - Title" is tried as title + artist hint
    [Fact]
    public async Task PrefillAsync_ArtistFirstTitle_QueriesTitleWithArtistHint()
    {
        OEmbedReturns("Queen - Bohemian Rhapsody (Karaoke Version)");
        SearchReturns("Bohemian Rhapsody", "Queen", Recording("Bohemian Rhapsody", "Queen", "q", "queen-mbid"));

        var prefill = await CreateSut().PrefillAsync(VideoId);

        Assert.Equal("Queen - Bohemian Rhapsody", prefill!.CleanedTitle);
        Assert.Equal("q", prefill.Match!.ExternalId);
        Assert.Equal("Queen", prefill.Match.ArtistName);
        Assert.Equal("queen-mbid", prefill.Match.ArtistExternalId);
        Assert.True(prefill.Match.IsRemote);
    }

    // [AC] REQ-MSRC-12: the title can come first ("Title - Artist"); the reverse split is tried when the first finds nothing
    [Fact]
    public async Task PrefillAsync_TitleFirst_TriesReverseSplit()
    {
        OEmbedReturns("Evidencias - Chitãozinho & Xororó Karaokê UPGRADED");
        SearchReturns("Evidencias", "Chitãozinho & Xororó", Recording("Evidências", "Chitãozinho & Xororó", "e"));

        var prefill = await CreateSut().PrefillAsync(VideoId);

        Assert.Equal("e", prefill!.Match!.ExternalId);
    }

    // [AC] REQ-MSRC-12: the channel name is removed before the query
    [Fact]
    public async Task PrefillAsync_ChannelNameIsRemoved()
    {
        OEmbedReturns("Sing King Karaoke - Shallow - Lady Gaga", "Sing King");

        var prefill = await CreateSut().PrefillAsync(VideoId);

        Assert.Equal("Shallow - Lady Gaga", prefill!.CleanedTitle);
    }

    // [AC] REQ-MSRC-12: a title with no separator is searched whole; the result with the same title wins over the first
    [Fact]
    public async Task PrefillAsync_SingleTitle_PrefersExactTitle()
    {
        OEmbedReturns("Garota de Ipanema (Karaoke)");
        SearchReturns("Garota de Ipanema", null,
            Recording("Garota de Ipanema (live)", "Other", "o"), Recording("Garota de Ipanema", "Tom Jobim", "g"));

        var prefill = await CreateSut().PrefillAsync(VideoId);

        Assert.Equal("g", prefill!.Match!.ExternalId);
    }

    // [AC] REQ-MSRC-12: nothing matches -> no match, the cleaned text is returned for the Title
    [Fact]
    public async Task PrefillAsync_NoRecordingMatches_ReturnsCleanedTextOnly()
    {
        OEmbedReturns("Unknown Artist - Unknown Song (Karaoke)");

        var prefill = await CreateSut().PrefillAsync(VideoId);

        Assert.Null(prefill!.Match);
        Assert.Equal("Unknown Artist - Unknown Song", prefill.CleanedTitle);
    }

    // [AC] REQ-MSRC-12: MusicBrainz unreachable -> no match, the cleaned text is still returned
    [Fact]
    public async Task PrefillAsync_MusicBrainzFails_ReturnsCleanedTextOnly()
    {
        OEmbedReturns("Queen - Bohemian Rhapsody");
        _provider.Setup(p => p.SearchSongsAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("offline"));

        var prefill = await CreateSut().PrefillAsync(VideoId);

        Assert.Null(prefill!.Match);
        Assert.Equal("Queen - Bohemian Rhapsody", prefill.CleanedTitle);
    }

    // [AC] REQ-MSRC-12: a title that is only noise -> empty cleaned text, no query
    [Fact]
    public async Task PrefillAsync_OnlyNoise_EmptyTextNoQuery()
    {
        OEmbedReturns("Karaoke Instrumental");

        var prefill = await CreateSut().PrefillAsync(VideoId);

        Assert.Equal(string.Empty, prefill!.CleanedTitle);
        _provider.Verify(p => p.SearchSongsAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
