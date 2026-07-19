using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyVocaList.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddArtistAndSongCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Artists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false, collation: "NOCASE_NOACCENT"),
                    NameNormalized = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false, collation: "NOCASE_NOACCENT"),
                    ExternalProvider = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true, collation: "NOCASE_NOACCENT"),
                    ExternalId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true, collation: "NOCASE_NOACCENT"),
                    HasManualEdits = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Artists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Songs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ArtistId = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false, collation: "NOCASE_NOACCENT"),
                    TitleNormalized = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false, collation: "NOCASE_NOACCENT"),
                    FeaturedArtists = table.Column<string>(type: "TEXT", nullable: true, collation: "NOCASE_NOACCENT"),
                    ExternalProvider = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true, collation: "NOCASE_NOACCENT"),
                    ExternalId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true, collation: "NOCASE_NOACCENT"),
                    HasManualEdits = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Songs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Songs_Artists_ArtistId",
                        column: x => x.ArtistId,
                        principalTable: "Artists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Artists_ExternalId",
                table: "Artists",
                column: "ExternalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Artists_NameNormalized",
                table: "Artists",
                column: "NameNormalized",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Songs_ArtistId_TitleNormalized",
                table: "Songs",
                columns: new[] { "ArtistId", "TitleNormalized" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Songs_ExternalId",
                table: "Songs",
                column: "ExternalId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Songs");

            migrationBuilder.DropTable(
                name: "Artists");
        }
    }
}
