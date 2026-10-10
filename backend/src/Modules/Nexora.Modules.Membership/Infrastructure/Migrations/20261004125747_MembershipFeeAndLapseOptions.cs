using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Modules.Membership.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MembershipFeeAndLapseOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ExcludedFromLapse",
                schema: "membership",
                table: "Subscriptions",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ExemptJoiningFee",
                schema: "membership",
                table: "Subscriptions",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "FreeMembership",
                schema: "membership",
                table: "Subscriptions",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "JoiningFee",
                schema: "membership",
                table: "Policies",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "ExemptJoiningFee",
                schema: "membership",
                table: "Applications",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "FreeMembership",
                schema: "membership",
                table: "Applications",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExcludedFromLapse",
                schema: "membership",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "ExemptJoiningFee",
                schema: "membership",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "FreeMembership",
                schema: "membership",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "JoiningFee",
                schema: "membership",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "ExemptJoiningFee",
                schema: "membership",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "FreeMembership",
                schema: "membership",
                table: "Applications");
        }
    }
}
