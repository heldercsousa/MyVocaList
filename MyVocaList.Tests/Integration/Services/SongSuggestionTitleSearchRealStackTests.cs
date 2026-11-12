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
/// Reproduces the remote title search end to end: real <see cref="DeezerProvider"/> over a fake HTTP
/// handler serving the real Deezer payload shape, real SQLite temp-file repositories, real
/// <see cref="SongSuggestionService"/> via the production composition.
/// </summary>
public class SongSuggestionTitleSearchRealStackTests
{
    // Real payload shape: unicode escape, title_version, extra unknown fields, top results not by the typed artist.
    private const string Payload = """
        {"data":[
         {"id":137258750,"readable":true,"title":"Commando","title_short":"Commando","title_version":"","isrc":"FR9W11622142","link":"https://www.deezer.com/track/137258750","duration":195,"rank":704927,"explicit_lyrics":true,"explicit_content_lyrics":1,"explicit_content_cover":1,"preview":"https://cdnt-preview.dzcdn.net/x.mp3?hdnea=abc","md5_image":"e953","artist":{"id":5288900,"name":"Niska","link":"l","picture":"p","type":"artist"},"album":{"id":14720756,"title":"Commando","cover":"c","md5_image":"m","type":"album"},"type":"track"},
         {"id":2,"readable":true,"title":"Comando (Ao Vivo)","title_short":"Comando","title_version":"(Ao Vivo)","artist":{"id":9,"name":"Banda X","type":"artist"},"album":{"id":1,"title":"A","type":"album"},"type":"track"},
         {"id":3,"readable":true,"title":"Chasse à l'homme","title_short":"Chasse à l'homme","title_version":"","artist":{"id":10,"name":"Zaz","type":"artist"},"type":"track"}
        ],"total":212,"next":"https://api.deezer.com/search/track?q=Commandi&index=25"}
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
        services.RemoveAll<IMusicMetadataProvider>();   // drop the production (real-network) Deezer registration
        services.AddSingleton<INetworkStatus, OnlineNetwork>();
        services.AddScoped<IMusicMetadataProvider>(_ => new DeezerProvider(
            new HttpClient(new StubHandler()) { BaseAddress = new Uri("https://api.deezer.com/") },
            NullLogger<DeezerProvider>.Instance));
    });

    // [AC] REQ-SFT-03/06: a title not stored locally still surfaces Deezer options through the real stack.
    [Fact]
    public async Task SearchTitleRemoteAsync_EmptyLibrary_ReturnsDeezerOptionsWithOkStatus()
    {
        await using var host = CreateHost();
        var sut = host.Resolve<ISongSuggestionService>();

        var result = await sut.SearchTitleRemoteAsync("commando", [], CancellationToken.None);

        Assert.Equal(RemoteSearchStatus.Ok, result.Status);
        Assert.Equal(3, result.Remote.Count);
        Assert.Contains(result.Remote, r => r.Title == "Commando" && r.ArtistName == "Niska");
        Assert.Contains(result.Remote, r => r.Title == "Comando" && r.Version == "Ao Vivo");
        Assert.Contains(result.Remote, r => r.Title == "Chasse à l'homme");
    }

    // [AC] REQ-SFT-03/06: same, with a populated library (dedupe lookups run against real rows).
    [Fact]
    public async Task SearchTitleRemoteAsync_PopulatedLibrary_ReturnsDeezerOptions()
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
    // Deezer options through the real service stack, and sets no unavailable/offline note.
    [Fact]
    public async Task TitleOptionSource_RealStack_YieldsDeezerOptionsAndNoNote()
    {
        await using var host = CreateHost();
        var vm = new SongFormViewModel(
            new Mock<IArtistService>().Object,
            new Mock<ISongService>().Object,
            new Mock<ISnackbarComponent>().Object,
            new Mock<ILogger<SongFormViewModel>>().Object,
            new Mock<ISongKaraokeUrlService>().Object,
            new Mock<ISecureStorageWrapper>().Object,
            new Mock<IMessenger>().Object,
            new Mock<INavigationService>().Object,
            host.Resolve<ISongSuggestionService>());
        vm.CompleteHydration();

        var options = await vm.TitleOptionSource.SearchAsync("commando", CancellationToken.None);

        Assert.Equal(3, options.Count);
        Assert.Equal("Commando", options[0].Headline);
        Assert.Equal(string.Empty, vm.TitleSupportingText);
    }
}
