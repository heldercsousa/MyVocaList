using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyVocaList.Infra.Migrations
{
    /// <inheritdoc />
    public partial class PersonConfigFixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_People_BirthdayDayMonth",
                table: "People");

            migrationBuilder.DropIndex(
                name: "IX_People_Email",
                table: "People");

            migrationBuilder.RenameIndex(
                name: "IX_People_FullNameNormalized",
                table: "People",
                newName: "IX_Persons_FullNameNormalized");

            migrationBuilder.AlterColumn<string>(
                name: "FullNameNormalized",
                table: "People",
                type: "TEXT",
                maxLength: 250,
                nullable: false,
                defaultValue: "",
                collation: "NOCASE_NOACCENT",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 250,
                oldNullable: true,
                oldCollation: "NOCASE_NOACCENT");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "People",
                type: "TEXT",
                maxLength: 100,
                nullable: true,
                collation: "NOCASE_NOACCENT",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldCollation: "NOCASE_NOACCENT");

            migrationBuilder.AlterColumn<string>(
                name: "BirthdayDayMonth",
                table: "People",
                type: "TEXT",
                maxLength: 5,
                nullable: true,
                collation: "NOCASE_NOACCENT",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldCollation: "NOCASE_NOACCENT");

            migrationBuilder.AddColumn<Guid>(
                name: "ExternalId",
                table: "People",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Persons_Email",
                table: "People",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Persons_ExternalId",
                table: "People",
                column: "ExternalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Persons_Name_Birthday",
                table: "People",
                columns: new[] { "FullNameNormalized", "BirthdayDayMonth" },
                unique: true,
                filter: "[BirthdayDayMonth] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Persons_Email",
                table: "People");

            migrationBuilder.DropIndex(
                name: "IX_Persons_ExternalId",
                table: "People");

            migrationBuilder.DropIndex(
                name: "IX_Persons_Name_Birthday",
                table: "People");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "People");

            migrationBuilder.RenameIndex(
                name: "IX_Persons_FullNameNormalized",
                table: "People",
                newName: "IX_People_FullNameNormalized");

            migrationBuilder.AlterColumn<string>(
                name: "FullNameNormalized",
                table: "People",
                type: "TEXT",
                maxLength: 250,
                nullable: true,
                collation: "NOCASE_NOACCENT",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 250,
                oldCollation: "NOCASE_NOACCENT");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "People",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                collation: "NOCASE_NOACCENT",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 100,
                oldNullable: true,
                oldCollation: "NOCASE_NOACCENT");

            migrationBuilder.AlterColumn<string>(
                name: "BirthdayDayMonth",
                table: "People",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                collation: "NOCASE_NOACCENT",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 5,
                oldNullable: true,
                oldCollation: "NOCASE_NOACCENT");

            migrationBuilder.CreateIndex(
                name: "IX_People_BirthdayDayMonth",
                table: "People",
                column: "BirthdayDayMonth");

            migrationBuilder.CreateIndex(
                name: "IX_People_Email",
                table: "People",
                column: "Email");
        }
    }
}
