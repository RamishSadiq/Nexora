using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Modules.Membership.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MembershipMemberTypeHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ClassificationChanges_Kind",
                schema: "membership",
                table: "ClassificationChanges");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ClassificationChanges_Kind",
                schema: "membership",
                table: "ClassificationChanges",
                sql: "[Kind] IN ('member-type', 'category', 'subscription-band')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ClassificationChanges_Kind",
                schema: "membership",
                table: "ClassificationChanges");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ClassificationChanges_Kind",
                schema: "membership",
                table: "ClassificationChanges",
                sql: "[Kind] IN ('category', 'subscription-band')");
        }
    }
}
