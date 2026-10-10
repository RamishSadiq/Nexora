using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Modules.Membership.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MembershipClassificationHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClassificationChanges",
                schema: "membership",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MembershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NameSnapshot = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    EffectiveOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassificationChanges", x => x.Id);
                    table.UniqueConstraint("AK_ClassificationChanges_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.CheckConstraint("CK_ClassificationChanges_Kind", "[Kind] IN ('category', 'subscription-band')");
                    table.CheckConstraint("CK_ClassificationChanges_Sequence", "[Sequence] > 0");
                    table.ForeignKey(
                        name: "FK_ClassificationChanges_SetupValues_TenantId_DefinitionId",
                        columns: x => new { x.TenantId, x.DefinitionId },
                        principalSchema: "membership",
                        principalTable: "SetupValues",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassificationChanges_Subscriptions_TenantId_MembershipId",
                        columns: x => new { x.TenantId, x.MembershipId },
                        principalSchema: "membership",
                        principalTable: "Subscriptions",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClassificationChangesDataAudit",
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
                    table.PrimaryKey("PK_ClassificationChangesDataAudit", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClassificationChanges_TenantId_DefinitionId",
                schema: "membership",
                table: "ClassificationChanges",
                columns: new[] { "TenantId", "DefinitionId" });

            migrationBuilder.CreateIndex(
                name: "IX_ClassificationChanges_TenantId_MembershipId_Kind_Sequence",
                schema: "membership",
                table: "ClassificationChanges",
                columns: new[] { "TenantId", "MembershipId", "Kind", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassificationChangesDataAudit_TenantId_EntityId_ChangedAtUtc",
                schema: "membership",
                table: "ClassificationChangesDataAudit",
                columns: new[] { "TenantId", "EntityId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ClassificationChangesDataAudit_TenantId_ParentId_ChangedAtUtc",
                schema: "membership",
                table: "ClassificationChangesDataAudit",
                columns: new[] { "TenantId", "ParentId", "ChangedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClassificationChanges",
                schema: "membership");

            migrationBuilder.DropTable(
                name: "ClassificationChangesDataAudit",
                schema: "membership");
        }
    }
}
