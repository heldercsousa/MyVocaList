using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyVocaList.Infra.Migrations
{
    /// <inheritdoc />
    public partial class PersonBirthDayMonth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: add the two INTEGER columns (the old column is still present for the conversion below).
            migrationBuilder.AddColumn<int>(
                name: "BirthDay",
                table: "People",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BirthMonth",
                table: "People",
                type: "INTEGER",
                nullable: true);

            // Step 2 (REQ-BDAY-08): convert every 'DD/MM' or 'DDMM' value. Other shapes are left NULL.
            migrationBuilder.Sql(
                """
                UPDATE People
                SET BirthDay = CAST(substr(replace(BirthdayDayMonth, '/', ''), 1, 2) AS INTEGER),
                    BirthMonth = CAST(substr(replace(BirthdayDayMonth, '/', ''), 3, 2) AS INTEGER)
                WHERE length(replace(BirthdayDayMonth, '/', '')) = 4
                  AND replace(BirthdayDayMonth, '/', '') GLOB '[0-9][0-9][0-9][0-9]';
                """);

            // Step 3 (REQ-BDAY-08): a pair that is not a valid date for its month becomes NULL in both columns.
            // Mirrors PersonService.ValidateBirthday, so an edit of a migrated record never dead-ends.
            migrationBuilder.Sql(
                """
                UPDATE People
                SET BirthDay = NULL, BirthMonth = NULL
                WHERE BirthMonth IS NOT NULL
                  AND (BirthMonth < 1 OR BirthMonth > 12
                       OR BirthDay < 1 OR BirthDay > 31
                       OR (BirthMonth = 2 AND BirthDay > 29)
                       OR (BirthMonth IN (4, 6, 9, 11) AND BirthDay > 30));
                """);

            // Step 4: drop the index that covers the old column, then the old column itself.
            migrationBuilder.DropIndex(
                name: "IX_Persons_Name_Birthday",
                table: "People");

            migrationBuilder.DropColumn(
                name: "BirthdayDayMonth",
                table: "People");

            // Step 5: CHECK constraint. On SQLite this rebuilds the table.
            migrationBuilder.AddCheckConstraint(
                name: "CK_Persons_Birthday",
                table: "People",
                sql: "(BirthDay IS NULL AND BirthMonth IS NULL) OR (BirthDay IS NOT NULL AND BirthMonth IS NOT NULL AND BirthDay BETWEEN 1 AND 31 AND BirthMonth BETWEEN 1 AND 12)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Persons_Birthday",
                table: "People");

            migrationBuilder.DropColumn(
                name: "BirthDay",
                table: "People");

            migrationBuilder.DropColumn(
                name: "BirthMonth",
                table: "People");

            migrationBuilder.AddColumn<string>(
                name: "BirthdayDayMonth",
                table: "People",
                type: "TEXT",
                maxLength: 5,
                nullable: true,
                collation: "NOCASE_NOACCENT");

            migrationBuilder.CreateIndex(
                name: "IX_Persons_Name_Birthday",
                table: "People",
                columns: new[] { "FullName", "BirthdayDayMonth" },
                unique: true,
                filter: "[BirthdayDayMonth] IS NOT NULL");
        }
    }
}
