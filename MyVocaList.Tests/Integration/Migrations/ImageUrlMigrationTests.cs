using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MyVocaList.Domain.Entity;
using MyVocaList.Infra;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Integration.Migrations;

/// <summary>
/// Migration tests for <c>AddImageUrlToSongsAndArtists</c> (REQ-ART-05). Real SQLite temp file via
/// <see cref="TestDbContextFactory"/>, never the in-memory provider. Temp databases are deleted on dispose.
/// </summary>
public class ImageUrlMigrationTests : IAsyncDisposable
{
    private readonly List<AppDbContext> _contexts = [];

    private async Task<AppDbContext> MigratedAsync()
    {
        var db = TestDbContextFactory.Create();
        _contexts.Add(db);
        await db.GetService<IMigrator>().MigrateAsync();
        return db;
    }

    private static async Task<IReadOnlyList<string>> ColumnsAsync(AppDbContext db, string table)
        => await db.Database.SqlQueryRaw<string>($"SELECT name AS Value FROM pragma_table_info('{table}')").ToListAsync();

    [Fact]
    public async Task ImageUrl_ColumnsExistOnSongsAndArtists()
    {
        // [AC] REQ-ART-05: Songs.ImageUrl and Artists.ImageUrl exist after the migration is applied
        var db = await MigratedAsync();

        Assert.Contains("ImageUrl", await ColumnsAsync(db, "Songs"));
        Assert.Contains("ImageUrl", await ColumnsAsync(db, "Artists"));
    }

    [Fact]
    public async Task ImageUrl_ColumnsAreNullableText()
    {
        // [AC] REQ-ART-05: both columns are nullable TEXT, so existing rows read back null
        var db = await MigratedAsync();

        var songType = await db.Database.SqlQueryRaw<string>("SELECT type AS Value FROM pragma_table_info('Songs') WHERE name = 'ImageUrl'").SingleAsync();
        var artistType = await db.Database.SqlQueryRaw<string>("SELECT type AS Value FROM pragma_table_info('Artists') WHERE name = 'ImageUrl'").SingleAsync();
        var songNotNull = await db.Database.SqlQueryRaw<int>("SELECT \"notnull\" AS Value FROM pragma_table_info('Songs') WHERE name = 'ImageUrl'").SingleAsync();
        var artistNotNull = await db.Database.SqlQueryRaw<int>("SELECT \"notnull\" AS Value FROM pragma_table_info('Artists') WHERE name = 'ImageUrl'").SingleAsync();

        Assert.Equal("TEXT", songType);
        Assert.Equal("TEXT", artistType);
        Assert.Equal(0, songNotNull);
        Assert.Equal(0, artistNotNull);
    }

    [Fact]
    public async Task ImageUrl_SongAndArtistSavedWithoutValue_ReadBackNull()
    {
        // [AC] REQ-ART-05: a song and an artist saved without an image URL read back null
        var db = await MigratedAsync();
        var artist = new Artist { Name = "Metallica", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.Artists.Add(artist);
        await db.SaveChangesAsync();
        db.Songs.Add(new Song { ArtistId = artist.Id, Title = "One", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var storedArtist = await db.Artists.AsNoTracking().SingleAsync();
        var storedSong = await db.Songs.AsNoTracking().SingleAsync();

        Assert.Null(storedArtist.ImageUrl);
        Assert.Null(storedSong.ImageUrl);
    }

    [Fact]
    public async Task ImageUrl_ValueRoundTripsOnSongAndArtist()
    {
        // [AC] REQ-ART-05: an image URL saved on a song and on an artist is read back unchanged
        const string songUrl = "https://cdn.example.com/cover/song.jpg";
        const string artistUrl = "https://cdn.example.com/artist/picture.jpg";
        var db = await MigratedAsync();
        var artist = new Artist { Name = "Metallica", ImageUrl = artistUrl, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.Artists.Add(artist);
        await db.SaveChangesAsync();
        db.Songs.Add(new Song { ArtistId = artist.Id, Title = "One", ImageUrl = songUrl, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var storedArtist = await db.Artists.AsNoTracking().SingleAsync();
        var storedSong = await db.Songs.AsNoTracking().SingleAsync();

        Assert.Equal(artistUrl, storedArtist.ImageUrl);
        Assert.Equal(songUrl, storedSong.ImageUrl);
    }

    [Fact]
    public async Task ImageUrl_RollbackToBeforeMigration_DropsColumnsKeepsTriggersAndRows()
    {
        // [AC] REQ-ART-05: rolling this migration back drops both ImageUrl columns, keeps the search triggers and keeps existing rows
        var db = await MigratedAsync();
        var migrations = db.Database.GetMigrations().ToList();
        var index = migrations.FindIndex(m => m.EndsWith("_AddImageUrlToSongsAndArtists", StringComparison.Ordinal));
        Assert.True(index > 0, "Migration 'AddImageUrlToSongsAndArtists' not found or has no predecessor.");

        const string ts = "'2026-01-01 00:00:00'";
        await db.Database.ExecuteSqlRawAsync($"INSERT INTO Artists (Id, Name, CreatedAt, UpdatedAt) VALUES (1, 'Metallica', {ts}, {ts})");
        await db.Database.ExecuteSqlRawAsync($"INSERT INTO Songs (Id, ArtistId, Title, Version, CreatedAt, UpdatedAt) VALUES (1, 1, 'One', '', {ts}, {ts})");

        await db.GetService<IMigrator>().MigrateAsync(migrations[index - 1]);

        Assert.DoesNotContain("ImageUrl", await ColumnsAsync(db, "Songs"));
        Assert.DoesNotContain("ImageUrl", await ColumnsAsync(db, "Artists"));

        var triggers = await db.Database.SqlQueryRaw<string>("SELECT name AS Value FROM sqlite_master WHERE type = 'trigger'").ToListAsync();
        Assert.Contains("SongSearch_artist_au", triggers);

        var songs = await db.Database.SqlQueryRaw<string>("SELECT Title AS Value FROM Songs").ToListAsync();
        var artists = await db.Database.SqlQueryRaw<string>("SELECT Name AS Value FROM Artists").ToListAsync();
        Assert.Equal(["One"], songs);
        Assert.Equal(["Metallica"], artists);
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
