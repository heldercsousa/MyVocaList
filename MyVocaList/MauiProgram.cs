using CommunityToolkit.Maui;
using Microsoft.EntityFrameworkCore;
using MyVocaList.Domain;
using MyVocaList.Domain.RepositoryInterface;
using MyVocaList.Infra;
using MyVocaList.Infra.Interceptor;
using MyVocaList.Infra.Repository;

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

        // Services
        builder.Services.AddScoped<IDatabaseInit, DatabaseInit>();
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

        // Apply database migrations on startup.
        // Clear any stale __EFMigrationsLock row left by a previous crashed session before
        // calling MigrateAsync(); otherwise EF Core 9 spins forever trying to acquire the lock.
        // SQLite on mobile is single-user, so no concurrent migration concern.
        Task.Run(async () =>
        {
            using var scope = mauiApp.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            try
            {
                await dbContext.Database.ExecuteSqlRawAsync("DELETE FROM __EFMigrationsLock");
            }
            catch { /* Table does not exist on first run — safe to ignore. */ }

            await dbContext.Database.MigrateAsync();
        }).GetAwaiter().GetResult();

        return mauiApp;
    }
}