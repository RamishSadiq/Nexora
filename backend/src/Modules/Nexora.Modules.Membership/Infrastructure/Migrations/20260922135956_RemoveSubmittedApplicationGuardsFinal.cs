using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Modules.Membership.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSubmittedApplicationGuardsFinal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Applications_TenantId_AccountId_ProductId",
                schema: "membership",
                table: "Applications");

            migrationBuilder.DropIndex(
                name: "IX_Applications_TenantId_ContactId_ProductId",
                schema: "membership",
                table: "Applications");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Applications_TenantId_AccountId_ProductId",
                schema: "membership",
                table: "Applications",
                columns: new[] { "TenantId", "AccountId", "ProductId" },
                unique: true,
                filter: "[AccountId] IS NOT NULL AND [Status] = 'submitted'");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_TenantId_ContactId_ProductId",
                schema: "membership",
                table: "Applications",
                columns: new[] { "TenantId", "ContactId", "ProductId" },
                unique: true,
                filter: "[AccountId] IS NULL AND [Status] = 'submitted'");
        }
    }
}
