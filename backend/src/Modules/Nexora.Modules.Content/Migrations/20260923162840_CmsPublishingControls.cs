using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Modules.Content.Migrations
{
    /// <inheritdoc />
    public partial class CmsPublishingControls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BrandColor",
                schema: "content",
                table: "Sites",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "#183a31");

            migrationBuilder.AddColumn<Guid>(
                name: "LogoMediaId",
                schema: "content",
                table: "Sites",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NavigationLabel",
                schema: "content",
                table: "Pages",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "NavigationOrder",
                schema: "content",
                table: "Pages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "ShowInNavigation",
                schema: "content",
                table: "Pages",
                type: "bit",
                nullable: false,
                defaultValue: true);

            // Preserve the existing renderer's menu labels/order on already-published sites.
            migrationBuilder.Sql("UPDATE [content].[Pages] SET [NavigationOrder] = CASE [Slug] WHEN 'home' THEN 0 WHEN 'about' THEN 1 WHEN 'membership' THEN 2 WHEN 'events' THEN 3 WHEN 'news' THEN 4 WHEN 'contact' THEN 5 ELSE 99 END, [NavigationLabel] = CASE [Slug] WHEN 'home' THEN 'Home' WHEN 'about' THEN 'About us' WHEN 'membership' THEN 'Membership' WHEN 'events' THEN 'Events' WHEN 'news' THEN 'News' WHEN 'contact' THEN 'Contact' ELSE '' END;");

            migrationBuilder.CreateTable(
                name: "MediaAssets",
                schema: "content",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SiteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Content = table.Column<byte[]>(type: "varbinary(max)", maxLength: 5242880, nullable: false),
                    ScanStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaAssets", x => x.Id);
                    table.UniqueConstraint("AK_MediaAssets_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_MediaAssets_Sites_TenantId_SiteId",
                        columns: x => new { x.TenantId, x.SiteId },
                        principalSchema: "content",
                        principalTable: "Sites",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PageRedirects",
                schema: "content",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SiteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PageRedirects", x => x.Id);
                    table.UniqueConstraint("AK_PageRedirects_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_PageRedirects_Pages_TenantId_PageId",
                        columns: x => new { x.TenantId, x.PageId },
                        principalSchema: "content",
                        principalTable: "Pages",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ScheduledPublications",
                schema: "content",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PublisherId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PublishAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Failure = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledPublications", x => x.Id);
                    table.UniqueConstraint("AK_ScheduledPublications_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_ScheduledPublications_Pages_TenantId_PageId",
                        columns: x => new { x.TenantId, x.PageId },
                        principalSchema: "content",
                        principalTable: "Pages",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssets_TenantId_SiteId",
                schema: "content",
                table: "MediaAssets",
                columns: new[] { "TenantId", "SiteId" });

            migrationBuilder.CreateIndex(
                name: "IX_PageRedirects_TenantId_PageId",
                schema: "content",
                table: "PageRedirects",
                columns: new[] { "TenantId", "PageId" });

            migrationBuilder.CreateIndex(
                name: "IX_PageRedirects_TenantId_SiteId_Slug",
                schema: "content",
                table: "PageRedirects",
                columns: new[] { "TenantId", "SiteId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledPublications_TenantId_PageId",
                schema: "content",
                table: "ScheduledPublications",
                columns: new[] { "TenantId", "PageId" },
                unique: true,
                filter: "[Status] = 'pending'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MediaAssets",
                schema: "content");

            migrationBuilder.DropTable(
                name: "PageRedirects",
                schema: "content");

            migrationBuilder.DropTable(
                name: "ScheduledPublications",
                schema: "content");

            migrationBuilder.DropColumn(
                name: "BrandColor",
                schema: "content",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "LogoMediaId",
                schema: "content",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "NavigationLabel",
                schema: "content",
                table: "Pages");

            migrationBuilder.DropColumn(
                name: "NavigationOrder",
                schema: "content",
                table: "Pages");

            migrationBuilder.DropColumn(
                name: "ShowInNavigation",
                schema: "content",
                table: "Pages");
        }
    }
}
