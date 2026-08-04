using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyVocaList.Extensions;
using MyVocaList.Infra;
using MyVocaList.Infra.Interceptor;

namespace MyVocaList.Tests.Infrastructure;

/// <summary>
/// Real DI-composition test harness over a SQLite temp file, mirroring the production
/// composition (<see cref="ServiceCollectionExtensions.AddAppServices"/>) with a single
/// long-lived <see cref="IServiceScope"/> — the MAUI single-window scope this pattern models.
/// </summary>
public sealed class UnitOfWorkTestHost : IAsyncDisposable
{
    private readonly ServiceProvider _root;
    private readonly string _dbPath;

    public IServiceScope Scope { get; }
    public IServiceProvider Services => Scope.ServiceProvider;
    public AppDbContext Db => Resolve<AppDbContext>();
    public RecordingTransactionLogWriter Log { get; }
    public T Resolve<T>() where T : notnull => Services.GetRequiredService<T>();

    private UnitOfWorkTestHost(ServiceProvider root, string dbPath, RecordingTransactionLogWriter log)
    {
        _root = root; _dbPath = dbPath; Log = log; Scope = root.CreateScope();
    }

    /// <summary>Current production composition: one scoped AppDbContext for the whole session.</summary>
    public static UnitOfWorkTestHost CreateLegacy(Action<IServiceCollection>? customize = null)
    {
        var (services, dbPath, log) = BaseCollection();
        services.AddDbContext<AppDbContext>((sp, o) => Configure(sp, o, dbPath));
        return Build(services, dbPath, log, customize);
    }

    private static (ServiceCollection, string, RecordingTransactionLogWriter) BaseCollection()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"uow_test_{Guid.NewGuid():N}.db");
        var log = new RecordingTransactionLogWriter();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<CollationInterceptor>();
        services.AddSingleton<ITransactionLogWriter>(log);
        services.AddSingleton<TransactionLogInterceptor>();
        services.AddAppServices();
        return (services, dbPath, log);
    }

    private static void Configure(IServiceProvider sp, DbContextOptionsBuilder o, string dbPath) => o
        .UseSqlite($"Data Source={dbPath}")
        .AddInterceptors(
            sp.GetRequiredService<CollationInterceptor>(),
            sp.GetRequiredService<TransactionLogInterceptor>())
        .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

    private static UnitOfWorkTestHost Build(
        ServiceCollection services, string dbPath, RecordingTransactionLogWriter log,
        Action<IServiceCollection>? customize)
    {
        customize?.Invoke(services);   // last-wins override point, used by fault injection (Task 0.4)
        var host = new UnitOfWorkTestHost(services.BuildServiceProvider(), dbPath, log);
        host.Db.Database.EnsureCreated();
        return host;
    }

    public async ValueTask DisposeAsync()
    {
        await Db.Database.EnsureDeletedAsync();
        Scope.Dispose();
        await _root.DisposeAsync();
        try { File.Delete(_dbPath); } catch (IOException) { /* temp file, best effort */ }
    }
}

/// <summary>Captures transaction-log entries in memory so REQ-UOW-14/15 can assert on them.</summary>
public sealed class RecordingTransactionLogWriter : ITransactionLogWriter
{
    public List<string> Entries { get; } = [];

    public string CurrentSessionLogPath { get; } = Path.Combine(Path.GetTempPath(), $"uow_test_log_{Guid.NewGuid():N}.log");

    public Task AppendAsync(LogEntry entry, CancellationToken ct)
    {
        Entries.Add($"{entry.Ts:O}|{entry.Op}|{entry.Entity}|{entry.Id}|{entry.Before}|{entry.After}");
        return Task.CompletedTask;
    }

    public Task PruneLogsOlderThanAsync(DateTime snapshotTs, CancellationToken ct) => Task.CompletedTask;
}
