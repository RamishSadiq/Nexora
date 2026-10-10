using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Modules.Events.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AttributeSections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Section",
                schema: "events",
                table: "AttributeDefinitions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "General Information");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Section",
                schema: "events",
                table: "AttributeDefinitions");
        }
    }
}
