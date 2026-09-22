using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Modules.Content.Migrations
{
    /// <inheritdoc />
    public partial class InitialContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "content");

            migrationBuilder.CreateTable(
                name: "Sites",
                schema: "content",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Tagline = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sites", x => x.Id);
                    table.UniqueConstraint("AK_Sites_TenantId_Id", x => new { x.TenantId, x.Id });
                });

            migrationBuilder.CreateTable(
                name: "Pages",
                schema: "content",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SiteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DraftId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PublishedId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pages", x => x.Id);
                    table.UniqueConstraint("AK_Pages_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_Pages_Sites_TenantId_SiteId",
                        columns: x => new { x.TenantId, x.SiteId },
                        principalSchema: "content",
                        principalTable: "Sites",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PageVersions",
                schema: "content",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    BlocksJson = table.Column<string>(type: "nvarchar(max)", maxLength: 40000, nullable: false),
                    AuthorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PageVersions", x => x.Id);
                    table.UniqueConstraint("AK_PageVersions_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_PageVersions_Pages_TenantId_PageId",
                        columns: x => new { x.TenantId, x.PageId },
                        principalSchema: "content",
                        principalTable: "Pages",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Publications",
                schema: "content",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PageVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Publications", x => x.Id);
                    table.UniqueConstraint("AK_Publications_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_Publications_Pages_TenantId_PageId",
                        columns: x => new { x.TenantId, x.PageId },
                        principalSchema: "content",
                        principalTable: "Pages",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Pages_TenantId_SiteId_Slug",
                schema: "content",
                table: "Pages",
                columns: new[] { "TenantId", "SiteId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PageVersions_TenantId_PageId",
                schema: "content",
                table: "PageVersions",
                columns: new[] { "TenantId", "PageId" });

            migrationBuilder.CreateIndex(
                name: "IX_Publications_TenantId_PageId",
                schema: "content",
                table: "Publications",
                columns: new[] { "TenantId", "PageId" });

            migrationBuilder.CreateIndex(
                name: "IX_Sites_TenantId",
                schema: "content",
                table: "Sites",
                column: "TenantId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PageVersions",
                schema: "content");

            migrationBuilder.DropTable(
                name: "Publications",
                schema: "content");

            migrationBuilder.DropTable(
                name: "Pages",
                schema: "content");

            migrationBuilder.DropTable(
                name: "Sites",
                schema: "content");
        }
    }
}
