using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyVocaList.Infra.Migrations
{
    // REQ-RMV-03/04/05, INV-RMV-1/2 (design.md § Migration). Removes Songs.Version and Songs.FeaturedArtists and
    // makes (ArtistId, Title) the song identity. Before the identity index is created, colliding titles are renamed
    // (REQ-RMV-04): pass A appends ' (<trimmed Version>)' to non-keeper rows, pass B appends ' #<Id>' to any row that
    // still shares its identity with a lower-Id row. Titles are truncated so the final title is at most 250 characters.
    // Whitespace for "empty Version" and the suffix is the set {space, tab, LF, CR} (char(32, 9, 10, 13)).
    // Migration-wide: the rename SQL needs the NOCASE_NOACCENT collation registered (CollationInterceptor).

    /// <inheritdoc />
    public partial class RemoveSongVersionAndFeaturedArtists : Migration
    {
        private const string Ws = "char(32, 9, 10, 13)";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The 3-column index is dropped first: a rename can produce a (ArtistId, Title, Version) collision that the
            // old index would reject, and REQ-RMV-04 (e) forbids the migration failing because of duplicates.
            migrationBuilder.DropIndex(
                name: "IX_Songs_ArtistId_Title_Version",
                table: "Songs");

            // Pass A (rules a, b, d): every non-keeper row with a non-empty Version gets ' (<Version>)'.
            // Keeper per identity group: the lowest-Id row with an empty Version, else the lowest-Id row overall.
            migrationBuilder.Sql(
                "UPDATE \"Songs\" SET \"Title\" = SUBSTR(\"Title\", 1, 250 - LENGTH(' (' || TRIM(\"Version\", " + Ws + ") || ')')) " +
                "|| ' (' || TRIM(\"Version\", " + Ws + ") || ')' " +
                "WHERE TRIM(\"Version\", " + Ws + ") <> '' " +
                "AND EXISTS (SELECT 1 FROM \"Songs\" o WHERE o.\"ArtistId\" = \"Songs\".\"ArtistId\" AND o.\"Title\" = \"Songs\".\"Title\" AND o.\"Id\" <> \"Songs\".\"Id\") " +
                "AND \"Id\" <> COALESCE(" +
                "(SELECT MIN(k.\"Id\") FROM \"Songs\" k WHERE k.\"ArtistId\" = \"Songs\".\"ArtistId\" AND k.\"Title\" = \"Songs\".\"Title\" AND TRIM(k.\"Version\", " + Ws + ") = ''), " +
                "(SELECT MIN(k.\"Id\") FROM \"Songs\" k WHERE k.\"ArtistId\" = \"Songs\".\"ArtistId\" AND k.\"Title\" = \"Songs\".\"Title\"));");

            // Pass B (rules c, d): every row still sharing its identity with a lower-Id row gets ' #<Id>'.
            // Applied three times: one application resolves every collision except a pre-existing literal ' #<Id>'
            // title that is itself duplicated; the repetition covers such chains (a deeper chain fails the unique index below).
            for (var pass = 0; pass < 3; pass++)
            {
                migrationBuilder.Sql(
                    "UPDATE \"Songs\" SET \"Title\" = SUBSTR(\"Title\", 1, 250 - LENGTH(' #' || \"Id\")) || ' #' || \"Id\" " +
                    "WHERE EXISTS (SELECT 1 FROM \"Songs\" o WHERE o.\"ArtistId\" = \"Songs\".\"ArtistId\" AND o.\"Title\" = \"Songs\".\"Title\" AND o.\"Id\" < \"Songs\".\"Id\");");
            }

            // Native DROP COLUMN, not EF's DropColumn: EF rebuilds the table on SQLite (rename of __ef_temp_Songs), which
            // breaks the FTS triggers that reference Songs ("no such table: main.Songs"). The native statement keeps the
            // triggers and rows in place (same approach as the AddSongLyricsOrigin Down). No index references either column.
            migrationBuilder.Sql("ALTER TABLE \"Songs\" DROP COLUMN \"FeaturedArtists\";");
            migrationBuilder.Sql("ALTER TABLE \"Songs\" DROP COLUMN \"Version\";");

            migrationBuilder.CreateIndex(
                name: "IX_Songs_ArtistId_Title",
                table: "Songs",
                columns: new[] { "ArtistId", "Title" },
                unique: true);
        }

        /// <inheritdoc />
        /// <remarks>
        /// Lossy by design (REQ-RMV-05): FeaturedArtists and Version values are not restored (the columns come back
        /// empty / null), and the REQ-RMV-04 renames are not undone.
        /// </remarks>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Songs_ArtistId_Title",
                table: "Songs");

            migrationBuilder.AddColumn<string>(
                name: "FeaturedArtists",
                table: "Songs",
                type: "TEXT",
                nullable: true,
                collation: "NOCASE_NOACCENT");

            migrationBuilder.AddColumn<string>(
                name: "Version",
                table: "Songs",
                type: "TEXT",
                maxLength: 60,
                nullable: false,
                defaultValue: "",
                collation: "NOCASE_NOACCENT");

            migrationBuilder.CreateIndex(
                name: "IX_Songs_ArtistId_Title_Version",
                table: "Songs",
                columns: new[] { "ArtistId", "Title", "Version" },
                unique: true);
        }
    }
}
