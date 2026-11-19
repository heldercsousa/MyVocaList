using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MyVocaList.Contracts.Enums;
using MyVocaList.Domain.Entity;
using MyVocaList.Infra;
using MyVocaList.Infra.Interceptor;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Integration.Migrations;

/// <summary>
/// Migration tests for <c>AddSongLyricsOrigin</c> (REQ-SLD-28, REQ-SLD-22). Real SQLite temp file via
/// <see cref="TestDbContextFactory"/>, never the in-memory provider. Temp databases are deleted on dispose.
/// </summary>
public class LyricsOriginMigrationTests : IAsyncDisposable
{
    private const string PreviousMigration = "20261114133332_AddVenueProfile";
    private const string ts = "'2026-01-01 00:00:00'";

    private readonly List<AppDbContext> _contexts = [];

    private async Task<AppDbContext> MigratedAsync()
    {
        var db = TestDbContextFactory.Create();
        _contexts.Add(db);
        await db.GetService<IMigrator>().MigrateAsync();
        return db;
    }

    private AppDbContext OpenFreshOnSameFile(AppDbContext source)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={source.Database.GetDbConnection().DataSource}")
            .AddInterceptors(new CollationInterceptor())
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .Options;
        var fresh = new AppDbContext(options);
        _contexts.Add(fresh);
        return fresh;
    }

    [Fact]
    public async Task LyricsOrigin_ColumnExistsNullableText()
    {
        // [AC] REQ-SLD-28: Songs.LyricsOrigin exists after the migration and is nullable
        var db = await MigratedAsync();

        var notNull = await db.Database.SqlQueryRaw<int>("SELECT \"notnull\" AS Value FROM pragma_table_info('Songs') WHERE name = 'LyricsOrigin'").SingleAsync();

        Assert.Equal(0, notNull);
    }

    [Fact]
    public async Task LyricsOrigin_SongStoredBeforeTheMigration_ReadsNull()
    {
        // [AC] REQ-SLD-28: a song stored before the migration has the origin "not recorded" (null)
        var db = TestDbContextFactory.Create();
        _contexts.Add(db);
        await db.GetService<IMigrator>().MigrateAsync(PreviousMigration);

        await db.Database.ExecuteSqlRawAsync($"INSERT INTO Artists (Id, Name, CreatedAt, UpdatedAt) VALUES (1, 'Metallica', {ts}, {ts})");
        await db.Database.ExecuteSqlRawAsync($"INSERT INTO Songs (Id, ArtistId, Title, Version, Lyrics, CreatedAt, UpdatedAt) VALUES (1, 1, 'One', '', 'Some lyrics', {ts}, {ts})");

        await db.GetService<IMigrator>().MigrateAsync();

        var storedSong = await db.Songs.AsNoTracking().SingleAsync();

        Assert.Null(storedSong.LyricsOrigin);
    }

    [Theory]
    [InlineData(LyricsOrigin.LyricsSource)]
    [InlineData(LyricsOrigin.User)]
    [InlineData(LyricsOrigin.NoneFound)]
    public async Task LyricsOrigin_EachValueRoundTrips(LyricsOrigin origin)
    {
        // [AC] REQ-SLD-22: a saved lyrics origin reads back equal from a fresh context, and the column stores the enum name
        var db = await MigratedAsync();
        var artist = new Artist { Name = "Metallica", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.Artists.Add(artist);
        await db.SaveChangesAsync();
        db.Songs.Add(new Song { ArtistId = artist.Id, Title = "One", LyricsOrigin = origin, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var rawText = await db.Database.SqlQueryRaw<string>("SELECT LyricsOrigin AS Value FROM Songs").SingleAsync();
        var fresh = OpenFreshOnSameFile(db);
        var storedSong = await fresh.Songs.AsNoTracking().SingleAsync();

        Assert.Equal(origin.ToString(), rawText);
        Assert.Equal(origin, storedSong.LyricsOrigin);
    }

    public async ValueTask DisposeAsync()
    {
        var paths = _contexts.Select(c => c.Database.GetDbConnection().DataSource).ToList();
        foreach (var c in _contexts) await c.DisposeAsync();
        SqliteConnection.ClearAllPools();
        foreach (var p in paths)
            if (File.Exists(p)) File.Delete(p);
        GC.SuppressFinalize(this);
    }
}
