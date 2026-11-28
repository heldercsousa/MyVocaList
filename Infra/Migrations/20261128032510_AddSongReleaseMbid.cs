using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyVocaList.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddSongReleaseMbid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReleaseMbid",
                table: "Songs",
                type: "TEXT",
                maxLength: 36,
                nullable: true,
                collation: "NOCASE_NOACCENT");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // EF's DropColumn rebuilds the table on SQLite, which fails while the search triggers reference Songs;
            // the native statement drops the column in place (no index, trigger or view references ReleaseMbid).
            migrationBuilder.Sql("ALTER TABLE \"Songs\" DROP COLUMN \"ReleaseMbid\";");
        }
    }
}
