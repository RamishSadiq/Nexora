using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Modules.Membership.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MembershipLifecycleEffectiveDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "ReinstatedOn",
                schema: "membership",
                table: "Subscriptions",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResignationReason",
                schema: "membership",
                table: "Subscriptions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ResignedOn",
                schema: "membership",
                table: "Subscriptions",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "StatusEffectiveOn",
                schema: "membership",
                table: "Subscriptions",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "EffectiveOn",
                schema: "membership",
                table: "History",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReinstatedOn",
                schema: "membership",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "ResignationReason",
                schema: "membership",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "ResignedOn",
                schema: "membership",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "StatusEffectiveOn",
                schema: "membership",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "EffectiveOn",
                schema: "membership",
                table: "History");
        }
    }
}
