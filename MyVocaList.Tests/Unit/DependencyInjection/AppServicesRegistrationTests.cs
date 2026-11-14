using System.Linq;
using System.Net.Http;
using System.Reflection;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using MyVocaList.Extensions;
using MyVocaList.Infra;
using MyVocaList.UI.Services;

namespace MyVocaList.Tests.Unit.DependencyInjection;

/// <summary>
/// DI-resolution regression tests for BUG-021 (SongsPage FAB crash):
/// "Unable to resolve service for type 'MyVocaList.Domain.ServicesInterfaces.ISimilarityScorer'
/// while attempting to activate 'MyVocaList.Services.ArtistResolutionService'."
/// Verifies the app's real registration graph (via ServiceCollectionExtensions.AddAppServices)
/// can activate every service in the SongFormViewModel dependency chain.
/// </summary>
public class AppServicesRegistrationTests
{
    private static ServiceProvider BuildProvider(Action<IServiceCollection>? extra = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        // Repositories need an AppDbContext registration; it is never queried here.
        services.AddDbContext<AppDbContext>(o => o.UseSqlite("Data Source=:memory:"));
        services.AddAppServices();
        extra?.Invoke(services);
        return services.BuildServiceProvider();
    }

    [Fact]
    // [BUG] BUG-021: FAB on SongsPage crashes — ISimilarityScorer not registered in DI
    public void AddAppServices_ResolvingArtistResolutionService_Succeeds()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IArtistResolutionService>();

        Assert.NotNull(service);
    }

    [Fact]
    // [BUG] BUG-021: the ISongResolutionService registration must resolve, and it needs ISimilarityScorer (SongFormViewModel no longer injects ISongResolutionService)
    public void AddAppServices_ResolvingSongResolutionService_Succeeds()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<ISongResolutionService>();

        Assert.NotNull(service);
    }

    [Fact]
    // [BUG] BUG-021: full SongFormViewModel dependency graph must activate (MAUI-platform-only deps mocked)
    public void AddAppServices_ResolvingSongFormViewModelGraph_Succeeds()
    {
        using var provider = BuildProvider(services =>
        {
            // MAUI-platform-only dependencies, registered in MauiProgram.cs — mocked here.
            services.AddSingleton(Mock.Of<ISnackbarComponent>());
            services.AddSingleton(Mock.Of<ISecureStorageWrapper>());
            services.AddSingleton<IMessenger>(new WeakReferenceMessenger());
            services.AddSingleton(Mock.Of<INavigationService>());
            // INetworkStatus is registered in MauiProgram.cs, not AddAppServices — mocked here (D21).
            services.AddSingleton(Mock.Of<INetworkStatus>());
            // IImageStore and IPhotoPicker are registered in MauiProgram.cs (platform paths, MediaPicker) — mocked here.
            services.AddSingleton(Mock.Of<IImageStore>());
            services.AddSingleton(Mock.Of<IPhotoPicker>());
            services.AddTransient<SongFormViewModel>();
        });
        using var scope = provider.CreateScope();

        var viewModel = scope.ServiceProvider.GetRequiredService<SongFormViewModel>();

        Assert.NotNull(viewModel);
    }

    [Fact]
    // [AC] REQ-SFT-02: the title search service resolves from AddAppServices
    public void AddAppServices_ResolvingSongSuggestionService_Succeeds()
    {
        using var provider = BuildProvider(services =>
        {
            // INetworkStatus is registered in MauiProgram.cs, not AddAppServices — mocked here (D21).
            services.AddSingleton(Mock.Of<INetworkStatus>());
        });
        using var scope = provider.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<ISongSuggestionService>();

        Assert.NotNull(service);
    }

    [Fact]
    // [AC] BUG-082: providers resolve with their configured typed HttpClient
    public void AddAppServices_ResolvingMusicMetadataProviders_HaveConfiguredHttpClient()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        var providers = scope.ServiceProvider.GetServices<IMusicMetadataProvider>().ToList();

        Assert.NotEmpty(providers);
        foreach (var metadataProvider in providers)
        {
            var httpClientField = metadataProvider.GetType()
                .GetField("_httpClient", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(httpClientField);

            var httpClient = (HttpClient)httpClientField!.GetValue(metadataProvider)!;

            Assert.NotNull(httpClient.BaseAddress);
            Assert.Contains("MyVocaList", httpClient.DefaultRequestHeaders.UserAgent.ToString());
        }
    }

    [Fact]
    // [AC] REQ-LYR-15: the LRCLIB client resolves with its base address and identified User-Agent; ISongLyricsService resolves
    public void AddAppServices_ResolvingLyricsProvider_HasConfiguredHttpClientAndSongLyricsService()
    {
        using var provider = BuildProvider(services =>
        {
            // INetworkStatus is registered in MauiProgram.cs, not AddAppServices — mocked here (D21).
            services.AddSingleton(Mock.Of<INetworkStatus>());
        });
        using var scope = provider.CreateScope();

        var lyricsProvider = scope.ServiceProvider.GetRequiredService<ILyricsProvider>();
        var httpClientField = typeof(LrclibLyricsProvider)
            .GetField("_httpClient", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(httpClientField);

        var httpClient = Assert.IsType<HttpClient>(httpClientField!.GetValue(lyricsProvider));
        Assert.Equal(new Uri("https://lrclib.net/"), httpClient.BaseAddress);
        Assert.Contains("MyVocaList/1.0", httpClient.DefaultRequestHeaders.UserAgent.ToString());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ISongLyricsService>());
    }
}
