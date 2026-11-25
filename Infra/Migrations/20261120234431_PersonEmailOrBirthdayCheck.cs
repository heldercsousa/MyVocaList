using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyVocaList.Infra.Migrations
{
    /// <inheritdoc />
    public partial class PersonEmailOrBirthdayCheck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // BUG-090: intentionally a no-op. This migration used to add CK_Persons_EmailOrBirthday, which aborted on
            // legacy rows with neither email nor birthday. The constraint is dropped by PersonDropEmailOrBirthdayCheck
            // (REQ-DUP-14), so it is never created. Kept (not deleted) so devices that already applied it stay consistent.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op: see Up.
        }
    }
}
