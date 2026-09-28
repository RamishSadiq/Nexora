using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Modules.Crm.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CrmFileSafety : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ScanStatus",
                schema: "crm",
                table: "Files",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "quarantined");

            migrationBuilder.AddColumn<DateTime>(
                name: "ScannedAtUtc",
                schema: "crm",
                table: "Files",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sha256",
                schema: "crm",
                table: "Files",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "Version",
                schema: "crm",
                table: "Files",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ScanStatus",
                schema: "crm",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "ScannedAtUtc",
                schema: "crm",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "Sha256",
                schema: "crm",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "Version",
                schema: "crm",
                table: "Files");
        }
    }
}
