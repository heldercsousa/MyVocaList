using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyVocaList.Infra.Migrations
{
    /// <inheritdoc />
    public partial class RemoveNormalizedColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Songs_ArtistId_TitleNormalized",
                table: "Songs");

            migrationBuilder.DropIndex(
                name: "IX_Persons_FullNameNormalized",
                table: "People");

            migrationBuilder.DropIndex(
                name: "IX_Persons_Name_Birthday",
                table: "People");

            migrationBuilder.DropIndex(
                name: "IX_Artists_NameNormalized",
                table: "Artists");

            migrationBuilder.DropColumn(
                name: "TitleNormalized",
                table: "Songs");

            migrationBuilder.DropColumn(
                name: "FullNameNormalized",
                table: "People");

            migrationBuilder.DropColumn(
                name: "NameNormalized",
                table: "Artists");

            migrationBuilder.CreateIndex(
                name: "IX_Songs_ArtistId_Title",
                table: "Songs",
                columns: new[] { "ArtistId", "Title" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Persons_FullName",
                table: "People",
                column: "FullName");

            migrationBuilder.CreateIndex(
                name: "IX_Persons_Name_Birthday",
                table: "People",
                columns: new[] { "FullName", "BirthdayDayMonth" },
                unique: true,
                filter: "[BirthdayDayMonth] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Artists_Name",
                table: "Artists",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Songs_ArtistId_Title",
                table: "Songs");

            migrationBuilder.DropIndex(
                name: "IX_Persons_FullName",
                table: "People");

            migrationBuilder.DropIndex(
                name: "IX_Persons_Name_Birthday",
                table: "People");

            migrationBuilder.DropIndex(
                name: "IX_Artists_Name",
                table: "Artists");

            migrationBuilder.AddColumn<string>(
                name: "TitleNormalized",
                table: "Songs",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                collation: "NOCASE_NOACCENT");

            migrationBuilder.AddColumn<string>(
                name: "FullNameNormalized",
                table: "People",
                type: "TEXT",
                maxLength: 250,
                nullable: false,
                defaultValue: "",
                collation: "NOCASE_NOACCENT");

            migrationBuilder.AddColumn<string>(
                name: "NameNormalized",
                table: "Artists",
                type: "TEXT",
                maxLength: 60,
                nullable: false,
                defaultValue: "",
                collation: "NOCASE_NOACCENT");

            migrationBuilder.CreateIndex(
                name: "IX_Songs_ArtistId_TitleNormalized",
                table: "Songs",
                columns: new[] { "ArtistId", "TitleNormalized" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Persons_FullNameNormalized",
                table: "People",
                column: "FullNameNormalized");

            migrationBuilder.CreateIndex(
                name: "IX_Persons_Name_Birthday",
                table: "People",
                columns: new[] { "FullNameNormalized", "BirthdayDayMonth" },
                unique: true,
                filter: "[BirthdayDayMonth] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Artists_NameNormalized",
                table: "Artists",
                column: "NameNormalized",
                unique: true);
        }
    }
}
