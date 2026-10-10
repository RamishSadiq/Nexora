using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Modules.Crm.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ContactCommunicationPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "BouncedInvalidEmailAddress",
                schema: "crm",
                table: "ContactProfiles",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DateEmailBounced",
                schema: "crm",
                table: "ContactProfiles",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DateStopAllCommunication",
                schema: "crm",
                table: "ContactProfiles",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DisclosureDataToThirdParty",
                schema: "crm",
                table: "ContactProfiles",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "StopAllCommunication",
                schema: "crm",
                table: "ContactProfiles",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ThirdPartyDisclosureConsentDate",
                schema: "crm",
                table: "ContactProfiles",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ThirdPartyEmailPermission",
                schema: "crm",
                table: "ContactProfiles",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TypeOfThirdParty",
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
                name: "BouncedInvalidEmailAddress",
                schema: "crm",
                table: "ContactProfiles");

            migrationBuilder.DropColumn(
                name: "DateEmailBounced",
                schema: "crm",
                table: "ContactProfiles");

            migrationBuilder.DropColumn(
                name: "DateStopAllCommunication",
                schema: "crm",
                table: "ContactProfiles");

            migrationBuilder.DropColumn(
                name: "DisclosureDataToThirdParty",
                schema: "crm",
                table: "ContactProfiles");

            migrationBuilder.DropColumn(
                name: "StopAllCommunication",
                schema: "crm",
                table: "ContactProfiles");

            migrationBuilder.DropColumn(
                name: "ThirdPartyDisclosureConsentDate",
                schema: "crm",
                table: "ContactProfiles");

            migrationBuilder.DropColumn(
                name: "ThirdPartyEmailPermission",
                schema: "crm",
                table: "ContactProfiles");

            migrationBuilder.DropColumn(
                name: "TypeOfThirdParty",
                schema: "crm",
                table: "ContactProfiles");
        }
    }
}
