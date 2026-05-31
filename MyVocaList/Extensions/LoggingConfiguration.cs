using Microsoft.Extensions.Configuration;
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
            loggerConfig.WriteTo.Sentry(o =>
            {
                o.Dsn = dsn;
                o.MinimumBreadcrumbLevel = LogEventLevel.Information;
                o.MinimumEventLevel = LogEventLevel.Error;
            });
        }
#endif

        return loggerConfig.CreateLogger();
    }
}
