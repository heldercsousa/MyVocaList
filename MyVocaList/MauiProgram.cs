using CommunityToolkit.Maui;
using DevExpress.Maui;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyVocaList.Infra.Data;
using MyVocaList.Infra.Data.Interceptors;
using MyVocaList.Infra.Data.Repositories;
using MyVocaList.Infra.Utils;
using MyVocaList.Services;
using MyVocaList.UI.Services;
using MyVocaList.UI.Pages.Artists;
using MyVocaList.UI.Pages.BackupRestore;
using MyVocaList.UI.Pages.Events;
using MyVocaList.UI.Pages.People;
using MyVocaList.UI.Pages.Preferences;
using MyVocaList.UI.Pages.Queue;
using MyVocaList.UI.Pages.Venues;
using MyVocaList.UI.ViewModels;

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

        // Database
        builder.Services.AddSingleton<CollationInterceptor>();
        builder.Services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "MyVocaList.db");
            options.UseSqlite($"Data Source={dbPath}")
                   .AddInterceptors(sp.GetRequiredService<CollationInterceptor>());
        });

        // Repositories
        builder.Services.AddScoped<IVenueRepository, VenueRepository>();
        builder.Services.AddScoped<IEventRepository, EventRepository>();

        // Utilities
        builder.Services.AddSingleton<ITextNormalizer, TextNormalizer>();

        // Services
        builder.Services.AddScoped<IDatabaseService, DatabaseService>();
        builder.Services.AddScoped<IVenueService, VenueService>();
        builder.Services.AddSingleton<ISnackbarService, SnackbarService>();

        // Shell
        builder.Services.AddSingleton<AppShellViewModel>();
        builder.Services.AddSingleton<AppShell>();

        // ViewModels
        builder.Services.AddTransient<VenuesViewModel>();

        // Pages
        builder.Services.AddTransient<QueuePage>();
        builder.Services.AddTransient<EventsPage>();
        builder.Services.AddTransient<VenuesPage>();
        builder.Services.AddTransient<PeoplePage>();
        builder.Services.AddTransient<ArtistsPage>();
        builder.Services.AddTransient<PreferencesPage>();
        builder.Services.AddTransient<BackupRestorePage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        var mauiApp = builder.Build();

        // Apply database migrations on startup - run on thread pool to avoid
        // blocking the main thread and SQLite write-lock contention between
        // EF Core's internal lock connection and migration connection.
        Task.Run(async () =>
        {
            using var scope = mauiApp.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.MigrateAsync();
        }).GetAwaiter().GetResult();

        return mauiApp;
    }
}