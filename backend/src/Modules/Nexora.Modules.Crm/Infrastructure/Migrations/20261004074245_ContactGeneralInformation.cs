using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Modules.Crm.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ContactGeneralInformation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Branch",
                schema: "crm",
                table: "ContactProfiles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactType",
                schema: "crm",
                table: "ContactProfiles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Nationality",
                schema: "crm",
                table: "ContactProfiles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OldReference",
                schema: "crm",
                table: "ContactProfiles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Region",
                schema: "crm",
                table: "ContactProfiles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Remarks",
                schema: "crm",
                table: "ContactProfiles",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourcePersonId",
                schema: "crm",
                table: "ContactProfiles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Branch",
                schema: "crm",
                table: "ContactProfiles");

            migrationBuilder.DropColumn(
                name: "ContactType",
                schema: "crm",
                table: "ContactProfiles");

            migrationBuilder.DropColumn(
                name: "Nationality",
                schema: "crm",
                table: "ContactProfiles");

            migrationBuilder.DropColumn(
                name: "OldReference",
                schema: "crm",
                table: "ContactProfiles");

            migrationBuilder.DropColumn(
                name: "Region",
                schema: "crm",
                table: "ContactProfiles");

            migrationBuilder.DropColumn(
                name: "Remarks",
                schema: "crm",
                table: "ContactProfiles");

            migrationBuilder.DropColumn(
                name: "SourcePersonId",
                schema: "crm",
                table: "ContactProfiles");
        }
    }
}
