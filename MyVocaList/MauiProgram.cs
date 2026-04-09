using CommunityToolkit.Maui;
using Microsoft.EntityFrameworkCore;
using MyVocaList.Domain.RepositoryInterface;
using MyVocaList.Infra;
using MyVocaList.Infra.Interceptor;
using MyVocaList.Infra.Repository;
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

        // Services
        builder.Services.AddScoped<IVenueService, VenueService>();
        builder.Services.AddScoped<IPersonService, PersonService>();
        builder.Services.AddSingleton<ISnackbarComponent, SnackbarComponent>();

        // Shell
        builder.Services.AddSingleton<AppShellViewModel>();
        builder.Services.AddSingleton<AppShell>();

        // ViewModels
        builder.Services.AddTransient<VenuesViewModel>();
        builder.Services.AddTransient<VenueFormViewModel>();

        // Pages
        builder.Services.AddTransient<VenueFormPage>();
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

        return builder.Build();
    }
}