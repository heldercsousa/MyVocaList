using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MyVocaList.Infra;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Integration.Migrations;

/// <summary>
/// Migration tests for the Song/Artist schema changes of the song-form title-first change (SFT).
/// M1 drops the HasManualEdits columns (REQ-SFT-37). Real SQLite temp file via
/// <see cref="TestDbContextFactory"/>, never the in-memory provider. Temp databases are deleted on dispose.
/// </summary>
public class SongMigrationTests : IAsyncDisposable
{
    private const string DropHasManualEditsName = "DropHasManualEdits";

    private readonly List<AppDbContext> _contexts = [];

    private async Task<AppDbContext> MigratedToAsync(string? target = null)
    {
        var db = TestDbContextFactory.Create();
        _contexts.Add(db);
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync(target);
        return db;
    }

    private static async Task<IReadOnlyList<string>> ColumnsAsync(AppDbContext db, string table)
        => await db.Database.SqlQueryRaw<string>($"SELECT name AS Value FROM pragma_table_info('{table}')").ToListAsync();

    private static async Task<bool> IsUniqueIndexAsync(AppDbContext db, string table, string indexName)
        => await db.Database.SqlQueryRaw<int>(
            $"SELECT \"unique\" AS Value FROM pragma_index_list('{table}') WHERE name = '{indexName}'")
            .SingleOrDefaultAsync() == 1;

    [Fact]
    public async Task M1_DropsHasManualEditsFromSongsAndArtists()
    {
        // [AC] REQ-SFT-37: HasManualEdits column no longer exists on Songs or Artists after migration
        var db = await MigratedToAsync();

        Assert.DoesNotContain("HasManualEdits", await ColumnsAsync(db, "Songs"));
        Assert.DoesNotContain("HasManualEdits", await ColumnsAsync(db, "Artists"));
    }

    [Fact]
    public async Task M1_ExistingRowsSurvive()
    {
        // [AC] REQ-SFT-37: dropping the columns keeps every existing artist and song row intact
        var db = TestDbContextFactory.Create();
        _contexts.Add(db);
        var migrations = db.Database.GetMigrations().ToList();
        var index = migrations.FindIndex(m => m.EndsWith("_" + DropHasManualEditsName, StringComparison.Ordinal));
        if (index < 1)
            throw new InvalidOperationException($"Migration '{DropHasManualEditsName}' not found (or has no predecessor).");
        var before = migrations[index - 1];

        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync(before);

        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO Artists (Id, Name, HasManualEdits, CreatedAt, UpdatedAt) VALUES (1, 'Metallica', 1, '2026-01-01 00:00:00', '2026-01-01 00:00:00')");
        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO Songs (Id, ArtistId, Title, Version, HasManualEdits, CreatedAt, UpdatedAt) VALUES (1, 1, 'One', '', 1, '2026-01-01 00:00:00', '2026-01-01 00:00:00')");
        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO Songs (Id, ArtistId, Title, Version, HasManualEdits, CreatedAt, UpdatedAt) VALUES (2, 1, 'One', 'Live', 0, '2026-01-01 00:00:00', '2026-01-01 00:00:00')");

        await migrator.MigrateAsync();

        var artists = await db.Database.SqlQueryRaw<string>("SELECT Id || '|' || Name AS Value FROM Artists").ToListAsync();
        var songs = await db.Database.SqlQueryRaw<string>(
            "SELECT Id || '|' || ArtistId || '|' || Title || '|' || Version AS Value FROM Songs ORDER BY Id").ToListAsync();
        Assert.Equal(["1|Metallica"], artists);
        Assert.Equal(["1|1|One|", "2|1|One|Live"], songs);
    }

    [Fact]
    public async Task M1_TableRebuild_KeepsUniqueCollatedIndexes()
    {
        // [AC] REQ-SFT-52: after the M1 table rebuild, Artists.Name stays unique under NOCASE_NOACCENT and the song identity index stays unique
        var db = await MigratedToAsync();

        Assert.True(await IsUniqueIndexAsync(db, "Artists", "IX_Artists_Name"));
        Assert.True(await IsUniqueIndexAsync(db, "Songs", "IX_Songs_ArtistId_Title_Version"));

        // Tolerates both schemas: the NOT NULL HasManualEdits column exists only until M1 is applied.
        var artistFlag = (await ColumnsAsync(db, "Artists")).Contains("HasManualEdits");
        var songFlag = (await ColumnsAsync(db, "Songs")).Contains("HasManualEdits");
        Task InsertArtist(int id, string name) => db.Database.ExecuteSqlRawAsync(
            $"INSERT INTO Artists (Id, Name, {(artistFlag ? "HasManualEdits, " : "")}CreatedAt, UpdatedAt) VALUES ({id}, '{name}', {(artistFlag ? "0, " : "")}'2026-01-01 00:00:00', '2026-01-01 00:00:00')");
        Task InsertSong(int id, string title) => db.Database.ExecuteSqlRawAsync(
            $"INSERT INTO Songs (Id, ArtistId, Title, Version, {(songFlag ? "HasManualEdits, " : "")}CreatedAt, UpdatedAt) VALUES ({id}, 1, '{title}', '', {(songFlag ? "0, " : "")}'2026-01-01 00:00:00', '2026-01-01 00:00:00')");

        await InsertArtist(1, "Metallica");
        await Assert.ThrowsAnyAsync<Exception>(() => InsertArtist(2, "metallica"));

        await InsertSong(1, "One");
        var ex = await Assert.ThrowsAnyAsync<Exception>(() => InsertSong(2, "one"));
        Assert.IsType<SqliteException>(ex is DbUpdateException ? ex.InnerException : ex);
    }

    private static async Task<AppDbContext> MigratedToLatestMinusAsync(AppDbContext db, string migrationName)
    {
        var migrations = db.Database.GetMigrations().ToList();
        if (!migrations.Any(m => m.EndsWith("_" + migrationName, StringComparison.Ordinal)))
            throw new InvalidOperationException($"Migration '{migrationName}' not found.");
        await db.GetService<IMigrator>().MigrateAsync(migrations.Single(m => m.EndsWith("_" + migrationName, StringComparison.Ordinal)));
        return db;
    }

    private static Task InsertArtistAsync(AppDbContext db, int id, string name) => db.Database.ExecuteSqlRawAsync(
        $"INSERT INTO Artists (Id, Name, CreatedAt, UpdatedAt) VALUES ({id}, '{name}', '2026-01-01 00:00:00', '2026-01-01 00:00:00')");

    private static Task InsertSongAsync(AppDbContext db, int id, string title, string? provider, string? externalId)
        => db.Database.ExecuteSqlRawAsync(
            "INSERT INTO Songs (Id, ArtistId, Title, Version, ExternalProvider, ExternalId, CreatedAt, UpdatedAt) VALUES " +
            $"({id}, 1, '{title}', '', {(provider is null ? "NULL" : $"'{provider}'")}, {(externalId is null ? "NULL" : $"'{externalId}'")}, '2026-01-01 00:00:00', '2026-01-01 00:00:00')");

    [Fact]
    public async Task M2_DuplicateIdentities_LowestIdKeepsIdentity_OthersClearedNotDeleted()
    {
        // [AC] REQ-SFT-39: duplicate (ExternalProvider, ExternalId) rows are deduplicated - lowest Id keeps the identity, the others are cleared, no row is deleted
        var db = TestDbContextFactory.Create();
        _contexts.Add(db);
        await MigratedToLatestMinusAsync(db, DropHasManualEditsName);

        await InsertArtistAsync(db, 1, "Metallica");
        await InsertSongAsync(db, 1, "One", "Deezer", "3135556");
        await InsertSongAsync(db, 2, "Two", "Deezer", "3135556");
        await InsertSongAsync(db, 3, "Three", "Deezer", "3135556");
        await InsertSongAsync(db, 4, "Four", "Deezer", "999");

        await db.GetService<IMigrator>().MigrateAsync();

        var rows = await db.Database.SqlQueryRaw<string>(
            "SELECT Id || '|' || ArtistId || '|' || Title || '|' || Version || '|' || IFNULL(ExternalProvider, 'null') || '|' || IFNULL(ExternalId, 'null') AS Value FROM Songs ORDER BY Id").ToListAsync();
        Assert.Equal(
            ["1|1|One||Deezer|3135556", "2|1|Two||null|null", "3|1|Three||null|null", "4|1|Four||Deezer|999"],
            rows);
    }

    [Fact]
    public async Task M2_UniqueIndex_RejectsSecondSongWithSameIdentity()
    {
        // [AC] REQ-SFT-39: after migration a second song with the same (ExternalProvider, ExternalId) is rejected by a UNIQUE constraint
        var db = await MigratedToAsync();
        await InsertArtistAsync(db, 1, "Metallica");
        await InsertSongAsync(db, 1, "One", "Deezer", "3135556");

        var ex = await Assert.ThrowsAnyAsync<Exception>(() => InsertSongAsync(db, 2, "Two", "Deezer", "3135556"));
        Assert.IsType<SqliteException>(ex is DbUpdateException ? ex.InnerException : ex);
    }

    [Fact]
    public async Task M2_NullIdentities_AllowMany()
    {
        // [AC] REQ-SFT-39: songs without an external identity (NULL, NULL) remain unrestricted by the unique index
        var db = await MigratedToAsync();
        await InsertArtistAsync(db, 1, "Metallica");

        await InsertSongAsync(db, 1, "One", null, null);
        await InsertSongAsync(db, 2, "Two", null, null);

        var count = await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM Songs").SingleAsync();
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task M2_IndexName_IsComposite()
    {
        // [AC] REQ-SFT-39: the external identity index is the composite unique IX_Songs_ExternalProvider_ExternalId and the single-column IX_Songs_ExternalId is gone
        var db = await MigratedToAsync();

        Assert.True(await IsUniqueIndexAsync(db, "Songs", "IX_Songs_ExternalProvider_ExternalId"));
        var names = await db.Database.SqlQueryRaw<string>("SELECT name AS Value FROM pragma_index_list('Songs')").ToListAsync();
        Assert.DoesNotContain("IX_Songs_ExternalId", names);
    }

    // ---- M3: FTS5 SongSearch + ArtistSearch, seven sync triggers, RETURNING off (REQ-SFT-34) ----

    private const string SongExternalIdentityUniqueName = "SongExternalIdentityUnique";

    private static readonly string[] FtsTriggers =
    [
        "SongSearch_ai", "SongSearch_au", "SongSearch_ad", "SongSearch_artist_au",
        "ArtistSearch_ai", "ArtistSearch_au", "ArtistSearch_ad",
    ];

    private static Task<int> CountAsync(AppDbContext db, string sql)
        => db.Database.SqlQueryRaw<int>($"SELECT COUNT(*) AS Value FROM ({sql})").SingleAsync();

    private static string MigrationId(AppDbContext db, string migrationName)
        => db.Database.GetMigrations().Single(m => m.EndsWith("_" + migrationName, StringComparison.Ordinal));

    private static MyVocaList.Domain.Entity.Artist NewArtist(string name)
        => new() { Name = name, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

    private static MyVocaList.Domain.Entity.Song NewSong(int artistId, string title)
        => new() { ArtistId = artistId, Title = title, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

    [Fact]
    public async Task FullMigrate_AllSevenFtsTriggersExist()
    {
        // [AC] REQ-SFT-34: after a full migrate, all seven FTS sync triggers exist (a table rebuild that drops them fails here).
        var db = await MigratedToAsync();
        var triggers = await db.Database
            .SqlQueryRaw<string>("SELECT name AS Value FROM sqlite_master WHERE type = 'trigger'")
            .ToListAsync();
        foreach (var name in FtsTriggers) Assert.Contains(name, triggers);
    }

    [Fact]
    public async Task FullMigrate_CreatesSongSearchAndArtistSearch()
    {
        // [AC] REQ-SFT-34: a full migrate creates the SongSearch and ArtistSearch FTS tables
        var db = await MigratedToAsync();
        var tables = await db.Database
            .SqlQueryRaw<string>("SELECT name AS Value FROM sqlite_master WHERE type = 'table'")
            .ToListAsync();
        Assert.Contains("SongSearch", tables);
        Assert.Contains("ArtistSearch", tables);
    }

    [Fact]
    public async Task M3_Backfill_OneRowPerSongAndArtist()
    {
        // [AC] REQ-SFT-34: migrating M3 over existing data backfills one SongSearch row per song and one ArtistSearch row per artist (INV-SFT-3)
        var db = TestDbContextFactory.Create();
        _contexts.Add(db);
        await MigratedToLatestMinusAsync(db, SongExternalIdentityUniqueName);

        await InsertArtistAsync(db, 1, "Metallica");
        await InsertArtistAsync(db, 2, "Oasis");
        const string ts = "'2026-01-01 00:00:00'";
        await db.Database.ExecuteSqlRawAsync(
            $"INSERT INTO Songs (Id, ArtistId, Title, Version, CreatedAt, UpdatedAt) VALUES (1, 1, 'One', '', {ts}, {ts}), (2, 1, 'Fuel', '', {ts}, {ts}), (3, 2, 'Wonderwall', '', {ts}, {ts})");

        await db.GetService<IMigrator>().MigrateAsync();

        Assert.Equal(3, await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM SongSearch").SingleAsync());
        Assert.Equal(2, await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM ArtistSearch").SingleAsync());
        var artist = await db.Database.SqlQueryRaw<string>("SELECT artist AS Value FROM SongSearch WHERE rowid = 3").SingleAsync();
        Assert.Equal("Oasis", artist);
    }

    [Fact]
    public async Task SongInsertViaEf_AddsSongSearchRow()
    {
        // [AC] REQ-SFT-34: an EF insert of a Song succeeds (RETURNING off) and the trigger adds its SongSearch row
        var db = await MigratedToAsync();
        var artist = NewArtist("Metallica");
        db.Artists.Add(artist);
        await db.SaveChangesAsync();
        var song = NewSong(artist.Id, "Enter Sandman");
        db.Songs.Add(song);
        await db.SaveChangesAsync();

        Assert.Equal(1, await CountAsync(db, $"SELECT rowid FROM SongSearch WHERE rowid = {song.Id}"));
    }

    [Fact]
    public async Task SongTitleUpdateViaEf_ReplacesSongSearchRow()
    {
        // [AC] REQ-SFT-34: an EF update of the title succeeds; the old title no longer matches and the new one does
        var db = await MigratedToAsync();
        var artist = NewArtist("Metallica");
        db.Artists.Add(artist);
        await db.SaveChangesAsync();
        var song = NewSong(artist.Id, "Enter Sandman");
        db.Songs.Add(song);
        await db.SaveChangesAsync();

        song.Title = "Nothing Else Matters";
        await db.SaveChangesAsync();

        Assert.Equal(0, await CountAsync(db, "SELECT rowid FROM SongSearch WHERE SongSearch MATCH '\"Sandman\"'"));
        Assert.Equal(1, await CountAsync(db, "SELECT rowid FROM SongSearch WHERE SongSearch MATCH '\"Matters\"'"));
    }

    [Fact]
    public async Task SongExecuteDelete_RemovesSongSearchRow()
    {
        // [AC] REQ-SFT-34: ExecuteDeleteAsync on Songs (bypasses change tracking) still removes the SongSearch row
        var db = await MigratedToAsync();
        await InsertArtistAsync(db, 1, "Metallica");
        await InsertSongAsync(db, 1, "One", null, null);
        Assert.Equal(1, await CountAsync(db, "SELECT rowid FROM SongSearch WHERE rowid = 1"));

        await db.Songs.Where(s => s.Id == 1).ExecuteDeleteAsync();

        Assert.Equal(0, await CountAsync(db, "SELECT rowid FROM SongSearch WHERE rowid = 1"));
    }

    [Fact]
    public async Task ArtistInsertViaEf_AddsArtistSearchRow()
    {
        // [AC] REQ-SFT-34: an EF insert of an Artist succeeds (RETURNING off) and the trigger adds its ArtistSearch row
        var db = await MigratedToAsync();
        var artist = NewArtist("Metallica");
        db.Artists.Add(artist);
        await db.SaveChangesAsync();

        Assert.Equal(1, await CountAsync(db, $"SELECT rowid FROM ArtistSearch WHERE rowid = {artist.Id}"));
    }

    [Fact]
    public async Task ArtistRenameViaEf_UpdatesSongSearchAndArtistSearch()
    {
        // [AC] REQ-SFT-34: renaming an artist via EF updates the name in ArtistSearch and in the artist column of its songs' SongSearch rows
        var db = await MigratedToAsync();
        var artist = NewArtist("Metallica");
        db.Artists.Add(artist);
        await db.SaveChangesAsync();
        var song = NewSong(artist.Id, "One");
        db.Songs.Add(song);
        await db.SaveChangesAsync();

        artist.Name = "Megadeth";
        await db.SaveChangesAsync();

        Assert.Equal("Megadeth", await db.Database.SqlQueryRaw<string>(
            $"SELECT artist AS Value FROM SongSearch WHERE rowid = {song.Id}").SingleAsync());
        Assert.Equal("Megadeth", await db.Database.SqlQueryRaw<string>(
            $"SELECT name AS Value FROM ArtistSearch WHERE rowid = {artist.Id}").SingleAsync());
    }

    [Fact]
    public async Task ArtistDelete_CascadeRemovesSongSearchRows()
    {
        // [AC] REQ-SFT-34: deleting an artist (FK cascade to its songs) removes the songs' SongSearch rows and the ArtistSearch row
        var db = await MigratedToAsync();
        var artist = NewArtist("Metallica");
        db.Artists.Add(artist);
        await db.SaveChangesAsync();
        db.Songs.Add(NewSong(artist.Id, "One"));
        db.Songs.Add(NewSong(artist.Id, "Fuel"));
        await db.SaveChangesAsync();
        Assert.Equal(2, await CountAsync(db, "SELECT rowid FROM SongSearch"));

        db.Artists.Remove(artist);
        await db.SaveChangesAsync();

        Assert.Equal(0, await CountAsync(db, "SELECT Id FROM Songs"));
        Assert.Equal(0, await CountAsync(db, "SELECT rowid FROM SongSearch"));
        Assert.Equal(0, await CountAsync(db, "SELECT rowid FROM ArtistSearch"));
    }

    [Fact]
    public async Task M3_Down_DropsTablesAndTriggers()
    {
        // [AC] REQ-SFT-34: rolling M3 back drops both FTS tables and all seven triggers
        var db = await MigratedToAsync();
        await db.GetService<IMigrator>().MigrateAsync(MigrationId(db, SongExternalIdentityUniqueName));

        var names = await db.Database
            .SqlQueryRaw<string>("SELECT name AS Value FROM sqlite_master WHERE name LIKE 'SongSearch%' OR name LIKE 'ArtistSearch%'")
            .ToListAsync();
        Assert.Empty(names);
        var triggers = await db.Database
            .SqlQueryRaw<string>("SELECT name AS Value FROM sqlite_master WHERE type = 'trigger'")
            .ToListAsync();
        foreach (var name in FtsTriggers) Assert.DoesNotContain(name, triggers);
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
