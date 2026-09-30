using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Modules.Crm.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountContactDepartment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Department",
                schema: "crm",
                table: "AccountContacts",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Department",
                schema: "crm",
                table: "AccountContacts");
        }
    }
}
