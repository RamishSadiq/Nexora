using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Modules.Membership.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MembershipAdministrativeDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LegacyMemberReference",
                schema: "membership",
                table: "Subscriptions",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MembershipPackSent",
                schema: "membership",
                table: "Subscriptions",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "MembershipPackSentDate",
                schema: "membership",
                table: "Subscriptions",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Remarks",
                schema: "membership",
                table: "Subscriptions",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LegacyMemberReference",
                schema: "membership",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "MembershipPackSent",
                schema: "membership",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "MembershipPackSentDate",
                schema: "membership",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "Remarks",
                schema: "membership",
                table: "Subscriptions");
        }
    }
}
