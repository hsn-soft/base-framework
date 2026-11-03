using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hsx.IdentityService.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class OwnershipModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EndedAt",
                table: "Subscription",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "AppRole",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CustomerId",
                table: "AppRole",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RoleType",
                table: "AppRole",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Subscription_CustomerId_ProductType",
                table: "Subscription",
                columns: new[] { "CustomerId", "ProductType" },
                unique: true,
                filter: "\"EndedAt\" IS NULL AND NOT \"IsDeleted\"");

            migrationBuilder.CreateIndex(
                name: "IX_AppRole_CompanyId",
                table: "AppRole",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_AppRole_CustomerId",
                table: "AppRole",
                column: "CustomerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Subscription_CustomerId_ProductType",
                table: "Subscription");

            migrationBuilder.DropIndex(
                name: "IX_AppRole_CompanyId",
                table: "AppRole");

            migrationBuilder.DropIndex(
                name: "IX_AppRole_CustomerId",
                table: "AppRole");

            migrationBuilder.DropColumn(
                name: "EndedAt",
                table: "Subscription");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "AppRole");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "AppRole");

            migrationBuilder.DropColumn(
                name: "RoleType",
                table: "AppRole");

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
    }
}
