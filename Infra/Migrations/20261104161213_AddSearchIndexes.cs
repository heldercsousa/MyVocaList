using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyVocaList.Infra.Migrations
{
    // M3 (design.md § 6.2, REQ-SFT-19/30-34): FTS5 trigram SongSearch + ArtistSearch, backfill, seven sync triggers.
    // Any migration that rebuilds Songs or Artists must recreate these triggers in the same migration
    // (constraints-reference.md § EF Core / SQLite).
    // UseSqlReturningClause(false) on Songs/Artists is a model annotation only (no schema operation):
    // EF Core cannot use RETURNING on tables that have AFTER triggers.

    /// <inheritdoc />
    public partial class AddSearchIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "CREATE VIRTUAL TABLE SongSearch USING fts5(title, artist, tokenize='trigram remove_diacritics 1');");

            // rowid = Songs.Id. Backfill:
            migrationBuilder.Sql(
                @"INSERT INTO SongSearch(rowid, title, artist)
  SELECT s.Id, s.Title, a.Name FROM Songs s JOIN Artists a ON a.Id = s.ArtistId;");

            migrationBuilder.Sql(
                @"CREATE TRIGGER SongSearch_ai AFTER INSERT ON Songs BEGIN
  INSERT INTO SongSearch(rowid, title, artist)
  VALUES (new.Id, new.Title, (SELECT Name FROM Artists WHERE Id = new.ArtistId)); END;");

            migrationBuilder.Sql(
                @"CREATE TRIGGER SongSearch_au AFTER UPDATE OF Title, ArtistId ON Songs BEGIN
  DELETE FROM SongSearch WHERE rowid = old.Id;
  INSERT INTO SongSearch(rowid, title, artist)
  VALUES (new.Id, new.Title, (SELECT Name FROM Artists WHERE Id = new.ArtistId)); END;");

            migrationBuilder.Sql(
                @"CREATE TRIGGER SongSearch_ad AFTER DELETE ON Songs BEGIN
  DELETE FROM SongSearch WHERE rowid = old.Id; END;");

            migrationBuilder.Sql(
                @"CREATE TRIGGER SongSearch_artist_au AFTER UPDATE OF Name ON Artists BEGIN
  UPDATE SongSearch SET artist = new.Name
  WHERE rowid IN (SELECT Id FROM Songs WHERE ArtistId = new.Id); END;");

            // Artist near-duplicate retrieval (REQ-SFT-19). rowid = Artists.Id.
            migrationBuilder.Sql(
                "CREATE VIRTUAL TABLE ArtistSearch USING fts5(name, tokenize='trigram remove_diacritics 1');");

            migrationBuilder.Sql(
                "INSERT INTO ArtistSearch(rowid, name) SELECT Id, Name FROM Artists;");

            migrationBuilder.Sql(
                @"CREATE TRIGGER ArtistSearch_ai AFTER INSERT ON Artists BEGIN
  INSERT INTO ArtistSearch(rowid, name) VALUES (new.Id, new.Name); END;");

            migrationBuilder.Sql(
                @"CREATE TRIGGER ArtistSearch_au AFTER UPDATE OF Name ON Artists BEGIN
  DELETE FROM ArtistSearch WHERE rowid = old.Id;
  INSERT INTO ArtistSearch(rowid, name) VALUES (new.Id, new.Name); END;");

            migrationBuilder.Sql(
                @"CREATE TRIGGER ArtistSearch_ad AFTER DELETE ON Artists BEGIN
  DELETE FROM ArtistSearch WHERE rowid = old.Id; END;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS SongSearch_ai;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS SongSearch_au;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS SongSearch_ad;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS SongSearch_artist_au;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS ArtistSearch_ai;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS ArtistSearch_au;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS ArtistSearch_ad;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS SongSearch;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS ArtistSearch;");
        }
    }
}
