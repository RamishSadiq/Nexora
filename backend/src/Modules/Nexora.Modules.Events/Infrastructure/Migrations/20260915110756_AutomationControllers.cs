using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Modules.Events.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AutomationControllers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AutomationConfigurations",
                schema: "events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    SectionsJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    RulesJson = table.Column<string>(type: "nvarchar(max)", maxLength: 100000, nullable: false),
                    Version = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutomationConfigurations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AutomationConfigurations_TenantId_EntityType",
                schema: "events",
                table: "AutomationConfigurations",
                columns: new[] { "TenantId", "EntityType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AutomationConfigurations",
                schema: "events");
        }
    }
}
