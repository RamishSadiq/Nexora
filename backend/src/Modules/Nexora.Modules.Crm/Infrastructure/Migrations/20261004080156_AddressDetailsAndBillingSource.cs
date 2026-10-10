using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Modules.Crm.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddressDetailsAndBillingSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "BillingUsesMailingAddress",
                schema: "crm",
                table: "Records",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AccountName",
                schema: "crm",
                table: "Addresses",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddressType",
                schema: "crm",
                table: "Addresses",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "County",
                schema: "crm",
                table: "Addresses",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JobTitle",
                schema: "crm",
                table: "Addresses",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Line3",
                schema: "crm",
                table: "Addresses",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Line4",
                schema: "crm",
                table: "Addresses",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Purpose",
                schema: "crm",
                table: "Addresses",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "other");

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_TenantId_RecordId_Purpose",
                schema: "crm",
                table: "Addresses",
                columns: new[] { "TenantId", "RecordId", "Purpose" },
                unique: true,
                filter: "[Purpose] IN ('mailing', 'billing')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Addresses_TenantId_RecordId_Purpose",
                schema: "crm",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "BillingUsesMailingAddress",
                schema: "crm",
                table: "Records");

            migrationBuilder.DropColumn(
                name: "AccountName",
                schema: "crm",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "AddressType",
                schema: "crm",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "County",
                schema: "crm",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "JobTitle",
                schema: "crm",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "Line3",
                schema: "crm",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "Line4",
                schema: "crm",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "Purpose",
                schema: "crm",
                table: "Addresses");
        }
    }
}
