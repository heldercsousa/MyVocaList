using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace MyVocaList.Services;

/// <summary>
/// Extension methods for configuring Serilog logging in MAUI applications.
/// </summary>
public static class LoggingConfiguration
{
    /// <summary>
    /// Configure Serilog logging with appropriate sinks and log levels.
    /// </summary>
    /// <param name="builder">The MAUI app builder.</param>
    /// <returns>The builder for method chaining.</returns>
    public static MauiAppBuilder ConfigureSerilog(this MauiAppBuilder builder)
    {
        // Determine log level based on build configuration
#if DEBUG
        var minimumLevel = LogEventLevel.Debug;
#else
        var minimumLevel = LogEventLevel.Warning;
#endif

        // Configure log file path (rolling daily)
        var logDirectory = Path.Combine(FileSystem.AppDataDirectory, "logs");

        // Ensure log directory exists
        Directory.CreateDirectory(logDirectory);

        var logFilePath = Path.Combine(logDirectory, "myvocalist-.log");

        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(minimumLevel)

            // Override verbose frameworks to Warning level
            .MinimumLevel.Override("Microsoft.Maui", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)

            // Enrich logs with context
            .Enrich.FromLogContext()
            .Enrich.WithThreadId()

            // Debug sink (visible in IDE output)
            .WriteTo.Debug(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}")

            // File sink (rolling daily, 7 days retention)
            .WriteTo.File(
                path: logFilePath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}")

            .CreateLogger();

        // Integrate Serilog with Microsoft.Extensions.Logging
        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(dispose: true);

        Log.Information("Serilog configured. Log file: {LogFilePath}", logFilePath);

        return builder;
    }
}
