using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Modules.Membership.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ApplicationQuoteEndDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RequestedEndUtc",
                schema: "membership",
                table: "Applications",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Certificates",
                schema: "membership",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MembershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Number = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    HolderName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    StartsAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndsAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IssuedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Certificates", x => x.Id);
                    table.UniqueConstraint("AK_Certificates_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_Certificates_Subscriptions_TenantId_MembershipId",
                        columns: x => new { x.TenantId, x.MembershipId },
                        principalSchema: "membership",
                        principalTable: "Subscriptions",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Communications",
                schema: "membership",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MembershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Recipient = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Communications", x => x.Id);
                    table.UniqueConstraint("AK_Communications_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_Communications_Subscriptions_TenantId_MembershipId",
                        columns: x => new { x.TenantId, x.MembershipId },
                        principalSchema: "membership",
                        principalTable: "Subscriptions",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Policies",
                schema: "membership",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RenewalMode = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    YearEndMonth = table.Column<int>(type: "int", nullable: false),
                    YearEndDay = table.Column<int>(type: "int", nullable: false),
                    Prorate = table.Column<bool>(type: "bit", nullable: false),
                    ProcessingFee = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Credit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RequirePayment = table.Column<bool>(type: "bit", nullable: false),
                    RequireCompliance = table.Column<bool>(type: "bit", nullable: false),
                    RequireDeclaration = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Policies", x => x.Id);
                    table.UniqueConstraint("AK_Policies_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_Policies_Products_TenantId_ProductId",
                        columns: x => new { x.TenantId, x.ProductId },
                        principalSchema: "membership",
                        principalTable: "Products",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Reviews",
                schema: "membership",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MembershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TermEndUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaymentVerified = table.Column<bool>(type: "bit", nullable: false),
                    PaymentReference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ComplianceStatus = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Evidence = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    DeclarationAccepted = table.Column<bool>(type: "bit", nullable: false),
                    ExcludedFromRenewal = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reviews", x => x.Id);
                    table.UniqueConstraint("AK_Reviews_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_Reviews_Subscriptions_TenantId_MembershipId",
                        columns: x => new { x.TenantId, x.MembershipId },
                        principalSchema: "membership",
                        principalTable: "Subscriptions",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_TenantId_MembershipId_EndsAtUtc",
                schema: "membership",
                table: "Certificates",
                columns: new[] { "TenantId", "MembershipId", "EndsAtUtc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Communications_TenantId_MembershipId",
                schema: "membership",
                table: "Communications",
                columns: new[] { "TenantId", "MembershipId" });

            migrationBuilder.CreateIndex(
                name: "IX_Communications_TenantId_RequestId",
                schema: "membership",
                table: "Communications",
                columns: new[] { "TenantId", "RequestId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Policies_TenantId_ProductId",
                schema: "membership",
                table: "Policies",
                columns: new[] { "TenantId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_TenantId_MembershipId_TermEndUtc",
                schema: "membership",
                table: "Reviews",
                columns: new[] { "TenantId", "MembershipId", "TermEndUtc" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Certificates",
                schema: "membership");

            migrationBuilder.DropTable(
                name: "Communications",
                schema: "membership");

            migrationBuilder.DropTable(
                name: "Policies",
                schema: "membership");

            migrationBuilder.DropTable(
                name: "Reviews",
                schema: "membership");

            migrationBuilder.DropColumn(
                name: "RequestedEndUtc",
                schema: "membership",
                table: "Applications");
        }
    }
}
