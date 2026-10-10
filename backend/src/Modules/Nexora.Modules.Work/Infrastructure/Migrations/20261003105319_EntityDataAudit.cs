using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Modules.Work.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EntityDataAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AttributeDefinitionsDataAudit",
                schema: "work",
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
                    table.PrimaryKey("PK_AttributeDefinitionsDataAudit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AttributeValuesDataAudit",
                schema: "work",
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
                    table.PrimaryKey("PK_AttributeValuesDataAudit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AutomationConfigurationsDataAudit",
                schema: "work",
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
                    table.PrimaryKey("PK_AutomationConfigurationsDataAudit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HistoryDataAudit",
                schema: "work",
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
                    table.PrimaryKey("PK_HistoryDataAudit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImportBatchesDataAudit",
                schema: "work",
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
                    table.PrimaryKey("PK_ImportBatchesDataAudit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImportRowsDataAudit",
                schema: "work",
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
                    table.PrimaryKey("PK_ImportRowsDataAudit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ItemsDataAudit",
                schema: "work",
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
                    table.PrimaryKey("PK_ItemsDataAudit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotificationsDataAudit",
                schema: "work",
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
                    table.PrimaryKey("PK_NotificationsDataAudit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowActionReceiptsDataAudit",
                schema: "work",
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
                schema: "work",
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
                name: "IX_AttributeDefinitionsDataAudit_TenantId_EntityId_ChangedAtUtc",
                schema: "work",
                table: "AttributeDefinitionsDataAudit",
                columns: new[] { "TenantId", "EntityId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AttributeDefinitionsDataAudit_TenantId_ParentId_ChangedAtUtc",
                schema: "work",
                table: "AttributeDefinitionsDataAudit",
                columns: new[] { "TenantId", "ParentId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AttributeValuesDataAudit_TenantId_EntityId_ChangedAtUtc",
                schema: "work",
                table: "AttributeValuesDataAudit",
                columns: new[] { "TenantId", "EntityId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AttributeValuesDataAudit_TenantId_ParentId_ChangedAtUtc",
                schema: "work",
                table: "AttributeValuesDataAudit",
                columns: new[] { "TenantId", "ParentId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AutomationConfigurationsDataAudit_TenantId_EntityId_ChangedAtUtc",
                schema: "work",
                table: "AutomationConfigurationsDataAudit",
                columns: new[] { "TenantId", "EntityId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AutomationConfigurationsDataAudit_TenantId_ParentId_ChangedAtUtc",
                schema: "work",
                table: "AutomationConfigurationsDataAudit",
                columns: new[] { "TenantId", "ParentId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_HistoryDataAudit_TenantId_EntityId_ChangedAtUtc",
                schema: "work",
                table: "HistoryDataAudit",
                columns: new[] { "TenantId", "EntityId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_HistoryDataAudit_TenantId_ParentId_ChangedAtUtc",
                schema: "work",
                table: "HistoryDataAudit",
                columns: new[] { "TenantId", "ParentId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportBatchesDataAudit_TenantId_EntityId_ChangedAtUtc",
                schema: "work",
                table: "ImportBatchesDataAudit",
                columns: new[] { "TenantId", "EntityId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportBatchesDataAudit_TenantId_ParentId_ChangedAtUtc",
                schema: "work",
                table: "ImportBatchesDataAudit",
                columns: new[] { "TenantId", "ParentId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportRowsDataAudit_TenantId_EntityId_ChangedAtUtc",
                schema: "work",
                table: "ImportRowsDataAudit",
                columns: new[] { "TenantId", "EntityId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportRowsDataAudit_TenantId_ParentId_ChangedAtUtc",
                schema: "work",
                table: "ImportRowsDataAudit",
                columns: new[] { "TenantId", "ParentId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemsDataAudit_TenantId_EntityId_ChangedAtUtc",
                schema: "work",
                table: "ItemsDataAudit",
                columns: new[] { "TenantId", "EntityId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemsDataAudit_TenantId_ParentId_ChangedAtUtc",
                schema: "work",
                table: "ItemsDataAudit",
                columns: new[] { "TenantId", "ParentId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationsDataAudit_TenantId_EntityId_ChangedAtUtc",
                schema: "work",
                table: "NotificationsDataAudit",
                columns: new[] { "TenantId", "EntityId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationsDataAudit_TenantId_ParentId_ChangedAtUtc",
                schema: "work",
                table: "NotificationsDataAudit",
                columns: new[] { "TenantId", "ParentId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowActionReceiptsDataAudit_TenantId_EntityId_ChangedAtUtc",
                schema: "work",
                table: "WorkflowActionReceiptsDataAudit",
                columns: new[] { "TenantId", "EntityId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowActionReceiptsDataAudit_TenantId_ParentId_ChangedAtUtc",
                schema: "work",
                table: "WorkflowActionReceiptsDataAudit",
                columns: new[] { "TenantId", "ParentId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowOutboxDataAudit_TenantId_EntityId_ChangedAtUtc",
                schema: "work",
                table: "WorkflowOutboxDataAudit",
                columns: new[] { "TenantId", "EntityId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowOutboxDataAudit_TenantId_ParentId_ChangedAtUtc",
                schema: "work",
                table: "WorkflowOutboxDataAudit",
                columns: new[] { "TenantId", "ParentId", "ChangedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttributeDefinitionsDataAudit",
                schema: "work");

            migrationBuilder.DropTable(
                name: "AttributeValuesDataAudit",
                schema: "work");

            migrationBuilder.DropTable(
                name: "AutomationConfigurationsDataAudit",
                schema: "work");

            migrationBuilder.DropTable(
                name: "HistoryDataAudit",
                schema: "work");

            migrationBuilder.DropTable(
                name: "ImportBatchesDataAudit",
                schema: "work");

            migrationBuilder.DropTable(
                name: "ImportRowsDataAudit",
                schema: "work");

            migrationBuilder.DropTable(
                name: "ItemsDataAudit",
                schema: "work");

            migrationBuilder.DropTable(
                name: "NotificationsDataAudit",
                schema: "work");

            migrationBuilder.DropTable(
                name: "WorkflowActionReceiptsDataAudit",
                schema: "work");

            migrationBuilder.DropTable(
                name: "WorkflowOutboxDataAudit",
                schema: "work");
        }
    }
}
