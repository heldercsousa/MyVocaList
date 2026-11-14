using Microsoft.Extensions.DependencyInjection.Extensions;
using MyVocaList.Domain.RepositoryInterface;
using MyVocaList.Domain.UnitOfWork;
using MyVocaList.Infra.Repository;
using MyVocaList.Services.Places;

namespace MyVocaList.Extensions;

/// <summary>
/// Registers the platform-independent application services (repositories, HTTP metadata
/// providers, and business services). Extracted from <c>MauiProgram.CreateMauiApp</c> so the
/// registration graph can be verified by DI-resolution regression tests (BUG-021).
/// MAUI-platform-only registrations (DbContext paths, secure storage, pages, ViewModels,
/// Shell) remain in <c>MauiProgram.cs</c>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Adds repositories, music metadata HTTP providers, and business services.</summary>
    /// <param name="services">The service collection to register into.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        // Unit of Work — owns the short-lived AppDbContext per write (REQ-UOW-01). Registered here
        // (not in MauiProgram.cs) so every composition built from AddAppServices — production and
        // test harness alike — can activate the services that depend on it.
        services.AddSingleton<IUnitOfWork, MyVocaList.Infra.UnitOfWork.UnitOfWork>();

        // Repositories
        services.AddScoped<IVenueRepository, VenueRepository>();
        services.AddScoped<IPersonRepository, PersonRepository>();
        services.AddScoped<IArtistRepository, ArtistRepository>();
        services.AddScoped<ISongRepository, SongRepository>();
        services.AddScoped<ICatalogRepository, CatalogRepository>();

        // HTTP Clients — music metadata providers
        services.AddHttpClient<DeezerProvider>(client =>
        {
            client.BaseAddress = new Uri("https://api.deezer.com/");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("MyVocaList/1.0 (https://github.com/heldercsousa/myvocalist)");
        });
        // Resolve via the typed-client factory (sp.GetRequiredService<TProvider>()) instead of
        // AddScoped<IMusicMetadataProvider, TProvider>() — the latter constructs a second,
        // independent instance through ActivatorUtilities, bypassing the typed HttpClient pipeline
        // above and injecting an unconfigured HttpClient (no BaseAddress/User-Agent) (BUG-082).
        services.AddScoped<IMusicMetadataProvider>(sp => sp.GetRequiredService<DeezerProvider>());

        // Place lookup (venue map picker, REQ-VEN-25..28, 34). Same typed-client rule as Deezer above (BUG-082):
        // the provider is resolved through its typed HttpClient, never AddScoped<IGeocodingProvider, NominatimGeocodingProvider>().
        services.AddHttpClient<NominatimGeocodingProvider>(client =>
        {
            client.BaseAddress = new Uri("https://nominatim.openstreetmap.org/");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("MyVocaList/1.0 (https://github.com/heldercsousa/myvocalist)");
        });
        services.AddScoped<IGeocodingProvider>(sp => sp.GetRequiredService<NominatimGeocodingProvider>());
        // One gate for every scope: the usage rules (1 request per second, 100 remembered answers) are app-wide.
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<PlaceLookupGate>();
        services.AddScoped<IPlaceLookupService, PlaceLookupService>();

        // Venue picture from a map pick (REQ-VEN-38..40). Same typed-client rule as the Nominatim client above (BUG-082).
        services.AddHttpClient<VenuePictureLookupService>(client =>
        {
            client.BaseAddress = new Uri("https://www.wikidata.org/");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("MyVocaList/1.0 (https://github.com/heldercsousa/myvocalist)");
            client.Timeout = TimeSpan.FromMilliseconds(MyVocaList.Domain.Constants.SearchConstants.PlaceLookupTimeoutMilliseconds);
        });
        services.AddScoped<IVenuePictureLookupService>(sp => sp.GetRequiredService<VenuePictureLookupService>());

        // Picture download for the venue form (REQ-VEN-38, 39). Typed client, same rule as above; absolute addresses only.
        services.AddHttpClient<RemoteImageFetcher>(client =>
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd("MyVocaList/1.0 (https://github.com/heldercsousa/myvocalist)");
            client.Timeout = TimeSpan.FromMilliseconds(MyVocaList.Domain.Constants.SearchConstants.PlaceLookupTimeoutMilliseconds);
        });
        services.AddScoped<IRemoteImageFetcher>(sp => sp.GetRequiredService<RemoteImageFetcher>());

        // Lyrics source (LRCLIB). Same typed-client pattern as Deezer above (BUG-082): the interface
        // resolves through the typed-client factory, never AddScoped<ILyricsProvider, LrclibLyricsProvider>().
        services.AddHttpClient<LrclibLyricsProvider>(client =>
        {
            client.BaseAddress = new Uri("https://lrclib.net/");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("MyVocaList/1.0 (https://github.com/heldercsousa/myvocalist)");
        });
        services.AddScoped<ILyricsProvider>(sp => sp.GetRequiredService<LrclibLyricsProvider>());
        services.AddScoped<ISongLyricsService, SongLyricsService>();

        // Services
        services.AddScoped<IVenueService, VenueService>();
        services.AddScoped<IPersonService, PersonService>();
        services.AddScoped<IArtistService, ArtistService>();
        services.AddScoped<ISongService, SongService>();
        services.AddScoped<ICatalogService, CatalogService>();
        // BUG-021 fix: ArtistResolutionService and SongResolutionService require ISimilarityScorer;
        // it was never registered, crashing SongFormPage activation at navigation time.
        services.AddScoped<ISimilarityScorer, Infra.Similarity.SimilarityScorer>();
        services.AddScoped<IArtistResolutionService, ArtistResolutionService>();
        services.AddScoped<ISongResolutionService, SongResolutionService>();
        services.AddScoped<ISongSuggestionService, SongSuggestionService>();
        services.AddScoped<IArtistSuggestionService, ArtistSuggestionService>();

        // YouTube Karaoke
        services.AddScoped<ISongKaraokeUrlRepository, SongKaraokeUrlRepository>();
        services.AddScoped<ISongKaraokeUrlService, SongKaraokeUrlService>();
        services.AddScoped<IYouTubeSearchService, YouTubeSearchService>();
        services.AddScoped<INextSingerAlertService, NextSingerAlertService>();

        return services;
    }
}
