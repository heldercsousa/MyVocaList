using CommunityToolkit.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Serilog;
using MyVocaList.Domain.RepositoryInterface;
using MyVocaList.Domain.ServicesInterfaces;
using MyVocaList.Domain.Interfaces;
using MyVocaList.Extensions;
using MyVocaList.Infra;
using MyVocaList.Infra.Interceptor;
using MyVocaList.Infra.Repository;
using MyVocaList.Infra.Repositories;
using MyVocaList.Services;
using MyVocaList.UI.Services;
using CommunityToolkit.Mvvm.Messaging;
#if DEBUG
using MauiDevFlow.Agent;
#endif

namespace MyVocaList;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        // Load appsettings.json from embedded resource (optional — missing file or empty DSN is fine)
        var assembly = typeof(MauiProgram).Assembly;
        IConfiguration config = new ConfigurationBuilder().Build(); // empty fallback
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("appsettings.json", StringComparison.OrdinalIgnoreCase));
        if (resourceName != null)
        {
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream != null)
                config = new ConfigurationBuilder().AddJsonStream(stream).Build();
        }

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
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "MyVocaList.db");
        var backupDir = Path.Combine(FileSystem.AppDataDirectory, "backups");
        var logDir = Path.Combine(FileSystem.AppDataDirectory, "logs");

        builder.Services.AddSingleton<CollationInterceptor>();
        builder.Services.AddSingleton<ITransactionLogWriter>(_ => new TransactionLogWriter(logDir));
        builder.Services.AddSingleton<TransactionLogInterceptor>();
        builder.Services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.UseSqlite($"Data Source={dbPath}")
                   .AddInterceptors(
                       sp.GetRequiredService<CollationInterceptor>(),
                       sp.GetRequiredService<TransactionLogInterceptor>())
                   .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        });

        // Backup & Restore
        builder.Services.AddScoped<IBackupRepository, BackupRepository>();
        builder.Services.AddScoped<IBackupService>(sp => new BackupService(
            sp.GetRequiredService<IBackupRepository>(),
            sp.GetRequiredService<ITransactionLogWriter>(),
            sp.GetRequiredService<ILogger<BackupService>>(),
            dbPath,
            backupDir));
        builder.Services.AddTransient<BackupRestoreViewModel>();

        // Repositories, HTTP metadata providers, and business services
        // (extracted to a testable extension — see BUG-021 DI regression tests)
        builder.Services.AddAppServices();

        // MAUI-platform services
        builder.Services.AddSingleton<IPreferences>(_ => Preferences.Default);
        builder.Services.AddSingleton<IAppInfo>(_ => AppInfo.Current);
        builder.Services.AddSingleton<IFileSystem>(_ => FileSystem.Current);
        builder.Services.AddSingleton<IWhatsNewService, WhatsNewService>();

        // Version check — reads platform info at registration time (Singleton)
        builder.Services.AddHttpClient("version-check");
        builder.Services.AddSingleton<IVersionCheckService>(sp => new VersionCheckService(
            sp.GetRequiredService<IHttpClientFactory>(),
            AppInfo.Current.VersionString,
            DeviceInfo.Current.Platform == DevicePlatform.iOS ? "ios" : "android",
            sp.GetRequiredService<ILogger<VersionCheckService>>()));
        builder.Services.AddSingleton<ISnackbarComponent, SnackbarComponent>();
        builder.Services.AddSingleton<ISecureStorageWrapper, SecureStorageWrapper>();
        builder.Services.AddSingleton<INavigationService, MyVocaList.UI.Services.NavigationService>();
        builder.Services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
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
        builder.Services.AddTransient<ArtistPickerViewModel>();
        builder.Services.AddTransient<SongPickerViewModel>();
        builder.Services.AddTransient<YouTubeSearchViewModel>();
        builder.Services.AddTransient<QueueManagementViewModel>();
        builder.Services.AddTransient<PersonPickerViewModel>();
        builder.Services.AddTransient<QueueSongPickerViewModel>();

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
        builder.Services.AddTransient<ArtistPickerPage>();
        builder.Services.AddTransient<SongPickerPage>();
        builder.Services.AddTransient<YouTubeSearchPage>();
        builder.Services.AddTransient<BackupRestorePage>();
        builder.Services.AddTransient<QueueManagementPage>();
        builder.Services.AddTransient<PersonPickerPage>();
        builder.Services.AddTransient<QueueSongPickerPage>();
        builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<AboutViewModel>();
        builder.Services.AddTransient<AboutPage>();

        // Feedback
        builder.Services.AddTransient<IFeedbackService, FeedbackService>();
        builder.Services.AddTransient<FeedbackViewModel>();
        builder.Services.AddTransient<FeedbackPage>();
        builder.Services.AddHttpClient("feedback");
        builder.Services.AddSingleton<IDeviceInfo>(DeviceInfo.Current);
        builder.Services.AddSingleton<IAppInfo>(AppInfo.Current);

        // Register Serilog (file + debug sinks always; Sentry sink in release builds when DSN is set)
        // Assign Log.Logger so GlobalExceptionHandler's static Log.Fatal/Error calls are captured.
        var serilogLogger = LoggingConfiguration.Build(config);
        Log.Logger = serilogLogger;
        builder.Logging.AddSerilog(serilogLogger, dispose: true);

#if !DEBUG
        var sentryDsn = config["Sentry:Dsn"];
        if (!string.IsNullOrWhiteSpace(sentryDsn))
        {
            builder.UseSentry(options =>
            {
                options.Dsn = sentryDsn;
                options.Release = AppInfo.VersionString;
                options.Environment = "production";
                options.AttachScreenshot = false;
            });
        }
#endif
        return builder.Build();
    }

}
