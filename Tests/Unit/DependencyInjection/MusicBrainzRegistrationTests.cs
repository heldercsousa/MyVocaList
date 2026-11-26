using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyVocaList.Contracts.DTOs.List;
using MyVocaList.Extensions;
using MyVocaList.Infra;

namespace MyVocaList.Tests.Unit.DependencyInjection;

public class MusicBrainzRegistrationTests
{
    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];
        public ILogger CreateLogger(string categoryName) => new CapturingLogger(Entries);
        public void Dispose() { }

        private sealed class CapturingLogger(List<(LogLevel, string)> entries) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
                => entries.Add((logLevel, formatter(state, exception)));
        }
    }

    private static ServiceProvider Build(IConfiguration? config, CapturingLoggerProvider logs)
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(logs));
        services.AddDbContext<AppDbContext>(o => o.UseSqlite("Data Source=:memory:"));
        services.AddAppServices(config);
        return services.BuildServiceProvider();
    }

    [Fact]
    // [AC] REQ-MSRC-02: the only registered metadata provider is MusicBrainz
    public void AddAppServices_ResolvesMusicBrainzAsMetadataProvider()
    {
        using var provider = Build(null, new CapturingLoggerProvider());
        using var scope = provider.CreateScope();

        var providers = scope.ServiceProvider.GetServices<IMusicMetadataProvider>().ToList();

        var single = Assert.Single(providers);
        Assert.Equal("MusicBrainz", single.ProviderName);
    }

    [Fact]
    // [AC] REQ-MSRC-10: a missing UserAgent:Contact logs exactly one warning across several provider resolutions
    public void AddAppServices_MissingContact_LogsOneWarning()
    {
        var logs = new CapturingLoggerProvider();
        using var provider = Build(new ConfigurationBuilder().Build(), logs);
        for (var i = 0; i < 3; i++)
        {
            using var scope = provider.CreateScope();
            scope.ServiceProvider.GetServices<IMusicMetadataProvider>().ToList();
        }

        Assert.Single(logs.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("UserAgent:Contact"));
    }

    [Fact]
    // [AC] REQ-MSRC-10: a configured contact produces no warning
    public void AddAppServices_ConfiguredContact_NoWarning()
    {
        var logs = new CapturingLoggerProvider();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["UserAgent:Contact"] = "me@example.test" }).Build();
        using var provider = Build(config, logs);
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetServices<IMusicMetadataProvider>().ToList();

        Assert.DoesNotContain(logs.Entries, e => e.Level == LogLevel.Warning);
    }

    [Theory]
    [InlineData("MusicBrainz", "MB")]
    [InlineData("Deezer", "")]
    // [AC] REQ-MSRC-02: ArtistListItemDto_MusicBrainzProvider_ShowsMbBadge
    public void ArtistListItemDto_ProviderBadge(string provider, string expected)
        => Assert.Equal(expected, new ArtistListItemDto(1, "A", provider, 0).ProviderBadgeText);
}
