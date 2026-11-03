using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hsx.IdentityService.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class TenantManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ResellerTenantId",
                table: "Subscription",
                newName: "TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_Subscription_ResellerTenantId",
                table: "Subscription",
                newName: "IX_Subscription_TenantId");

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Customer",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "AccountTenantId",
                table: "Company",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Company",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Customer");

            migrationBuilder.DropColumn(
                name: "AccountTenantId",
                table: "Company");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Company");

            migrationBuilder.RenameColumn(
                name: "TenantId",
                table: "Subscription",
                newName: "ResellerTenantId");

            migrationBuilder.RenameIndex(
                name: "IX_Subscription_TenantId",
                table: "Subscription",
                newName: "IX_Subscription_ResellerTenantId");
        }
    }
}
