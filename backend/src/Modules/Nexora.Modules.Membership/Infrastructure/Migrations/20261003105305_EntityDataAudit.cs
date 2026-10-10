using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Modules.Membership.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EntityDataAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApplicationEvidenceDataAudit",
                schema: "membership",
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
                    table.PrimaryKey("PK_ApplicationEvidenceDataAudit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationsDataAudit",
                schema: "membership",
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
                    table.PrimaryKey("PK_ApplicationsDataAudit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AttributeDefinitionsDataAudit",
                schema: "membership",
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
                schema: "membership",
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
                schema: "membership",
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
                name: "CertificatesDataAudit",
                schema: "membership",
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
                    table.PrimaryKey("PK_CertificatesDataAudit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CommunicationsDataAudit",
                schema: "membership",
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
                    table.PrimaryKey("PK_CommunicationsDataAudit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HistoryDataAudit",
                schema: "membership",
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
                name: "PoliciesDataAudit",
                schema: "membership",
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
                    table.PrimaryKey("PK_PoliciesDataAudit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PortalApplicationDraftsDataAudit",
                schema: "membership",
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
                    table.PrimaryKey("PK_PortalApplicationDraftsDataAudit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PortalNotificationsDataAudit",
                schema: "membership",
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
                    table.PrimaryKey("PK_PortalNotificationsDataAudit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductsDataAudit",
                schema: "membership",
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
                    table.PrimaryKey("PK_ProductsDataAudit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReviewsDataAudit",
                schema: "membership",
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
                    table.PrimaryKey("PK_ReviewsDataAudit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SetupValuesDataAudit",
                schema: "membership",
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
                    table.PrimaryKey("PK_SetupValuesDataAudit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionsDataAudit",
                schema: "membership",
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
                    table.PrimaryKey("PK_SubscriptionsDataAudit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowActionReceiptsDataAudit",
                schema: "membership",
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
                schema: "membership",
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
                name: "IX_ApplicationEvidenceDataAudit_TenantId_EntityId_ChangedAtUtc",
                schema: "membership",
                table: "ApplicationEvidenceDataAudit",
                columns: new[] { "TenantId", "EntityId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationEvidenceDataAudit_TenantId_ParentId_ChangedAtUtc",
                schema: "membership",
                table: "ApplicationEvidenceDataAudit",
                columns: new[] { "TenantId", "ParentId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationsDataAudit_TenantId_EntityId_ChangedAtUtc",
                schema: "membership",
                table: "ApplicationsDataAudit",
                columns: new[] { "TenantId", "EntityId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationsDataAudit_TenantId_ParentId_ChangedAtUtc",
                schema: "membership",
                table: "ApplicationsDataAudit",
                columns: new[] { "TenantId", "ParentId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AttributeDefinitionsDataAudit_TenantId_EntityId_ChangedAtUtc",
                schema: "membership",
                table: "AttributeDefinitionsDataAudit",
                columns: new[] { "TenantId", "EntityId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AttributeDefinitionsDataAudit_TenantId_ParentId_ChangedAtUtc",
                schema: "membership",
                table: "AttributeDefinitionsDataAudit",
                columns: new[] { "TenantId", "ParentId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AttributeValuesDataAudit_TenantId_EntityId_ChangedAtUtc",
                schema: "membership",
                table: "AttributeValuesDataAudit",
                columns: new[] { "TenantId", "EntityId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AttributeValuesDataAudit_TenantId_ParentId_ChangedAtUtc",
                schema: "membership",
                table: "AttributeValuesDataAudit",
                columns: new[] { "TenantId", "ParentId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AutomationConfigurationsDataAudit_TenantId_EntityId_ChangedAtUtc",
                schema: "membership",
                table: "AutomationConfigurationsDataAudit",
                columns: new[] { "TenantId", "EntityId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AutomationConfigurationsDataAudit_TenantId_ParentId_ChangedAtUtc",
                schema: "membership",
                table: "AutomationConfigurationsDataAudit",
                columns: new[] { "TenantId", "ParentId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CertificatesDataAudit_TenantId_EntityId_ChangedAtUtc",
                schema: "membership",
                table: "CertificatesDataAudit",
                columns: new[] { "TenantId", "EntityId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CertificatesDataAudit_TenantId_ParentId_ChangedAtUtc",
                schema: "membership",
                table: "CertificatesDataAudit",
                columns: new[] { "TenantId", "ParentId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationsDataAudit_TenantId_EntityId_ChangedAtUtc",
                schema: "membership",
                table: "CommunicationsDataAudit",
                columns: new[] { "TenantId", "EntityId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationsDataAudit_TenantId_ParentId_ChangedAtUtc",
                schema: "membership",
                table: "CommunicationsDataAudit",
                columns: new[] { "TenantId", "ParentId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_HistoryDataAudit_TenantId_EntityId_ChangedAtUtc",
                schema: "membership",
                table: "HistoryDataAudit",
                columns: new[] { "TenantId", "EntityId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_HistoryDataAudit_TenantId_ParentId_ChangedAtUtc",
                schema: "membership",
                table: "HistoryDataAudit",
                columns: new[] { "TenantId", "ParentId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PoliciesDataAudit_TenantId_EntityId_ChangedAtUtc",
                schema: "membership",
                table: "PoliciesDataAudit",
                columns: new[] { "TenantId", "EntityId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PoliciesDataAudit_TenantId_ParentId_ChangedAtUtc",
                schema: "membership",
                table: "PoliciesDataAudit",
                columns: new[] { "TenantId", "ParentId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PortalApplicationDraftsDataAudit_TenantId_EntityId_ChangedAtUtc",
                schema: "membership",
                table: "PortalApplicationDraftsDataAudit",
                columns: new[] { "TenantId", "EntityId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PortalApplicationDraftsDataAudit_TenantId_ParentId_ChangedAtUtc",
                schema: "membership",
                table: "PortalApplicationDraftsDataAudit",
                columns: new[] { "TenantId", "ParentId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PortalNotificationsDataAudit_TenantId_EntityId_ChangedAtUtc",
                schema: "membership",
                table: "PortalNotificationsDataAudit",
                columns: new[] { "TenantId", "EntityId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PortalNotificationsDataAudit_TenantId_ParentId_ChangedAtUtc",
                schema: "membership",
                table: "PortalNotificationsDataAudit",
                columns: new[] { "TenantId", "ParentId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductsDataAudit_TenantId_EntityId_ChangedAtUtc",
                schema: "membership",
                table: "ProductsDataAudit",
                columns: new[] { "TenantId", "EntityId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductsDataAudit_TenantId_ParentId_ChangedAtUtc",
                schema: "membership",
                table: "ProductsDataAudit",
                columns: new[] { "TenantId", "ParentId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ReviewsDataAudit_TenantId_EntityId_ChangedAtUtc",
                schema: "membership",
                table: "ReviewsDataAudit",
                columns: new[] { "TenantId", "EntityId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ReviewsDataAudit_TenantId_ParentId_ChangedAtUtc",
                schema: "membership",
                table: "ReviewsDataAudit",
                columns: new[] { "TenantId", "ParentId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SetupValuesDataAudit_TenantId_EntityId_ChangedAtUtc",
                schema: "membership",
                table: "SetupValuesDataAudit",
                columns: new[] { "TenantId", "EntityId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SetupValuesDataAudit_TenantId_ParentId_ChangedAtUtc",
                schema: "membership",
                table: "SetupValuesDataAudit",
                columns: new[] { "TenantId", "ParentId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionsDataAudit_TenantId_EntityId_ChangedAtUtc",
                schema: "membership",
                table: "SubscriptionsDataAudit",
                columns: new[] { "TenantId", "EntityId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionsDataAudit_TenantId_ParentId_ChangedAtUtc",
                schema: "membership",
                table: "SubscriptionsDataAudit",
                columns: new[] { "TenantId", "ParentId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowActionReceiptsDataAudit_TenantId_EntityId_ChangedAtUtc",
                schema: "membership",
                table: "WorkflowActionReceiptsDataAudit",
                columns: new[] { "TenantId", "EntityId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowActionReceiptsDataAudit_TenantId_ParentId_ChangedAtUtc",
                schema: "membership",
                table: "WorkflowActionReceiptsDataAudit",
                columns: new[] { "TenantId", "ParentId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowOutboxDataAudit_TenantId_EntityId_ChangedAtUtc",
                schema: "membership",
                table: "WorkflowOutboxDataAudit",
                columns: new[] { "TenantId", "EntityId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowOutboxDataAudit_TenantId_ParentId_ChangedAtUtc",
                schema: "membership",
                table: "WorkflowOutboxDataAudit",
                columns: new[] { "TenantId", "ParentId", "ChangedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationEvidenceDataAudit",
                schema: "membership");

            migrationBuilder.DropTable(
                name: "ApplicationsDataAudit",
                schema: "membership");

            migrationBuilder.DropTable(
                name: "AttributeDefinitionsDataAudit",
                schema: "membership");

            migrationBuilder.DropTable(
                name: "AttributeValuesDataAudit",
                schema: "membership");

            migrationBuilder.DropTable(
                name: "AutomationConfigurationsDataAudit",
                schema: "membership");

            migrationBuilder.DropTable(
                name: "CertificatesDataAudit",
                schema: "membership");

            migrationBuilder.DropTable(
                name: "CommunicationsDataAudit",
                schema: "membership");

            migrationBuilder.DropTable(
                name: "HistoryDataAudit",
                schema: "membership");

            migrationBuilder.DropTable(
                name: "PoliciesDataAudit",
                schema: "membership");

            migrationBuilder.DropTable(
                name: "PortalApplicationDraftsDataAudit",
                schema: "membership");

            migrationBuilder.DropTable(
                name: "PortalNotificationsDataAudit",
                schema: "membership");

            migrationBuilder.DropTable(
                name: "ProductsDataAudit",
                schema: "membership");

            migrationBuilder.DropTable(
                name: "ReviewsDataAudit",
                schema: "membership");

            migrationBuilder.DropTable(
                name: "SetupValuesDataAudit",
                schema: "membership");

            migrationBuilder.DropTable(
                name: "SubscriptionsDataAudit",
                schema: "membership");

            migrationBuilder.DropTable(
                name: "WorkflowActionReceiptsDataAudit",
                schema: "membership");

            migrationBuilder.DropTable(
                name: "WorkflowOutboxDataAudit",
                schema: "membership");
        }
    }
}
