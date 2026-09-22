using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Nexora.Modules.Identity.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NexoraIdentityDbContext))]
[Migration("20260918090000_ContentPermissions")]
public sealed class ContentPermissions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        INSERT INTO [identity].[RolePermissions] ([RoleId], [Permission])
        SELECT r.[Id], p.[Permission]
        FROM [identity].[Roles] r
        CROSS JOIN (VALUES ('content.read'), ('content.manage'), ('content.publish')) p([Permission])
        WHERE r.[IsSystem] = 1 AND r.[NormalizedName] = 'PLATFORM ADMINISTRATOR'
        AND NOT EXISTS (SELECT 1 FROM [identity].[RolePermissions] existing
                        WHERE existing.[RoleId] = r.[Id] AND existing.[Permission] = p.[Permission]);
        """);
    // Keep explicit role grants on rollback; older binaries do not use these permissions.
    protected override void Down(MigrationBuilder migrationBuilder) { }
}
