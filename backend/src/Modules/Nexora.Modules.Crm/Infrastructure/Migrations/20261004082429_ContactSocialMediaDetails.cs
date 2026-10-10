using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Modules.Crm.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ContactSocialMediaDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "NumberOfFollowers",
                schema: "crm",
                table: "ContactProfiles",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "NumberOfFollowing",
                schema: "crm",
                table: "ContactProfiles",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "NumberOfMentions",
                schema: "crm",
                table: "ContactProfiles",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "NumberOfTweetsSent",
                schema: "crm",
                table: "ContactProfiles",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "NumberOfTweetsWithTags",
                schema: "crm",
                table: "ContactProfiles",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "SocialMetricsObservedOn",
                schema: "crm",
                table: "ContactProfiles",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SocialMetricsSource",
                schema: "crm",
                table: "ContactProfiles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ContactSocialProfiles",
                schema: "crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Identifier = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactSocialProfiles", x => x.Id);
                    table.UniqueConstraint("AK_ContactSocialProfiles_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_ContactSocialProfiles_Records_TenantId_RecordId",
                        columns: x => new { x.TenantId, x.RecordId },
                        principalSchema: "crm",
                        principalTable: "Records",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ContactSocialProfilesDataAudit",
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
                    table.PrimaryKey("PK_ContactSocialProfilesDataAudit", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContactSocialProfiles_TenantId_RecordId_Channel",
                schema: "crm",
                table: "ContactSocialProfiles",
                columns: new[] { "TenantId", "RecordId", "Channel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContactSocialProfilesDataAudit_TenantId_EntityId_ChangedAtUtc",
                schema: "crm",
                table: "ContactSocialProfilesDataAudit",
                columns: new[] { "TenantId", "EntityId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ContactSocialProfilesDataAudit_TenantId_ParentId_ChangedAtUtc",
                schema: "crm",
                table: "ContactSocialProfilesDataAudit",
                columns: new[] { "TenantId", "ParentId", "ChangedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContactSocialProfiles",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "ContactSocialProfilesDataAudit",
                schema: "crm");

            migrationBuilder.DropColumn(
                name: "NumberOfFollowers",
                schema: "crm",
                table: "ContactProfiles");

            migrationBuilder.DropColumn(
                name: "NumberOfFollowing",
                schema: "crm",
                table: "ContactProfiles");

            migrationBuilder.DropColumn(
                name: "NumberOfMentions",
                schema: "crm",
                table: "ContactProfiles");

            migrationBuilder.DropColumn(
                name: "NumberOfTweetsSent",
                schema: "crm",
                table: "ContactProfiles");

            migrationBuilder.DropColumn(
                name: "NumberOfTweetsWithTags",
                schema: "crm",
                table: "ContactProfiles");

            migrationBuilder.DropColumn(
                name: "SocialMetricsObservedOn",
                schema: "crm",
                table: "ContactProfiles");

            migrationBuilder.DropColumn(
                name: "SocialMetricsSource",
                schema: "crm",
                table: "ContactProfiles");
        }
    }
}
