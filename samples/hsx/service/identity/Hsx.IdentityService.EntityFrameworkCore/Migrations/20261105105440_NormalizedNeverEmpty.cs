using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hsx.IdentityService.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class NormalizedNeverEmpty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_Tenant_NormalizedName_NotEmpty",
                table: "Tenant",
                sql: "\"NormalizedName\" <> ''");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Customer_NormalizedDomain_NotEmpty",
                table: "Customer",
                sql: "\"NormalizedDomain\" <> ''");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Company_NormalizedName_NotEmpty",
                table: "Company",
                sql: "\"NormalizedName\" <> ''");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AppUser_NormalizedEmail_NotEmpty",
                table: "AppUser",
                sql: "\"NormalizedEmail\" <> ''");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AppUser_NormalizedUserName_NotEmpty",
                table: "AppUser",
                sql: "\"NormalizedUserName\" <> ''");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AppRole_NormalizedName_NotEmpty",
                table: "AppRole",
                sql: "\"NormalizedName\" <> ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Tenant_NormalizedName_NotEmpty",
                table: "Tenant");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Customer_NormalizedDomain_NotEmpty",
                table: "Customer");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Company_NormalizedName_NotEmpty",
                table: "Company");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AppUser_NormalizedEmail_NotEmpty",
                table: "AppUser");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AppUser_NormalizedUserName_NotEmpty",
                table: "AppUser");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AppRole_NormalizedName_NotEmpty",
                table: "AppRole");
        }
    }
}
