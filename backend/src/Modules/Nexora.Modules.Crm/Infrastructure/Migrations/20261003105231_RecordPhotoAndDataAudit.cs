using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Modules.Crm.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RecordPhotoAndDataAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PhotoFileId",
                schema: "crm",
                table: "Records",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AccountContactRolesDataAudit",
                schema: "crm",
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
                    table.PrimaryKey("PK_AccountContactRolesDataAudit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AccountContactsDataAudit",
                schema: "crm",
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
                    table.PrimaryKey("PK_AccountContactsDataAudit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AccountProfilesDataAudit",
                schema: "crm",
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
                    table.PrimaryKey("PK_AccountProfilesDataAudit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ActivitiesDataAudit",
                schema: "crm",
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
                    table.PrimaryKey("PK_ActivitiesDataAudit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AddressesDataAudit",
                schema: "crm",
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
                    table.PrimaryKey("PK_AddressesDataAudit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AutomationConfigurationsDataAudit",
                schema: "crm",
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
                name: "CommunicationMethodsDataAudit",
                schema: "crm",
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
                    table.PrimaryKey("PK_CommunicationMethodsDataAudit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContactProfilesDataAudit",
                schema: "crm",
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
                    table.PrimaryKey("PK_ContactProfilesDataAudit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomFieldDefinitionsDataAudit",
                schema: "crm",
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
                    table.PrimaryKey("PK_CustomFieldDefinitionsDataAudit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomFieldValuesDataAudit",
                schema: "crm",
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
                    table.PrimaryKey("PK_CustomFieldValuesDataAudit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FilesDataAudit",
                schema: "crm",
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
                    table.PrimaryKey("PK_FilesDataAudit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotesDataAudit",
                schema: "crm",
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
                    table.PrimaryKey("PK_NotesDataAudit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RecordsDataAudit",
                schema: "crm",
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
                    table.PrimaryKey("PK_RecordsDataAudit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RelationshipsDataAudit",
                schema: "crm",
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
                    table.PrimaryKey("PK_RelationshipsDataAudit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SavedViewsDataAudit",
                schema: "crm",
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
                    table.PrimaryKey("PK_SavedViewsDataAudit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TagsDataAudit",
                schema: "crm",
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
                    table.PrimaryKey("PK_TagsDataAudit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowActionReceiptsDataAudit",
                schema: "crm",
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
                schema: "crm",
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
                name: "IX_AccountContactRolesDataAudit_TenantId_EntityId_ChangedAtUtc",
                schema: "crm",
                table: "AccountContactRolesDataAudit",
                columns: new[] { "TenantId", "EntityId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountContactRolesDataAudit_TenantId_ParentId_ChangedAtUtc",
                schema: "crm",
                table: "AccountContactRolesDataAudit",
                columns: new[] { "TenantId", "ParentId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountContactsDataAudit_TenantId_EntityId_ChangedAtUtc",
                schema: "crm",
                table: "AccountContactsDataAudit",
                columns: new[] { "TenantId", "EntityId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountContactsDataAudit_TenantId_ParentId_ChangedAtUtc",
                schema: "crm",
                table: "AccountContactsDataAudit",
                columns: new[] { "TenantId", "ParentId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountProfilesDataAudit_TenantId_EntityId_ChangedAtUtc",
                schema: "crm",
                table: "AccountProfilesDataAudit",
                columns: new[] { "TenantId", "EntityId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountProfilesDataAudit_TenantId_ParentId_ChangedAtUtc",
                schema: "crm",
                table: "AccountProfilesDataAudit",
                columns: new[] { "TenantId", "ParentId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ActivitiesDataAudit_TenantId_EntityId_ChangedAtUtc",
                schema: "crm",
                table: "ActivitiesDataAudit",
                columns: new[] { "TenantId", "EntityId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ActivitiesDataAudit_TenantId_ParentId_ChangedAtUtc",
                schema: "crm",
                table: "ActivitiesDataAudit",
                columns: new[] { "TenantId", "ParentId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AddressesDataAudit_TenantId_EntityId_ChangedAtUtc",
                schema: "crm",
                table: "AddressesDataAudit",
                columns: new[] { "TenantId", "EntityId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AddressesDataAudit_TenantId_ParentId_ChangedAtUtc",
                schema: "crm",
                table: "AddressesDataAudit",
                columns: new[] { "TenantId", "ParentId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AutomationConfigurationsDataAudit_TenantId_EntityId_ChangedAtUtc",
                schema: "crm",
                table: "AutomationConfigurationsDataAudit",
                columns: new[] { "TenantId", "EntityId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AutomationConfigurationsDataAudit_TenantId_ParentId_ChangedAtUtc",
                schema: "crm",
                table: "AutomationConfigurationsDataAudit",
                columns: new[] { "TenantId", "ParentId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationMethodsDataAudit_TenantId_EntityId_ChangedAtUtc",
                schema: "crm",
                table: "CommunicationMethodsDataAudit",
                columns: new[] { "TenantId", "EntityId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationMethodsDataAudit_TenantId_ParentId_ChangedAtUtc",
                schema: "crm",
                table: "CommunicationMethodsDataAudit",
                columns: new[] { "TenantId", "ParentId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ContactProfilesDataAudit_TenantId_EntityId_ChangedAtUtc",
                schema: "crm",
                table: "ContactProfilesDataAudit",
                columns: new[] { "TenantId", "EntityId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ContactProfilesDataAudit_TenantId_ParentId_ChangedAtUtc",
                schema: "crm",
                table: "ContactProfilesDataAudit",
                columns: new[] { "TenantId", "ParentId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomFieldDefinitionsDataAudit_TenantId_EntityId_ChangedAtUtc",
                schema: "crm",
                table: "CustomFieldDefinitionsDataAudit",
                columns: new[] { "TenantId", "EntityId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomFieldDefinitionsDataAudit_TenantId_ParentId_ChangedAtUtc",
                schema: "crm",
                table: "CustomFieldDefinitionsDataAudit",
                columns: new[] { "TenantId", "ParentId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomFieldValuesDataAudit_TenantId_EntityId_ChangedAtUtc",
                schema: "crm",
                table: "CustomFieldValuesDataAudit",
                columns: new[] { "TenantId", "EntityId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomFieldValuesDataAudit_TenantId_ParentId_ChangedAtUtc",
                schema: "crm",
                table: "CustomFieldValuesDataAudit",
                columns: new[] { "TenantId", "ParentId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_FilesDataAudit_TenantId_EntityId_ChangedAtUtc",
                schema: "crm",
                table: "FilesDataAudit",
                columns: new[] { "TenantId", "EntityId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_FilesDataAudit_TenantId_ParentId_ChangedAtUtc",
                schema: "crm",
                table: "FilesDataAudit",
                columns: new[] { "TenantId", "ParentId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_NotesDataAudit_TenantId_EntityId_ChangedAtUtc",
                schema: "crm",
                table: "NotesDataAudit",
                columns: new[] { "TenantId", "EntityId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_NotesDataAudit_TenantId_ParentId_ChangedAtUtc",
                schema: "crm",
                table: "NotesDataAudit",
                columns: new[] { "TenantId", "ParentId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RecordsDataAudit_TenantId_EntityId_ChangedAtUtc",
                schema: "crm",
                table: "RecordsDataAudit",
                columns: new[] { "TenantId", "EntityId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RecordsDataAudit_TenantId_ParentId_ChangedAtUtc",
                schema: "crm",
                table: "RecordsDataAudit",
                columns: new[] { "TenantId", "ParentId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RelationshipsDataAudit_TenantId_EntityId_ChangedAtUtc",
                schema: "crm",
                table: "RelationshipsDataAudit",
                columns: new[] { "TenantId", "EntityId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RelationshipsDataAudit_TenantId_ParentId_ChangedAtUtc",
                schema: "crm",
                table: "RelationshipsDataAudit",
                columns: new[] { "TenantId", "ParentId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SavedViewsDataAudit_TenantId_EntityId_ChangedAtUtc",
                schema: "crm",
                table: "SavedViewsDataAudit",
                columns: new[] { "TenantId", "EntityId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SavedViewsDataAudit_TenantId_ParentId_ChangedAtUtc",
                schema: "crm",
                table: "SavedViewsDataAudit",
                columns: new[] { "TenantId", "ParentId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TagsDataAudit_TenantId_EntityId_ChangedAtUtc",
                schema: "crm",
                table: "TagsDataAudit",
                columns: new[] { "TenantId", "EntityId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TagsDataAudit_TenantId_ParentId_ChangedAtUtc",
                schema: "crm",
                table: "TagsDataAudit",
                columns: new[] { "TenantId", "ParentId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowActionReceiptsDataAudit_TenantId_EntityId_ChangedAtUtc",
                schema: "crm",
                table: "WorkflowActionReceiptsDataAudit",
                columns: new[] { "TenantId", "EntityId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowActionReceiptsDataAudit_TenantId_ParentId_ChangedAtUtc",
                schema: "crm",
                table: "WorkflowActionReceiptsDataAudit",
                columns: new[] { "TenantId", "ParentId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowOutboxDataAudit_TenantId_EntityId_ChangedAtUtc",
                schema: "crm",
                table: "WorkflowOutboxDataAudit",
                columns: new[] { "TenantId", "EntityId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowOutboxDataAudit_TenantId_ParentId_ChangedAtUtc",
                schema: "crm",
                table: "WorkflowOutboxDataAudit",
                columns: new[] { "TenantId", "ParentId", "ChangedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountContactRolesDataAudit",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "AccountContactsDataAudit",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "AccountProfilesDataAudit",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "ActivitiesDataAudit",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "AddressesDataAudit",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "AutomationConfigurationsDataAudit",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "CommunicationMethodsDataAudit",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "ContactProfilesDataAudit",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "CustomFieldDefinitionsDataAudit",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "CustomFieldValuesDataAudit",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "FilesDataAudit",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "NotesDataAudit",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "RecordsDataAudit",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "RelationshipsDataAudit",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "SavedViewsDataAudit",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "TagsDataAudit",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "WorkflowActionReceiptsDataAudit",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "WorkflowOutboxDataAudit",
                schema: "crm");

            migrationBuilder.DropColumn(
                name: "PhotoFileId",
                schema: "crm",
                table: "Records");
        }
    }
}
