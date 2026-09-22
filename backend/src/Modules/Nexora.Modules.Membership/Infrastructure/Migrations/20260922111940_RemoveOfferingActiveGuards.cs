using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Modules.Membership.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveOfferingActiveGuards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_TenantId_AccountId_ProductId",
                schema: "membership",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_TenantId_ContactId_ProductId",
                schema: "membership",
                table: "Subscriptions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
        }
    }
}
