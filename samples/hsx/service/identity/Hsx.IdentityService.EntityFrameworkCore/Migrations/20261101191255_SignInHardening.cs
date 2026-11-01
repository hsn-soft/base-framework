using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hsx.IdentityService.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class SignInHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FailedLoginCount",
                table: "AppUser",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LockoutEndAt",
                table: "AppUser",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AuthLoginAudit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IsSuccess = table.Column<bool>(type: "boolean", nullable: false),
                    FailureReason = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthLoginAudit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuthPasswordPolicy",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    MinLength = table.Column<int>(type: "integer", nullable: false),
                    RequireDigit = table.Column<bool>(type: "boolean", nullable: false),
                    RequireLower = table.Column<bool>(type: "boolean", nullable: false),
                    RequireUpper = table.Column<bool>(type: "boolean", nullable: false),
                    RequireNonAlphanumeric = table.Column<bool>(type: "boolean", nullable: false),
                    MaxFailedLoginCount = table.Column<int>(type: "integer", nullable: false),
                    LockoutMinutes = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthPasswordPolicy", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuthRefreshToken",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReplacedByTokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthRefreshToken", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuthLoginAudit_CreatedAt",
                table: "AuthLoginAudit",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuthLoginAudit_UserId",
                table: "AuthLoginAudit",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthPasswordPolicy_TenantId",
                table: "AuthPasswordPolicy",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuthRefreshToken_TokenHash",
                table: "AuthRefreshToken",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuthRefreshToken_UserId",
                table: "AuthRefreshToken",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuthLoginAudit");

            migrationBuilder.DropTable(
                name: "AuthPasswordPolicy");

            migrationBuilder.DropTable(
                name: "AuthRefreshToken");

            migrationBuilder.DropColumn(
                name: "FailedLoginCount",
                table: "AppUser");

            migrationBuilder.DropColumn(
                name: "LockoutEndAt",
                table: "AppUser");
        }
    }
}
