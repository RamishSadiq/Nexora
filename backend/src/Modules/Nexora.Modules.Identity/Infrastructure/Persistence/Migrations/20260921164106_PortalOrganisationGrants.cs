using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Modules.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PortalOrganisationGrants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_PortalAccess_TenantId_Id",
                schema: "identity",
                table: "PortalAccess",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateTable(
                name: "PortalOrganisationGrants",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PortalAccessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CanSubmitApplications = table.Column<bool>(type: "bit", nullable: false),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortalOrganisationGrants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PortalOrganisationGrants_PortalAccess_TenantId_PortalAccessId",
                        columns: x => new { x.TenantId, x.PortalAccessId },
                        principalSchema: "identity",
                        principalTable: "PortalAccess",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PortalOrganisationGrants_TenantId_PortalAccessId_AccountId",
                schema: "identity",
                table: "PortalOrganisationGrants",
                columns: new[] { "TenantId", "PortalAccessId", "AccountId" },
                unique: true,
                filter: "[IsRevoked] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PortalOrganisationGrants",
                schema: "identity");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_PortalAccess_TenantId_Id",
                schema: "identity",
                table: "PortalAccess");
        }
    }
}
