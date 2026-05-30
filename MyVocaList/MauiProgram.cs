using CommunityToolkit.Maui;
using Microsoft.EntityFrameworkCore;
using MyVocaList.Domain.RepositoryInterface;
using MyVocaList.Infra;
using MyVocaList.Infra.Interceptor;
using MyVocaList.Infra.Repository;
using MyVocaList.UI.Services;
#if DEBUG
using MauiDevFlow.Agent;
#endif

namespace MyVocaList;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseDevExpress(useLocalization: false)
            .UseDevExpressCollectionView()
            .UseDevExpressControls()
            .UseDevExpressEditors()
            .ConfigureFonts(fonts =>
            {
                // Roboto for Material Design 3
                fonts.AddFont("Roboto-Regular.ttf", "RobotoRegular");
                fonts.AddFont("Roboto-Medium.ttf", "RobotoMedium");
                fonts.AddFont("Roboto-Bold.ttf", "RobotoBold");
            });

        #if DEBUG
        builder.AddMauiDevFlowAgent();
        #endif

        // Database
        builder.Services.AddSingleton<CollationInterceptor>();
        builder.Services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "MyVocaList.db");
            options.UseSqlite($"Data Source={dbPath}")
                   .AddInterceptors(sp.GetRequiredService<CollationInterceptor>())
                   .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        });

        // Repositories
        builder.Services.AddScoped<IVenueRepository, VenueRepository>();
        builder.Services.AddScoped<IEventRepository, EventRepository>();
        builder.Services.AddScoped<IPersonRepository, PersonRepository>();
        builder.Services.AddScoped<IArtistRepository, ArtistRepository>();
        builder.Services.AddScoped<ISongRepository, SongRepository>();
        builder.Services.AddScoped<ICatalogRepository, CatalogRepository>();

        // HTTP Clients — music metadata providers
        builder.Services.AddHttpClient<MusicBrainzProvider>(client =>
        {
            client.BaseAddress = new Uri("https://musicbrainz.org/ws/2/");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("MyVocaList/1.0 (heldercsousa@gmail.com)");
        });
        builder.Services.AddHttpClient<DeezerProvider>(client =>
        {
            client.BaseAddress = new Uri("https://api.deezer.com/");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("MyVocaList/1.0 (heldercsousa@gmail.com)");
        });
        builder.Services.AddScoped<IMusicMetadataProvider, MusicBrainzProvider>();
        builder.Services.AddScoped<IMusicMetadataProvider, DeezerProvider>();

        // Services
        builder.Services.AddScoped<IVenueService, VenueService>();
        builder.Services.AddScoped<IPersonService, PersonService>();
        builder.Services.AddSingleton<ISnackbarComponent, SnackbarComponent>();
        builder.Services.AddScoped<IArtistService, ArtistService>();
        builder.Services.AddScoped<ISongService, SongService>();
        builder.Services.AddScoped<ICatalogService, CatalogService>();
        builder.Services.AddScoped<IMusicMetadataService, MusicMetadataService>();

        // YouTube Karaoke
        builder.Services.AddScoped<ISongKaraokeUrlRepository, SongKaraokeUrlRepository>();
        builder.Services.AddScoped<ISongKaraokeUrlService, SongKaraokeUrlService>();
        builder.Services.AddScoped<IYouTubeSearchService, YouTubeSearchService>();
        builder.Services.AddScoped<INextSingerAlertService, NextSingerAlertService>();
        builder.Services.AddSingleton<ISecureStorageWrapper, SecureStorageWrapper>();
        builder.Services.AddHttpClient();
#if ANDROID
        builder.Services.AddSingleton<IOverlayService, MyVocaList.Platforms.Android.Services.OverlayService>();
#else
        builder.Services.AddSingleton<IOverlayService, NoOpOverlayService>();
#endif

        // Shell
        builder.Services.AddSingleton<AppShellViewModel>();
        builder.Services.AddSingleton<AppShell>();

        // ViewModels
        builder.Services.AddTransient<VenuesViewModel>();
        builder.Services.AddTransient<VenueFormViewModel>();
        builder.Services.AddTransient<PersonsViewModel>();
        builder.Services.AddTransient<PersonFormViewModel>();
        builder.Services.AddTransient<ArtistsViewModel>();
        builder.Services.AddTransient<ArtistFormViewModel>();
        builder.Services.AddTransient<SongsViewModel>();
        builder.Services.AddTransient<SongFormViewModel>();

        // Pages
        builder.Services.AddTransient<VenueFormPage>();
        builder.Services.AddTransient<PersonFormPage>();
        builder.Services.AddTransient<QueuePage>();
        builder.Services.AddTransient<EventsPage>();
        builder.Services.AddTransient<VenuesPage>();
        builder.Services.AddTransient<PeoplePage>();
        builder.Services.AddTransient<ArtistsPage>();
        builder.Services.AddTransient<ArtistFormPage>();
        builder.Services.AddTransient<SongsPage>();
        builder.Services.AddTransient<SongFormPage>();
        builder.Services.AddTransient<PreferencesPage>();
        builder.Services.AddTransient<BackupRestorePage>();
        builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddTransient<SettingsPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
