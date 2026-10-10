using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Modules.Membership.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MembershipApplicationRequestedDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "membership",
                table: "Applications",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "InvoiceToAccount",
                schema: "membership",
                table: "Applications",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InvoicingAccountId",
                schema: "membership",
                table: "Applications",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvoicingAccountName",
                schema: "membership",
                table: "Applications",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LifecyclePurpose",
                schema: "membership",
                table: "Applications",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentFrequency",
                schema: "membership",
                table: "Applications",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                schema: "membership",
                table: "Applications",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RequestedCategoryId",
                schema: "membership",
                table: "Applications",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestedCategoryName",
                schema: "membership",
                table: "Applications",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RequestedMemberTypeId",
                schema: "membership",
                table: "Applications",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestedMemberTypeName",
                schema: "membership",
                table: "Applications",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RequestedSubscriptionBandId",
                schema: "membership",
                table: "Applications",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestedSubscriptionBandName",
                schema: "membership",
                table: "Applications",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StaffRemarks",
                schema: "membership",
                table: "Applications",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "StatusEffectiveOn",
                schema: "membership",
                table: "Applications",
                type: "date",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Applications_TenantId_RequestedCategoryId",
                schema: "membership",
                table: "Applications",
                columns: new[] { "TenantId", "RequestedCategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_Applications_TenantId_RequestedMemberTypeId",
                schema: "membership",
                table: "Applications",
                columns: new[] { "TenantId", "RequestedMemberTypeId" });

            migrationBuilder.CreateIndex(
                name: "IX_Applications_TenantId_RequestedSubscriptionBandId",
                schema: "membership",
                table: "Applications",
                columns: new[] { "TenantId", "RequestedSubscriptionBandId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Applications_SetupValues_TenantId_RequestedCategoryId",
                schema: "membership",
                table: "Applications",
                columns: new[] { "TenantId", "RequestedCategoryId" },
                principalSchema: "membership",
                principalTable: "SetupValues",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Applications_SetupValues_TenantId_RequestedMemberTypeId",
                schema: "membership",
                table: "Applications",
                columns: new[] { "TenantId", "RequestedMemberTypeId" },
                principalSchema: "membership",
                principalTable: "SetupValues",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Applications_SetupValues_TenantId_RequestedSubscriptionBandId",
                schema: "membership",
                table: "Applications",
                columns: new[] { "TenantId", "RequestedSubscriptionBandId" },
                principalSchema: "membership",
                principalTable: "SetupValues",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Applications_SetupValues_TenantId_RequestedCategoryId",
                schema: "membership",
                table: "Applications");

            migrationBuilder.DropForeignKey(
                name: "FK_Applications_SetupValues_TenantId_RequestedMemberTypeId",
                schema: "membership",
                table: "Applications");

            migrationBuilder.DropForeignKey(
                name: "FK_Applications_SetupValues_TenantId_RequestedSubscriptionBandId",
                schema: "membership",
                table: "Applications");

            migrationBuilder.DropIndex(
                name: "IX_Applications_TenantId_RequestedCategoryId",
                schema: "membership",
                table: "Applications");

            migrationBuilder.DropIndex(
                name: "IX_Applications_TenantId_RequestedMemberTypeId",
                schema: "membership",
                table: "Applications");

            migrationBuilder.DropIndex(
                name: "IX_Applications_TenantId_RequestedSubscriptionBandId",
                schema: "membership",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "Description",
                schema: "membership",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "InvoiceToAccount",
                schema: "membership",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "InvoicingAccountId",
                schema: "membership",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "InvoicingAccountName",
                schema: "membership",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "LifecyclePurpose",
                schema: "membership",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "PaymentFrequency",
                schema: "membership",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                schema: "membership",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "RequestedCategoryId",
                schema: "membership",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "RequestedCategoryName",
                schema: "membership",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "RequestedMemberTypeId",
                schema: "membership",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "RequestedMemberTypeName",
                schema: "membership",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "RequestedSubscriptionBandId",
                schema: "membership",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "RequestedSubscriptionBandName",
                schema: "membership",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "StaffRemarks",
                schema: "membership",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "StatusEffectiveOn",
                schema: "membership",
                table: "Applications");
        }
    }
}
