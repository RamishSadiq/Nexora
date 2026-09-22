using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Modules.Membership.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PortalOrganisationApplications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_TenantId_ContactId_ProductId",
                schema: "membership",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_PortalApplicationDrafts_TenantId_ContactId_ProductId",
                schema: "membership",
                table: "PortalApplicationDrafts");

            migrationBuilder.AddColumn<Guid>(
                name: "AccountId",
                schema: "membership",
                table: "PortalApplicationDrafts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_TenantId_AccountId_ProductId",
                schema: "membership",
                table: "Subscriptions",
                columns: new[] { "TenantId", "AccountId", "ProductId" },
                unique: true,
                filter: "[Status] = 'active' AND [AccountId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_TenantId_ContactId_ProductId",
                schema: "membership",
                table: "Subscriptions",
                columns: new[] { "TenantId", "ContactId", "ProductId" },
                unique: true,
                filter: "[Status] = 'active' AND [AccountId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PortalApplicationDrafts_TenantId_ContactId_AccountId_ProductId",
                schema: "membership",
                table: "PortalApplicationDrafts",
                columns: new[] { "TenantId", "ContactId", "AccountId", "ProductId" },
                unique: true,
                filter: "[ApplicationId] IS NULL AND [AccountId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PortalApplicationDrafts_TenantId_ContactId_ProductId",
                schema: "membership",
                table: "PortalApplicationDrafts",
                columns: new[] { "TenantId", "ContactId", "ProductId" },
                unique: true,
                filter: "[ApplicationId] IS NULL AND [AccountId] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_TenantId_AccountId_ProductId",
                schema: "membership",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_TenantId_ContactId_ProductId",
                schema: "membership",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_PortalApplicationDrafts_TenantId_ContactId_AccountId_ProductId",
                schema: "membership",
                table: "PortalApplicationDrafts");

            migrationBuilder.DropIndex(
                name: "IX_PortalApplicationDrafts_TenantId_ContactId_ProductId",
                schema: "membership",
                table: "PortalApplicationDrafts");

            migrationBuilder.DropColumn(
                name: "AccountId",
                schema: "membership",
                table: "PortalApplicationDrafts");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_TenantId_ContactId_ProductId",
                schema: "membership",
                table: "Subscriptions",
                columns: new[] { "TenantId", "ContactId", "ProductId" },
                unique: true,
                filter: "Status = 'active'");

            migrationBuilder.CreateIndex(
                name: "IX_PortalApplicationDrafts_TenantId_ContactId_ProductId",
                schema: "membership",
                table: "PortalApplicationDrafts",
                columns: new[] { "TenantId", "ContactId", "ProductId" },
                unique: true,
                filter: "[ApplicationId] IS NULL");
        }
    }
}
