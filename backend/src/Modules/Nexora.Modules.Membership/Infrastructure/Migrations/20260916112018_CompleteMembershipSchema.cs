using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Modules.Membership.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CompleteMembershipSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AccountId",
                schema: "membership",
                table: "Subscriptions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccountName",
                schema: "membership",
                table: "Subscriptions",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AccountId",
                schema: "membership",
                table: "Applications",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccountName",
                schema: "membership",
                table: "Applications",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApplicantNotes",
                schema: "membership",
                table: "Applications",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApplicantRole",
                schema: "membership",
                table: "Applications",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApplicationType",
                schema: "membership",
                table: "Applications",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "TermsAccepted",
                schema: "membership",
                table: "Applications",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "TermsAcceptedAtUtc",
                schema: "membership",
                table: "Applications",
                type: "datetime2",
                nullable: true);

            // AutomationConfigurations was already created by AutomationControllers.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountId",
                schema: "membership",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "AccountName",
                schema: "membership",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "AccountId",
                schema: "membership",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "AccountName",
                schema: "membership",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "ApplicantNotes",
                schema: "membership",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "ApplicantRole",
                schema: "membership",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "ApplicationType",
                schema: "membership",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "TermsAccepted",
                schema: "membership",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "TermsAcceptedAtUtc",
                schema: "membership",
                table: "Applications");
        }
    }
}
