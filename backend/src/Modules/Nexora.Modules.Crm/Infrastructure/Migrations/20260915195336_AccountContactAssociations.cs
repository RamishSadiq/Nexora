using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Modules.Crm.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AccountContactAssociations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountContacts",
                schema: "crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContactId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobTitle = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    WorkClassification = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IsMainContact = table.Column<bool>(type: "bit", nullable: false),
                    IsAuthorisedContact = table.Column<bool>(type: "bit", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Version = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountContacts", x => x.Id);
                    table.UniqueConstraint("AK_AccountContacts_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_AccountContacts_Records_TenantId_AccountId",
                        columns: x => new { x.TenantId, x.AccountId },
                        principalSchema: "crm",
                        principalTable: "Records",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountContacts_Records_TenantId_ContactId",
                        columns: x => new { x.TenantId, x.ContactId },
                        principalSchema: "crm",
                        principalTable: "Records",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccountContactRoles",
                schema: "crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssociationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountContactRoles", x => x.Id);
                    table.UniqueConstraint("AK_AccountContactRoles_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_AccountContactRoles_AccountContacts_TenantId_AssociationId",
                        columns: x => new { x.TenantId, x.AssociationId },
                        principalSchema: "crm",
                        principalTable: "AccountContacts",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountContactRoles_TenantId_AssociationId_RoleCode",
                schema: "crm",
                table: "AccountContactRoles",
                columns: new[] { "TenantId", "AssociationId", "RoleCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountContacts_TenantId_AccountId_ContactId",
                schema: "crm",
                table: "AccountContacts",
                columns: new[] { "TenantId", "AccountId", "ContactId" },
                unique: true,
                filter: "EndDate IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AccountContacts_TenantId_ContactId_EndDate",
                schema: "crm",
                table: "AccountContacts",
                columns: new[] { "TenantId", "ContactId", "EndDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountContactRoles",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "AccountContacts",
                schema: "crm");
        }
    }
}
