using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Modules.Crm.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ProfileTaxonomy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProfileAssignmentsDataAudit",
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
                    table.PrimaryKey("PK_ProfileAssignmentsDataAudit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProfileDefinitions",
                schema: "crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentNode = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    ChildNode = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    NormalizedParent = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    NormalizedChild = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    Version = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfileDefinitions", x => x.Id);
                    table.UniqueConstraint("AK_ProfileDefinitions_TenantId_Id", x => new { x.TenantId, x.Id });
                });

            migrationBuilder.CreateTable(
                name: "ProfileDefinitionsDataAudit",
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
                    table.PrimaryKey("PK_ProfileDefinitionsDataAudit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProfileAssignments",
                schema: "crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Slot = table.Column<int>(type: "int", nullable: false),
                    DefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfileAssignments", x => x.Id);
                    table.UniqueConstraint("AK_ProfileAssignments_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.CheckConstraint("CK_ProfileAssignments_Slot", "[Slot] >= 1 AND [Slot] <= 5");
                    table.ForeignKey(
                        name: "FK_ProfileAssignments_ProfileDefinitions_TenantId_DefinitionId",
                        columns: x => new { x.TenantId, x.DefinitionId },
                        principalSchema: "crm",
                        principalTable: "ProfileDefinitions",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProfileAssignments_Records_TenantId_RecordId",
                        columns: x => new { x.TenantId, x.RecordId },
                        principalSchema: "crm",
                        principalTable: "Records",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProfileAssignments_TenantId_DefinitionId",
                schema: "crm",
                table: "ProfileAssignments",
                columns: new[] { "TenantId", "DefinitionId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProfileAssignments_TenantId_RecordId_Slot",
                schema: "crm",
                table: "ProfileAssignments",
                columns: new[] { "TenantId", "RecordId", "Slot" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProfileAssignmentsDataAudit_TenantId_EntityId_ChangedAtUtc",
                schema: "crm",
                table: "ProfileAssignmentsDataAudit",
                columns: new[] { "TenantId", "EntityId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ProfileAssignmentsDataAudit_TenantId_ParentId_ChangedAtUtc",
                schema: "crm",
                table: "ProfileAssignmentsDataAudit",
                columns: new[] { "TenantId", "ParentId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ProfileDefinitions_TenantId_NormalizedParent_NormalizedChild",
                schema: "crm",
                table: "ProfileDefinitions",
                columns: new[] { "TenantId", "NormalizedParent", "NormalizedChild" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProfileDefinitionsDataAudit_TenantId_EntityId_ChangedAtUtc",
                schema: "crm",
                table: "ProfileDefinitionsDataAudit",
                columns: new[] { "TenantId", "EntityId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ProfileDefinitionsDataAudit_TenantId_ParentId_ChangedAtUtc",
                schema: "crm",
                table: "ProfileDefinitionsDataAudit",
                columns: new[] { "TenantId", "ParentId", "ChangedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProfileAssignments",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "ProfileAssignmentsDataAudit",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "ProfileDefinitionsDataAudit",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "ProfileDefinitions",
                schema: "crm");
        }
    }
}
