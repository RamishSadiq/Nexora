using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Modules.Membership.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PortalOrganisationProductAvailability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PortalOrganisationApplicationsEnabled",
                schema: "membership",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PortalOrganisationApplicationsEnabled",
                schema: "membership",
                table: "Products");
        }
    }
}
