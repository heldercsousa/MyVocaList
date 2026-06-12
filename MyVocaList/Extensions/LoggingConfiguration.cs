using Microsoft.Extensions.Configuration;
using Sentry;
using Serilog;
using Serilog.Events;

namespace MyVocaList.Extensions;

/// <summary>
/// Builds the application Serilog logger. Call <see cref="Build"/> once at startup
/// and pass the result to <c>builder.Services.AddSerilog()</c>.
/// </summary>
public static class LoggingConfiguration
{
    /// <summary>
    /// Builds and returns the configured Serilog logger.
    /// In release builds, also attaches the Sentry sink for Error/Fatal events.
    /// </summary>
    public static Serilog.Core.Logger Build(IConfiguration config)
    {
#if DEBUG
        var minimumLevel = LogEventLevel.Debug;
#else
        var minimumLevel = LogEventLevel.Warning;
#endif

        var logDirectory = Path.Combine(FileSystem.AppDataDirectory, "logs");
        Directory.CreateDirectory(logDirectory);
        var logFilePath = Path.Combine(logDirectory, "myvocalist-.log");

        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Is(minimumLevel)
            .MinimumLevel.Override("Microsoft.Maui", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithThreadId()
            .WriteTo.Debug(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: logFilePath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}");

#if !DEBUG
        var dsn = config["Sentry:Dsn"];
        if (!string.IsNullOrWhiteSpace(dsn))
        {
            // Compute session ID before Sentry SDK init — Preferences.Get requires the
            // Android Application context to be ready, which it is at this point.
            var sessionId = GetOrCreateSessionId();

            // Use the full named-parameter overload: SentrySerilogOptions only exposes
            // MinimumEventLevel / MinimumBreadcrumbLevel — Dsn, Release, Environment and
            // defaultTags must be passed as named arguments to this overload.
            // AttachScreenshot is SDK-default false; no explicit assignment is needed.
            loggerConfig.WriteTo.Sentry(
                dsn: dsn,
                release: AppInfo.VersionString,
                environment: "production",
                minimumBreadcrumbLevel: LogEventLevel.Information,
                minimumEventLevel: LogEventLevel.Error,
                defaultTags: new Dictionary<string, string>
                {
                    ["device.model"] = DeviceInfo.Model,
                    ["os.name"] = DeviceInfo.Platform.ToString(),
                    ["os.version"] = DeviceInfo.VersionString
                });

            // session_id is an extra (not a tag) — attach via the static SDK scope.
            SentrySdk.ConfigureScope(scope => scope.SetExtra("session_id", sessionId));
        }
#endif

        return loggerConfig.CreateLogger();
    }

    private static string GetOrCreateSessionId()
    {
        const string key = "session_id";
        var id = Preferences.Get(key, null);
        if (id == null)
        {
            id = Guid.NewGuid().ToString("N");
            Preferences.Set(key, id);
        }
        return id;
    }
}
