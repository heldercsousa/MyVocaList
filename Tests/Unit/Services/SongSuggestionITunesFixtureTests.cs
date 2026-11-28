using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using MyVocaList.Contracts.DTOs;
using MyVocaList.Contracts.DTOs.Suggestions;
using MyVocaList.Domain.Constants;
using MyVocaList.Domain.ReadModels;
using MyVocaList.Domain.Resolution;
using MyVocaList.Infra.Similarity;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Unit.Services;

/// <summary>
/// Title suggestions driven by RECORDED iTunes responses (Tests/Fixtures/iTunes, captured 2026-11-28) through the real
/// ITunesSearchProvider and SongSuggestionService. Only the HTTP layer and the library repositories are stubbed.
/// </summary>
public class SongSuggestionITunesFixtureTests
{
    private const string FixtureFolder = "Fixtures/iTunes";

    private static readonly Dictionary<string, string> FixtureByTerm = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Don't Cry"] = "itunes-dont-cry.json",
        ["don't cry gun"] = "itunes-dont-cry-gun.json",
        ["Don't crie"] = "itunes-dont-crie.json",
        ["Don't"] = "itunes-dont.json",
        ["don't cry"] = "itunes-dont-cry-only.json",
    };

    private sealed class FixtureHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // The term is the last query parameter and is percent-encoded by ITunesSearchProvider.
            var term = Uri.UnescapeDataString(request.RequestUri!.Query.Split("term=", 2)[1]);
            var body = FixtureByTerm.TryGetValue(term, out var file)
                ? File.ReadAllText(Path.Combine(AppContext.BaseDirectory, FixtureFolder, file), Encoding.UTF8)
                : """{"resultCount":0,"results":[]}""";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
        }
    }

    private static SongSuggestionService CreateSut()
    {
        var songRepo = new Mock<ISongRepository>();
        var artistRepo = new Mock<IArtistRepository>();
        songRepo
            .Setup(r => r.FindByTitleAndArtistNamesAsync(
                It.IsAny<IReadOnlyCollection<(string Title, string ArtistName)>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SongSearchHit>());
        artistRepo
            .Setup(r => r.GetByNamesCollatedAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Artist>());

        var network = new Mock<INetworkStatus>();
        network.SetupGet(n => n.HasInternetAccess).Returns(true);

        var client = new HttpClient(new FixtureHandler()) { BaseAddress = new Uri("https://itunes.apple.com/") };
        var provider = new ITunesSearchProvider(client, new ITunesRequestBudget(TimeProvider.System), NullLogger<ITunesSearchProvider>.Instance);

        return new SongSuggestionService(
            songRepo.Object,
            artistRepo.Object,
            PassthroughUnitOfWork.Over(songRepo, artistRepo),
            [provider],
            new SimilarityScorer(),
            network.Object,
            NullLogger<SongSuggestionService>.Instance);
    }

    private static IEnumerable<(string Title, string Artist)> RecordedRows(string file)
    {
        using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, FixtureFolder, file)));
        foreach (var item in doc.RootElement.GetProperty("results").EnumerateArray())
            yield return (item.GetProperty("trackName").GetString()!, item.GetProperty("artistName").GetString()!);
    }

    [Fact]
    public async Task SearchTitleRemoteAsync_DontCry_FirstRowIsGunsNRoses()
    {
        // [AC] REQ-ITS-18 check 1: "Don't Cry" → first row "Don't Cry" by "Guns N' Roses" (recorded fixture)
        var result = await CreateSut().SearchTitleRemoteAsync("Don't Cry", []);

        var first = result.Remote[0];
        Assert.Equal("Don't Cry", first.Title);
        Assert.Equal("Guns N' Roses", first.ArtistName);
    }

    [Fact]
    public async Task SearchTitleRemoteAsync_DontCryGun_GunsNRosesFirst()
    {
        // [AC] REQ-ITS-18 check 2: "don't cry gun" → first row "Don't Cry" by "Guns N' Roses" (recorded fixture)
        var result = await CreateSut().SearchTitleRemoteAsync("don't cry gun", []);

        var first = result.Remote[0];
        Assert.Equal("Don't Cry", first.Title);
        Assert.Equal("Guns N' Roses", first.ArtistName);
    }

    [Fact]
    public async Task SearchTitleRemoteAsync_DontCrie_GunsNRosesInTopThree()
    {
        // [AC] REQ-ITS-18 check 3: "Don't crie" → a "Don't Cry" by "Guns N' Roses" row in the top 3 (recorded fixture)
        var result = await CreateSut().SearchTitleRemoteAsync("Don't crie", []);

        Assert.Contains(result.Remote.Take(3), r => r.Title == "Don't Cry" && r.ArtistName == "Guns N' Roses");
    }
}
