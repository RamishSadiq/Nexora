using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Modules.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PortalAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PortalAccess",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContactId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortalAccess", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PortalAccess_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PortalAccess_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PortalAccess_TenantId_ContactId",
                schema: "identity",
                table: "PortalAccess",
                columns: new[] { "TenantId", "ContactId" },
                unique: true,
                filter: "[IsRevoked] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PortalAccess_TenantId_UserId",
                schema: "identity",
                table: "PortalAccess",
                columns: new[] { "TenantId", "UserId" },
                unique: true,
                filter: "[IsRevoked] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PortalAccess_UserId",
                schema: "identity",
                table: "PortalAccess",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PortalAccess",
                schema: "identity");
        }
    }
}
