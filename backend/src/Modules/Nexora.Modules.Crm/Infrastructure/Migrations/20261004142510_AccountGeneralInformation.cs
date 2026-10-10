using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Modules.Crm.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AccountGeneralInformation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccountType",
                schema: "crm",
                table: "AccountProfiles",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Branch",
                schema: "crm",
                table: "AccountProfiles",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmployeesRange",
                schema: "crm",
                table: "AccountProfiles",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IndustrySector",
                schema: "crm",
                table: "AccountProfiles",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MarketingSource",
                schema: "crm",
                table: "AccountProfiles",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NumberOfEmployees",
                schema: "crm",
                table: "AccountProfiles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OldReference",
                schema: "crm",
                table: "AccountProfiles",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Region",
                schema: "crm",
                table: "AccountProfiles",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceAccountId",
                schema: "crm",
                table: "AccountProfiles",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TradingStatus",
                schema: "crm",
                table: "AccountProfiles",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TurnoverRange",
                schema: "crm",
                table: "AccountProfiles",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Venue",
                schema: "crm",
                table: "AccountProfiles",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountType",
                schema: "crm",
                table: "AccountProfiles");

            migrationBuilder.DropColumn(
                name: "Branch",
                schema: "crm",
                table: "AccountProfiles");

            migrationBuilder.DropColumn(
                name: "EmployeesRange",
                schema: "crm",
                table: "AccountProfiles");

            migrationBuilder.DropColumn(
                name: "IndustrySector",
                schema: "crm",
                table: "AccountProfiles");

            migrationBuilder.DropColumn(
                name: "MarketingSource",
                schema: "crm",
                table: "AccountProfiles");

            migrationBuilder.DropColumn(
                name: "NumberOfEmployees",
                schema: "crm",
                table: "AccountProfiles");

            migrationBuilder.DropColumn(
                name: "OldReference",
                schema: "crm",
                table: "AccountProfiles");

            migrationBuilder.DropColumn(
                name: "Region",
                schema: "crm",
                table: "AccountProfiles");

            migrationBuilder.DropColumn(
                name: "SourceAccountId",
                schema: "crm",
                table: "AccountProfiles");

            migrationBuilder.DropColumn(
                name: "TradingStatus",
                schema: "crm",
                table: "AccountProfiles");

            migrationBuilder.DropColumn(
                name: "TurnoverRange",
                schema: "crm",
                table: "AccountProfiles");

            migrationBuilder.DropColumn(
                name: "Venue",
                schema: "crm",
                table: "AccountProfiles");
        }
    }
}
