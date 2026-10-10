using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Modules.Crm.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CommunicationPriority : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Priority",
                schema: "crm",
                table: "CommunicationMethods",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "other");

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationMethods_TenantId_RecordId_Kind_Priority",
                schema: "crm",
                table: "CommunicationMethods",
                columns: new[] { "TenantId", "RecordId", "Kind", "Priority" },
                unique: true,
                filter: "[Priority] IN ('primary', 'secondary')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CommunicationMethods_TenantId_RecordId_Kind_Priority",
                schema: "crm",
                table: "CommunicationMethods");

            migrationBuilder.DropColumn(
                name: "Priority",
                schema: "crm",
                table: "CommunicationMethods");

        }
    }
}
