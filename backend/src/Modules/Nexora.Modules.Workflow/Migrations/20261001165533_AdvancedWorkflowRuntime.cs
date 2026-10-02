using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Modules.Workflow.Migrations
{
    /// <inheritdoc />
    public partial class AdvancedWorkflowRuntime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AncestryJson",
                schema: "workflow",
                table: "Runs",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ContextJson",
                schema: "workflow",
                table: "Runs",
                type: "nvarchar(max)",
                maxLength: 262144,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "ParentRunId",
                schema: "workflow",
                table: "Runs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RootRunId",
                schema: "workflow",
                table: "Runs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TriggerJson",
                schema: "workflow",
                table: "Runs",
                type: "nvarchar(max)",
                maxLength: 262144,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Connectors",
                schema: "workflow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Endpoint = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Port = table.Column<int>(type: "int", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ProtectedSecret = table.Column<string>(type: "nvarchar(max)", maxLength: 16000, nullable: false),
                    Sender = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Model = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Connectors", x => x.Id);
                    table.UniqueConstraint("AK_Connectors_TenantId_Id", x => new { x.TenantId, x.Id });
                });

            migrationBuilder.CreateTable(
                name: "EmailTemplates",
                schema: "workflow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", maxLength: 16000, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailTemplates", x => x.Id);
                    table.UniqueConstraint("AK_EmailTemplates_TenantId_Id", x => new { x.TenantId, x.Id });
                });

            migrationBuilder.CreateTable(
                name: "Schedules",
                schema: "workflow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NextAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActivatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LeaseUntilUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LastError = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Schedules", x => x.Id);
                    table.UniqueConstraint("AK_Schedules_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_Schedules_Versions_TenantId_WorkflowVersionId",
                        columns: x => new { x.TenantId, x.WorkflowVersionId },
                        principalSchema: "workflow",
                        principalTable: "Versions",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Tokens",
                schema: "workflow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentTokenId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NodeId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Scope = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DueAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LeaseUntilUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ContextJson = table.Column<string>(type: "nvarchar(max)", maxLength: 262144, nullable: false),
                    FramesJson = table.Column<string>(type: "nvarchar(max)", maxLength: 262144, nullable: false),
                    PendingInputsJson = table.Column<string>(type: "nvarchar(max)", maxLength: 262144, nullable: false),
                    PendingNodeId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Error = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    Steps = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tokens", x => x.Id);
                    table.UniqueConstraint("AK_Tokens_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_Tokens_Runs_TenantId_RunId",
                        columns: x => new { x.TenantId, x.RunId },
                        principalSchema: "workflow",
                        principalTable: "Runs",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Webhooks",
                schema: "workflow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProtectedSecret = table.Column<string>(type: "nvarchar(max)", maxLength: 16000, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Webhooks", x => x.Id);
                    table.UniqueConstraint("AK_Webhooks_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_Webhooks_Definitions_TenantId_DefinitionId",
                        columns: x => new { x.TenantId, x.DefinitionId },
                        principalSchema: "workflow",
                        principalTable: "Definitions",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Deliveries",
                schema: "workflow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConnectorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RequestJson = table.Column<string>(type: "nvarchar(max)", maxLength: 262144, nullable: false),
                    ResultJson = table.Column<string>(type: "nvarchar(max)", maxLength: 262144, nullable: false),
                    Error = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FinishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Deliveries", x => x.Id);
                    table.UniqueConstraint("AK_Deliveries_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_Deliveries_Connectors_TenantId_ConnectorId",
                        columns: x => new { x.TenantId, x.ConnectorId },
                        principalSchema: "workflow",
                        principalTable: "Connectors",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Deliveries_TenantId_ConnectorId",
                schema: "workflow",
                table: "Deliveries",
                columns: new[] { "TenantId", "ConnectorId" });

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_IsEnabled_NextAtUtc",
                schema: "workflow",
                table: "Schedules",
                columns: new[] { "IsEnabled", "NextAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_TenantId_DefinitionId",
                schema: "workflow",
                table: "Schedules",
                columns: new[] { "TenantId", "DefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_TenantId_WorkflowVersionId",
                schema: "workflow",
                table: "Schedules",
                columns: new[] { "TenantId", "WorkflowVersionId" });

            migrationBuilder.CreateIndex(
                name: "IX_Tokens_Status_DueAtUtc_LeaseUntilUtc",
                schema: "workflow",
                table: "Tokens",
                columns: new[] { "Status", "DueAtUtc", "LeaseUntilUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Tokens_TenantId_RunId",
                schema: "workflow",
                table: "Tokens",
                columns: new[] { "TenantId", "RunId" });

            migrationBuilder.CreateIndex(
                name: "IX_Webhooks_TenantId_DefinitionId",
                schema: "workflow",
                table: "Webhooks",
                columns: new[] { "TenantId", "DefinitionId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Deliveries",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "EmailTemplates",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "Schedules",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "Tokens",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "Webhooks",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "Connectors",
                schema: "workflow");

            migrationBuilder.DropColumn(
                name: "AncestryJson",
                schema: "workflow",
                table: "Runs");

            migrationBuilder.DropColumn(
                name: "ContextJson",
                schema: "workflow",
                table: "Runs");

            migrationBuilder.DropColumn(
                name: "ParentRunId",
                schema: "workflow",
                table: "Runs");

            migrationBuilder.DropColumn(
                name: "RootRunId",
                schema: "workflow",
                table: "Runs");

            migrationBuilder.DropColumn(
                name: "TriggerJson",
                schema: "workflow",
                table: "Runs");
        }
    }
}
