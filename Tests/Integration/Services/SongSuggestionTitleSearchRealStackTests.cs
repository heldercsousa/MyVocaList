using System.Net;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using CommunityToolkit.Mvvm.Messaging;
using MyVocaList.Contracts.DTOs.Suggestions;
using MyVocaList.Navigation;
using MyVocaList.UI.Services;
using MyVocaList.Domain.ServicesInterfaces;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Integration.Services;

/// <summary>
/// Device symptom (SFT 5.5): typing "commando" in the New Song Title field showed no menu and no note.
/// Reproduces the remote title search end to end: real <see cref="MusicBrainzProvider"/> over a fake HTTP
/// handler serving the real MusicBrainz payload shape, real SQLite temp-file repositories, real
/// <see cref="SongSuggestionService"/> via the production composition.
/// </summary>
public class SongSuggestionTitleSearchRealStackTests
{
    // Real payload shape: unicode escape, title_version, extra unknown fields, top results not by the typed artist.
    private const string Payload = """
        {"recordings":[
         {"id":"11111111-1111-1111-1111-111111111111","title":"Commando","length":195000,"artist-credit":[{"name":"Niska","artist":{"id":"a1111111-1111-1111-1111-111111111111","name":"Niska"}}],"releases":[{"title":"Commando"}]},
         {"id":"22222222-2222-2222-2222-222222222222","title":"Comando","length":180000,"artist-credit":[{"name":"Banda X","artist":{"id":"a2222222-2222-2222-2222-222222222222","name":"Banda X"}}],"releases":[{"title":"A"}]},
         {"id":"33333333-3333-3333-3333-333333333333","title":"Chasse à l'homme","artist-credit":[{"name":"Zaz","artist":{"id":"a3333333-3333-3333-3333-333333333333","name":"Zaz"}}]}
        ]}
        """;

    private sealed class StubHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(Payload, Encoding.UTF8, "application/json")
            });
    }

    private sealed class OnlineNetwork : INetworkStatus
    {
        public bool HasInternetAccess => true;
    }

    private static UnitOfWorkTestHost CreateHost() => UnitOfWorkTestHost.Create(services =>
    {
        services.RemoveAll<IMusicMetadataProvider>();   // drop the production (real-network) MusicBrainz registration
        services.AddSingleton<INetworkStatus, OnlineNetwork>();
        services.AddScoped<IMusicMetadataProvider>(_ => new MusicBrainzProvider(
            new HttpClient(new StubHandler()) { BaseAddress = new Uri("https://musicbrainz.org/ws/2/") },
            new MusicBrainzRateLimiter(),
            NullLogger<MusicBrainzProvider>.Instance));
    });

    // [AC] REQ-SFT-03/06: a title not stored locally still surfaces MusicBrainz options through the real stack.
    [Fact]
    public async Task SearchTitleRemoteAsync_EmptyLibrary_ReturnsMusicBrainzOptionsWithOkStatus()
    {
        await using var host = CreateHost();
        var sut = host.Resolve<ISongSuggestionService>();

        var result = await sut.SearchTitleRemoteAsync("commando", [], CancellationToken.None);

        Assert.Equal(RemoteSearchStatus.Ok, result.Status);
        Assert.Equal(3, result.Remote.Count);
        Assert.Contains(result.Remote, r => r.Title == "Commando" && r.ArtistName == "Niska");
        // [AC] REQ-RMV-08: a same-title row by another artist is its own group, not collapsed.
        Assert.Contains(result.Remote, r => r.Title == "Comando" && r.ArtistName == "Banda X");
        Assert.Contains(result.Remote, r => r.Title == "Chasse à l'homme");
    }

    // [AC] REQ-SFT-03/06: same, with a populated library (dedupe lookups run against real rows).
    [Fact]
    public async Task SearchTitleRemoteAsync_PopulatedLibrary_ReturnsMusicBrainzOptions()
    {
        await using var host = CreateHost();
        var (ok, msg, artist) = await host.Resolve<IArtistService>().CreateArtistAsync("Ramones");
        Assert.True(ok, msg);
        var (ok2, msg2, _) = await host.Resolve<ISongService>().CreateSongAsync(artist!.Id, "Commando", ct: CancellationToken.None);
        Assert.True(ok2, msg2);
        var sut = host.Resolve<ISongSuggestionService>();

        var result = await sut.SearchTitleRemoteAsync("commando", [], CancellationToken.None);

        Assert.Equal(RemoteSearchStatus.Ok, result.Status);
        Assert.NotEmpty(result.Remote);
    }

    // [AC] REQ-SFT-03/06: the Title field's option source (SongFormViewModel.TitleOptionSource) yields
    // MusicBrainz options through the real service stack, and sets no unavailable/offline note.
    [Fact]
    public async Task TitleOptionSource_RealStack_YieldsMusicBrainzOptionsAndNoNote()
    {
        await using var host = CreateHost();
        var vm = new SongFormViewModel(
            new Mock<IArtistService>().Object,
            new Mock<ISongService>().Object,
            new Mock<ISnackbarComponent>().Object,
            new Mock<ILogger<SongFormViewModel>>().Object,
            new Mock<ISongKaraokeUrlService>().Object,
            new Mock<IMessenger>().Object,
            new Mock<INavigationService>().Object,
            host.Resolve<ISongSuggestionService>(),
            TestImageStore.PassThrough().Object,
            new Mock<IPhotoPicker>().Object,
            new Mock<ISongLyricsService>().Object);
        vm.CompleteHydration();

        var options = await vm.TitleOptionSource.SearchAsync("commando", CancellationToken.None);

        Assert.Equal(3, options.Count);
        Assert.Equal("Commando", options[0].Headline);
        Assert.Equal(string.Empty, vm.TitleSupportingText);
    }
}
