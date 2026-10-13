using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Modules.Content.Migrations
{
    /// <inheritdoc />
    public partial class WebsitePresentation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WebsiteConfigurations",
                schema: "content",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SiteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DraftJson = table.Column<string>(type: "nvarchar(max)", maxLength: 10000, nullable: false),
                    PublishedJson = table.Column<string>(type: "nvarchar(max)", maxLength: 10000, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebsiteConfigurations", x => x.Id);
                    table.UniqueConstraint("AK_WebsiteConfigurations_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_WebsiteConfigurations_Sites_TenantId_SiteId",
                        columns: x => new { x.TenantId, x.SiteId },
                        principalSchema: "content",
                        principalTable: "Sites",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WebsiteConfigurationsDataAudit",
                schema: "content",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EntityId = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OtherParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ChangedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Operation = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    ChangesJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebsiteConfigurationsDataAudit", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WebsiteConfigurations_TenantId_SiteId",
                schema: "content",
                table: "WebsiteConfigurations",
                columns: new[] { "TenantId", "SiteId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WebsiteConfigurationsDataAudit_TenantId_EntityId_ChangedAtUtc",
                schema: "content",
                table: "WebsiteConfigurationsDataAudit",
                columns: new[] { "TenantId", "EntityId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_WebsiteConfigurationsDataAudit_TenantId_ParentId_ChangedAtUtc",
                schema: "content",
                table: "WebsiteConfigurationsDataAudit",
                columns: new[] { "TenantId", "ParentId", "ChangedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WebsiteConfigurations",
                schema: "content");

            migrationBuilder.DropTable(
                name: "WebsiteConfigurationsDataAudit",
                schema: "content");
        }
    }
}
