using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Modules.Content.Migrations
{
    /// <inheritdoc />
    public partial class EntityDataAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MediaAssetsDataAudit",
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
                    table.PrimaryKey("PK_MediaAssetsDataAudit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PageRedirectsDataAudit",
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
                    table.PrimaryKey("PK_PageRedirectsDataAudit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PagesDataAudit",
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
                    table.PrimaryKey("PK_PagesDataAudit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PageVersionsDataAudit",
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
                    table.PrimaryKey("PK_PageVersionsDataAudit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PortalConfigurationsDataAudit",
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
                    table.PrimaryKey("PK_PortalConfigurationsDataAudit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PublicationsDataAudit",
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
                    table.PrimaryKey("PK_PublicationsDataAudit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScheduledPublicationsDataAudit",
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
                    table.PrimaryKey("PK_ScheduledPublicationsDataAudit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SitesDataAudit",
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
                    table.PrimaryKey("PK_SitesDataAudit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowActionReceiptsDataAudit",
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
                    table.PrimaryKey("PK_WorkflowActionReceiptsDataAudit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowOutboxDataAudit",
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
                    table.PrimaryKey("PK_WorkflowOutboxDataAudit", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssetsDataAudit_TenantId_EntityId_ChangedAtUtc",
                schema: "content",
                table: "MediaAssetsDataAudit",
                columns: new[] { "TenantId", "EntityId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssetsDataAudit_TenantId_ParentId_ChangedAtUtc",
                schema: "content",
                table: "MediaAssetsDataAudit",
                columns: new[] { "TenantId", "ParentId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PageRedirectsDataAudit_TenantId_EntityId_ChangedAtUtc",
                schema: "content",
                table: "PageRedirectsDataAudit",
                columns: new[] { "TenantId", "EntityId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PageRedirectsDataAudit_TenantId_ParentId_ChangedAtUtc",
                schema: "content",
                table: "PageRedirectsDataAudit",
                columns: new[] { "TenantId", "ParentId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PagesDataAudit_TenantId_EntityId_ChangedAtUtc",
                schema: "content",
                table: "PagesDataAudit",
                columns: new[] { "TenantId", "EntityId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PagesDataAudit_TenantId_ParentId_ChangedAtUtc",
                schema: "content",
                table: "PagesDataAudit",
                columns: new[] { "TenantId", "ParentId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PageVersionsDataAudit_TenantId_EntityId_ChangedAtUtc",
                schema: "content",
                table: "PageVersionsDataAudit",
                columns: new[] { "TenantId", "EntityId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PageVersionsDataAudit_TenantId_ParentId_ChangedAtUtc",
                schema: "content",
                table: "PageVersionsDataAudit",
                columns: new[] { "TenantId", "ParentId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PortalConfigurationsDataAudit_TenantId_EntityId_ChangedAtUtc",
                schema: "content",
                table: "PortalConfigurationsDataAudit",
                columns: new[] { "TenantId", "EntityId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PortalConfigurationsDataAudit_TenantId_ParentId_ChangedAtUtc",
                schema: "content",
                table: "PortalConfigurationsDataAudit",
                columns: new[] { "TenantId", "ParentId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PublicationsDataAudit_TenantId_EntityId_ChangedAtUtc",
                schema: "content",
                table: "PublicationsDataAudit",
                columns: new[] { "TenantId", "EntityId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PublicationsDataAudit_TenantId_ParentId_ChangedAtUtc",
                schema: "content",
                table: "PublicationsDataAudit",
                columns: new[] { "TenantId", "ParentId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledPublicationsDataAudit_TenantId_EntityId_ChangedAtUtc",
                schema: "content",
                table: "ScheduledPublicationsDataAudit",
                columns: new[] { "TenantId", "EntityId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledPublicationsDataAudit_TenantId_ParentId_ChangedAtUtc",
                schema: "content",
                table: "ScheduledPublicationsDataAudit",
                columns: new[] { "TenantId", "ParentId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SitesDataAudit_TenantId_EntityId_ChangedAtUtc",
                schema: "content",
                table: "SitesDataAudit",
                columns: new[] { "TenantId", "EntityId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SitesDataAudit_TenantId_ParentId_ChangedAtUtc",
                schema: "content",
                table: "SitesDataAudit",
                columns: new[] { "TenantId", "ParentId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowActionReceiptsDataAudit_TenantId_EntityId_ChangedAtUtc",
                schema: "content",
                table: "WorkflowActionReceiptsDataAudit",
                columns: new[] { "TenantId", "EntityId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowActionReceiptsDataAudit_TenantId_ParentId_ChangedAtUtc",
                schema: "content",
                table: "WorkflowActionReceiptsDataAudit",
                columns: new[] { "TenantId", "ParentId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowOutboxDataAudit_TenantId_EntityId_ChangedAtUtc",
                schema: "content",
                table: "WorkflowOutboxDataAudit",
                columns: new[] { "TenantId", "EntityId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowOutboxDataAudit_TenantId_ParentId_ChangedAtUtc",
                schema: "content",
                table: "WorkflowOutboxDataAudit",
                columns: new[] { "TenantId", "ParentId", "ChangedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MediaAssetsDataAudit",
                schema: "content");

            migrationBuilder.DropTable(
                name: "PageRedirectsDataAudit",
                schema: "content");

            migrationBuilder.DropTable(
                name: "PagesDataAudit",
                schema: "content");

            migrationBuilder.DropTable(
                name: "PageVersionsDataAudit",
                schema: "content");

            migrationBuilder.DropTable(
                name: "PortalConfigurationsDataAudit",
                schema: "content");

            migrationBuilder.DropTable(
                name: "PublicationsDataAudit",
                schema: "content");

            migrationBuilder.DropTable(
                name: "ScheduledPublicationsDataAudit",
                schema: "content");

            migrationBuilder.DropTable(
                name: "SitesDataAudit",
                schema: "content");

            migrationBuilder.DropTable(
                name: "WorkflowActionReceiptsDataAudit",
                schema: "content");

            migrationBuilder.DropTable(
                name: "WorkflowOutboxDataAudit",
                schema: "content");
        }
    }
}
