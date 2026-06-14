using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyVocaList.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddSongVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Songs_ArtistId_Title",
                table: "Songs");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Songs_ArtistId_Title_Version",
                table: "Songs");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Songs");

            migrationBuilder.CreateIndex(
                name: "IX_Songs_ArtistId_Title",
                table: "Songs",
                columns: new[] { "ArtistId", "Title" },
                unique: true);
        }
    }
}
