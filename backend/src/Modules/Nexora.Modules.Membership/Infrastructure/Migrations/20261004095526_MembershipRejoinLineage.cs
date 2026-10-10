using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Modules.Membership.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MembershipRejoinLineage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PreviousMembershipId",
                schema: "membership",
                table: "Subscriptions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "RejoinedOn",
                schema: "membership",
                table: "Subscriptions",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PreviousMembershipId",
                schema: "membership",
                table: "Applications",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_TenantId_PreviousMembershipId",
                schema: "membership",
                table: "Subscriptions",
                columns: new[] { "TenantId", "PreviousMembershipId" },
                unique: true,
                filter: "[PreviousMembershipId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_TenantId_PreviousMembershipId",
                schema: "membership",
                table: "Applications",
                columns: new[] { "TenantId", "PreviousMembershipId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Applications_Subscriptions_TenantId_PreviousMembershipId",
                schema: "membership",
                table: "Applications",
                columns: new[] { "TenantId", "PreviousMembershipId" },
                principalSchema: "membership",
                principalTable: "Subscriptions",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Subscriptions_Subscriptions_TenantId_PreviousMembershipId",
                schema: "membership",
                table: "Subscriptions",
                columns: new[] { "TenantId", "PreviousMembershipId" },
                principalSchema: "membership",
                principalTable: "Subscriptions",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Applications_Subscriptions_TenantId_PreviousMembershipId",
                schema: "membership",
                table: "Applications");

            migrationBuilder.DropForeignKey(
                name: "FK_Subscriptions_Subscriptions_TenantId_PreviousMembershipId",
                schema: "membership",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_TenantId_PreviousMembershipId",
                schema: "membership",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Applications_TenantId_PreviousMembershipId",
                schema: "membership",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "PreviousMembershipId",
                schema: "membership",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "RejoinedOn",
                schema: "membership",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "PreviousMembershipId",
                schema: "membership",
                table: "Applications");
        }
    }
}
