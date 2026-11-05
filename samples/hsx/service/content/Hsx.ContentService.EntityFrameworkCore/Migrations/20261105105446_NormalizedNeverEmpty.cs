using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hsx.ContentService.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class NormalizedNeverEmpty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_CustomerContentVpSetting_NormalizedDomainName_NotEmpty",
                table: "CustomerContentVpSetting",
                sql: "\"NormalizedDomainName\" <> ''");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CustomerContent_NormalizedContentKey_NotEmpty",
                table: "CustomerContent",
                sql: "\"NormalizedContentKey\" <> ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_CustomerContentVpSetting_NormalizedDomainName_NotEmpty",
                table: "CustomerContentVpSetting");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CustomerContent_NormalizedContentKey_NotEmpty",
                table: "CustomerContent");
        }
    }
}
