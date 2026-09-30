using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Modules.Crm.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMainAccountSelection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsMainAccount",
                schema: "crm",
                table: "AccountContacts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_AccountContacts_TenantId_ContactId",
                schema: "crm",
                table: "AccountContacts",
                columns: new[] { "TenantId", "ContactId" },
                unique: true,
                filter: "[IsMainAccount] = 1 AND [EndDate] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AccountContacts_TenantId_ContactId",
                schema: "crm",
                table: "AccountContacts");

            migrationBuilder.DropColumn(
                name: "IsMainAccount",
                schema: "crm",
                table: "AccountContacts");
        }
    }
}
