using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Modules.Membership.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PortalNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApplicationEvidence",
                schema: "membership",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DraftId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Content = table.Column<byte[]>(type: "varbinary(max)", maxLength: 5242880, nullable: false),
                    Size = table.Column<int>(type: "int", nullable: false),
                    Sha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ScanStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ScannedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationEvidence", x => x.Id);
                    table.UniqueConstraint("AK_ApplicationEvidence_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_ApplicationEvidence_PortalApplicationDrafts_TenantId_DraftId",
                        columns: x => new { x.TenantId, x.DraftId },
                        principalSchema: "membership",
                        principalTable: "PortalApplicationDrafts",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PortalNotifications",
                schema: "membership",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecipientUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Kind = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    DeliveryStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DeliveryAttempts = table.Column<int>(type: "int", nullable: false),
                    ReadAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortalNotifications", x => x.Id);
                    table.UniqueConstraint("AK_PortalNotifications_TenantId_Id", x => new { x.TenantId, x.Id });
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationEvidence_TenantId_DraftId",
                schema: "membership",
                table: "ApplicationEvidence",
                columns: new[] { "TenantId", "DraftId" });

            migrationBuilder.CreateIndex(
                name: "IX_PortalNotifications_TenantId_RecipientUserId_CreatedAtUtc",
                schema: "membership",
                table: "PortalNotifications",
                columns: new[] { "TenantId", "RecipientUserId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationEvidence",
                schema: "membership");

            migrationBuilder.DropTable(
                name: "PortalNotifications",
                schema: "membership");
        }
    }
}
