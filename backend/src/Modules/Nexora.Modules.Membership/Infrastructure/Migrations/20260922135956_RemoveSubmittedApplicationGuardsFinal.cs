using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Modules.Membership.Infrastructure.Migrations;

public partial class RemoveSubmittedApplicationGuardsFinal : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // The preceding migration already removed these indexes on a clean replay.
        // Retain the repair for databases that still have them, without dropping twice.
        migrationBuilder.Sql("DROP INDEX IF EXISTS [IX_Applications_TenantId_AccountId_ProductId] ON [membership].[Applications];");
        migrationBuilder.Sql("DROP INDEX IF EXISTS [IX_Applications_TenantId_ContactId_ProductId] ON [membership].[Applications];");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // The immediately preceding schema also has neither index. Its own Down
        // restores them when rolling back past RemoveSubmittedApplicationGuards.
    }
}
