using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Modules.Membership.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MembershipFinancialArrangement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "InvoiceToAccount",
                schema: "membership",
                table: "Subscriptions",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InvoicingAccountId",
                schema: "membership",
                table: "Subscriptions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvoicingAccountName",
                schema: "membership",
                table: "Subscriptions",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "NextRenewalDate",
                schema: "membership",
                table: "Subscriptions",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PaidById",
                schema: "membership",
                table: "Subscriptions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaidByKind",
                schema: "membership",
                table: "Subscriptions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaidByName",
                schema: "membership",
                table: "Subscriptions",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentFrequency",
                schema: "membership",
                table: "Subscriptions",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                schema: "membership",
                table: "Subscriptions",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PromotionCode",
                schema: "membership",
                table: "Subscriptions",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "RecordedRenewalDate",
                schema: "membership",
                table: "Subscriptions",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RenewalMonth",
                schema: "membership",
                table: "Subscriptions",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InvoiceToAccount",
                schema: "membership",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "InvoicingAccountId",
                schema: "membership",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "InvoicingAccountName",
                schema: "membership",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "NextRenewalDate",
                schema: "membership",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "PaidById",
                schema: "membership",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "PaidByKind",
                schema: "membership",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "PaidByName",
                schema: "membership",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "PaymentFrequency",
                schema: "membership",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                schema: "membership",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "PromotionCode",
                schema: "membership",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "RecordedRenewalDate",
                schema: "membership",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "RenewalMonth",
                schema: "membership",
                table: "Subscriptions");
        }
    }
}
