using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Modules.Membership.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OneActiveMembershipPerSubject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_TenantId_AccountId",
                schema: "membership",
                table: "Subscriptions",
                columns: new[] { "TenantId", "AccountId" },
                unique: true,
                filter: "[AccountId] IS NOT NULL AND [Status] = 'active'");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_TenantId_ContactId",
                schema: "membership",
                table: "Subscriptions",
                columns: new[] { "TenantId", "ContactId" },
                unique: true,
                filter: "[AccountId] IS NULL AND [Status] = 'active'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_TenantId_AccountId",
                schema: "membership",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_TenantId_ContactId",
                schema: "membership",
                table: "Subscriptions");
        }
    }
}
