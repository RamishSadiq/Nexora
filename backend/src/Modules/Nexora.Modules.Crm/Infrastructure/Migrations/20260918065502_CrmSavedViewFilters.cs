using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Modules.Crm.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CrmSavedViewFilters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Columns",
                schema: "crm",
                table: "SavedViews",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                schema: "crm",
                table: "SavedViews",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerTeamId",
                schema: "crm",
                table: "SavedViews",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerUserId",
                schema: "crm",
                table: "SavedViews",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                schema: "crm",
                table: "SavedViews");

            migrationBuilder.DropColumn(
                name: "OwnerTeamId",
                schema: "crm",
                table: "SavedViews");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                schema: "crm",
                table: "SavedViews");

            migrationBuilder.AlterColumn<string>(
                name: "Columns",
                schema: "crm",
                table: "SavedViews",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000);
        }
    }
}
