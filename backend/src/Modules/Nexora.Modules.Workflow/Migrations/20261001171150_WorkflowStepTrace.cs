using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Modules.Workflow.Migrations
{
    /// <inheritdoc />
    public partial class WorkflowStepTrace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InputsJson",
                schema: "workflow",
                table: "Steps",
                type: "nvarchar(max)",
                maxLength: 262144,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OutputsJson",
                schema: "workflow",
                table: "Steps",
                type: "nvarchar(max)",
                maxLength: 262144,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Scope",
                schema: "workflow",
                table: "Steps",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InputsJson",
                schema: "workflow",
                table: "Steps");

            migrationBuilder.DropColumn(
                name: "OutputsJson",
                schema: "workflow",
                table: "Steps");

            migrationBuilder.DropColumn(
                name: "Scope",
                schema: "workflow",
                table: "Steps");
        }
    }
}
