using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hsx.IdentityService.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthLoginAuditNormalizedUserNameOrEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NormalizedUserNameOrEmail",
                table: "AuthLoginAudits",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NormalizedUserNameOrEmail",
                table: "AuthLoginAudits");
        }
    }
}
