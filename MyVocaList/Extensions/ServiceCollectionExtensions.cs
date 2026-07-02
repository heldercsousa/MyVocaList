using MyVocaList.Domain.RepositoryInterface;
using MyVocaList.Domain.ServicesInterfaces;
using MyVocaList.Infra.Repository;
using MyVocaList.Infra.Repositories;

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
        // Repositories
        services.AddScoped<IVenueRepository, VenueRepository>();
        services.AddScoped<Domain.RepositoryInterface.IEventRepository, Infra.Repository.EventRepository>();
        services.AddScoped<IPersonRepository, PersonRepository>();
        services.AddScoped<IArtistRepository, ArtistRepository>();
        services.AddScoped<ISongRepository, SongRepository>();
        services.AddScoped<ICatalogRepository, CatalogRepository>();

        // Queue Management repositories (new architecture)
        services.AddScoped<Domain.Interfaces.IEventRepository, Infra.Repositories.EventRepository>();
        services.AddScoped<Domain.Interfaces.IQueueRepository, Infra.Repositories.QueueRepository>();

        // HTTP Clients — music metadata providers
        services.AddHttpClient<MusicBrainzProvider>(client =>
        {
            client.BaseAddress = new Uri("https://musicbrainz.org/ws/2/");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("MyVocaList/1.0 (https://github.com/heldercsousa/myvocalist)");
        });
        services.AddHttpClient<DeezerProvider>(client =>
        {
            client.BaseAddress = new Uri("https://api.deezer.com/");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("MyVocaList/1.0 (https://github.com/heldercsousa/myvocalist)");
        });
        services.AddScoped<IMusicMetadataProvider, MusicBrainzProvider>();
        services.AddScoped<IMusicMetadataProvider, DeezerProvider>();

        // Services
        services.AddScoped<IVenueService, VenueService>();
        services.AddScoped<IPersonService, PersonService>();
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IQueueServiceNew, QueueServiceNew>();
        services.AddScoped<IArtistService, ArtistService>();
        services.AddScoped<ISongService, SongService>();
        services.AddScoped<ICatalogService, CatalogService>();
        services.AddScoped<IMusicMetadataService, MusicMetadataService>();
        // BUG-021 fix: ArtistResolutionService and SongResolutionService require ISimilarityScorer;
        // it was never registered, crashing SongFormPage activation at navigation time.
        services.AddScoped<ISimilarityScorer, Infra.Similarity.SimilarityScorer>();
        services.AddScoped<IArtistResolutionService, ArtistResolutionService>();
        services.AddScoped<ISongResolutionService, SongResolutionService>();

        // YouTube Karaoke
        services.AddScoped<ISongKaraokeUrlRepository, SongKaraokeUrlRepository>();
        services.AddScoped<ISongKaraokeUrlService, SongKaraokeUrlService>();
        services.AddScoped<IYouTubeSearchService, YouTubeSearchService>();
        services.AddScoped<INextSingerAlertService, NextSingerAlertService>();

        return services;
    }
}
