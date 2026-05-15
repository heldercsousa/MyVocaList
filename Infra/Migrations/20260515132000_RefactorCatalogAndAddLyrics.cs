using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyVocaList.Infra.Migrations
{
    /// <inheritdoc />
    public partial class RefactorCatalogAndAddLyrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // App not in production — wipe existing data to avoid FK/unique constraint violations
            // during schema refactor (ArtistId NOT NULL stays, Catalog table added).
            migrationBuilder.Sql("DELETE FROM Songs;");
            migrationBuilder.Sql("DELETE FROM Artists;");

            migrationBuilder.AddColumn<string>(
                name: "Lyrics",
                table: "Songs",
                type: "TEXT",
                maxLength: 10000,
                nullable: true,
                collation: "NOCASE_NOACCENT");

            migrationBuilder.CreateTable(
                name: "Catalog",
                columns: table => new
                {
                    ArtistId = table.Column<int>(type: "INTEGER", nullable: false),
                    SongId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Catalog", x => new { x.ArtistId, x.SongId });
                    table.ForeignKey(
                        name: "FK_Catalog_Artists_ArtistId",
                        column: x => x.ArtistId,
                        principalTable: "Artists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Catalog_Songs_SongId",
                        column: x => x.SongId,
                        principalTable: "Songs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Catalog_SongId",
                table: "Catalog",
                column: "SongId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Catalog");

            migrationBuilder.DropColumn(
                name: "Lyrics",
                table: "Songs");
        }
    }
}
