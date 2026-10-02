using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Modules.Crm.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class WorkflowOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkflowActionReceipts",
                schema: "crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActionKey = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    ResultJson = table.Column<string>(type: "nvarchar(max)", maxLength: 16000, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowActionReceipts", x => x.Id);
                    table.UniqueConstraint("AK_WorkflowActionReceipts_TenantId_Id", x => new { x.TenantId, x.Id });
                });

            migrationBuilder.CreateTable(
                name: "WorkflowOutbox",
                schema: "crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityKey = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    RecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    CurrentJson = table.Column<string>(type: "nvarchar(max)", maxLength: 262144, nullable: false),
                    PreviousJson = table.Column<string>(type: "nvarchar(max)", maxLength: 262144, nullable: false),
                    AncestryJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    CausationRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RootRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeliveredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowOutbox", x => x.Id);
                    table.UniqueConstraint("AK_WorkflowOutbox_TenantId_Id", x => new { x.TenantId, x.Id });
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowOutbox_DeliveredAtUtc_EntityKey_CreatedAtUtc",
                schema: "crm",
                table: "WorkflowOutbox",
                columns: new[] { "DeliveredAtUtc", "EntityKey", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkflowActionReceipts",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "WorkflowOutbox",
                schema: "crm");
        }
    }
}
