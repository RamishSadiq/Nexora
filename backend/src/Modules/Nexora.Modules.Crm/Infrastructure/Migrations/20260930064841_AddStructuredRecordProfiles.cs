using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Modules.Crm.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStructuredRecordProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountProfiles",
                schema: "crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TradingAs = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DateBusinessCommenced = table.Column<DateOnly>(type: "date", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountProfiles", x => x.Id);
                    table.UniqueConstraint("AK_AccountProfiles_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_AccountProfiles_Records_TenantId_RecordId",
                        columns: x => new { x.TenantId, x.RecordId },
                        principalSchema: "crm",
                        principalTable: "Records",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ContactProfiles",
                schema: "crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FirstName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MiddleName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PreviousLastName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PostNominal = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactProfiles", x => x.Id);
                    table.UniqueConstraint("AK_ContactProfiles_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_ContactProfiles_Records_TenantId_RecordId",
                        columns: x => new { x.TenantId, x.RecordId },
                        principalSchema: "crm",
                        principalTable: "Records",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountProfiles_TenantId_RecordId",
                schema: "crm",
                table: "AccountProfiles",
                columns: new[] { "TenantId", "RecordId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContactProfiles_TenantId_RecordId",
                schema: "crm",
                table: "ContactProfiles",
                columns: new[] { "TenantId", "RecordId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountProfiles",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "ContactProfiles",
                schema: "crm");
        }
    }
}
