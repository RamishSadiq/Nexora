using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Modules.Membership.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PortalApplicationDrafts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PortalApplicationsEnabled",
                schema: "membership",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PortalTerms",
                schema: "membership",
                table: "Products",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "PortalApplicationDrafts",
                schema: "membership",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContactId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartsAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ProductVersion = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PolicyVersion = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Terms = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortalApplicationDrafts", x => x.Id);
                    table.UniqueConstraint("AK_PortalApplicationDrafts_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_PortalApplicationDrafts_Products_TenantId_ProductId",
                        columns: x => new { x.TenantId, x.ProductId },
                        principalSchema: "membership",
                        principalTable: "Products",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PortalApplicationDrafts_TenantId_ContactId",
                schema: "membership",
                table: "PortalApplicationDrafts",
                columns: new[] { "TenantId", "ContactId" });

            migrationBuilder.CreateIndex(
                name: "IX_PortalApplicationDrafts_TenantId_ContactId_ProductId",
                schema: "membership",
                table: "PortalApplicationDrafts",
                columns: new[] { "TenantId", "ContactId", "ProductId" },
                unique: true,
                filter: "[ApplicationId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PortalApplicationDrafts_TenantId_ProductId",
                schema: "membership",
                table: "PortalApplicationDrafts",
                columns: new[] { "TenantId", "ProductId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PortalApplicationDrafts",
                schema: "membership");

            migrationBuilder.DropColumn(
                name: "PortalApplicationsEnabled",
                schema: "membership",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "PortalTerms",
                schema: "membership",
                table: "Products");
        }
    }
}
