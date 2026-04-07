using Microsoft.EntityFrameworkCore;
using MyVocaList.Infra;
using MyVocaList.Infra.Interceptor;

namespace MyVocaList.Tests.Infrastructure;

/// <summary>Creates a fresh SQLite AppDbContext for each test run.</summary>
public static class TestDbContextFactory
{
    /// <summary>
    /// Creates an isolated SQLite AppDbContext backed by a unique temp file.
    /// Registers <see cref="CollationInterceptor"/> so NOCASE_NOACCENT collation and the
    /// accent-insensitive LIKE override are available — matching production behaviour.
    /// </summary>
    public static AppDbContext Create()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"myvocalist_test_{Guid.NewGuid():N}.db");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .AddInterceptors(new CollationInterceptor())
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .Options;

        return new AppDbContext(options);
    }
}
