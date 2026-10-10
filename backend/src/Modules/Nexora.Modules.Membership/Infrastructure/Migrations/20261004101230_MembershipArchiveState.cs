using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Modules.Membership.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MembershipArchiveState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ArchiveReason",
                schema: "membership",
                table: "Subscriptions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAtUtc",
                schema: "membership",
                table: "Subscriptions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ArchivedByUserId",
                schema: "membership",
                table: "Subscriptions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                schema: "membership",
                table: "Subscriptions",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArchiveReason",
                schema: "membership",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "ArchivedAtUtc",
                schema: "membership",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "ArchivedByUserId",
                schema: "membership",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                schema: "membership",
                table: "Subscriptions");
        }
    }
}
