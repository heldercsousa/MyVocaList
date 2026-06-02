using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyVocaList.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddSongKaraokeUrls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SongKaraokeUrls",
                columns: table => new
                {
                    VideoId = table.Column<string>(type: "TEXT", maxLength: 11, nullable: false, collation: "NOCASE_NOACCENT"),
                    SongId = table.Column<int>(type: "INTEGER", nullable: false),
                    PlayCount = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    DurationSeconds = table.Column<int>(type: "INTEGER", nullable: true),
                    LastUsedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AddedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    Label = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true, collation: "NOCASE_NOACCENT")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SongKaraokeUrls", x => new { x.SongId, x.VideoId });
                    table.ForeignKey(
                        name: "FK_SongKaraokeUrls_Songs_SongId",
                        column: x => x.SongId,
                        principalTable: "Songs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SongKaraokeUrls");
        }
    }
}
