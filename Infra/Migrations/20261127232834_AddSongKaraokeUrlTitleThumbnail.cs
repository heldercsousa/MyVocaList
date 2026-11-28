using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyVocaList.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddSongKaraokeUrlTitleThumbnail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ThumbnailUrl",
                table: "SongKaraokeUrls",
                type: "TEXT",
                maxLength: 500,
                nullable: true,
                collation: "NOCASE_NOACCENT");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "SongKaraokeUrls",
                type: "TEXT",
                maxLength: 300,
                nullable: true,
                collation: "NOCASE_NOACCENT");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ThumbnailUrl",
                table: "SongKaraokeUrls");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "SongKaraokeUrls");
        }
    }
}
