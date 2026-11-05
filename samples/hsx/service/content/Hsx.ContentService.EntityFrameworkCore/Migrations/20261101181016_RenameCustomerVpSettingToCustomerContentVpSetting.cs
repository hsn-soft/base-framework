using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hsx.ContentService.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class RenameCustomerVpSettingToCustomerContentVpSetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerVpSetting");

            migrationBuilder.CreateTable(
                name: "CustomerContentVpSetting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScopeKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DomainName = table.Column<string>(type: "character varying(253)", maxLength: 253, nullable: false),
                    NormalizedDomainName = table.Column<string>(type: "character varying(253)", maxLength: 253, nullable: false),
                    IsBlocked = table.Column<bool>(type: "boolean", nullable: false),
                    DailyDirectVideoGenerationLimit = table.Column<int>(type: "integer", nullable: false),
                    DailyTrendVideoGenerationLimit = table.Column<int>(type: "integer", nullable: false),
                    DailyAnalysisVideoGenerationLimit = table.Column<int>(type: "integer", nullable: false),
                    IsAnalysisSourcesEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerContentVpSetting", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerContentVpSetting_IsBlocked",
                table: "CustomerContentVpSetting",
                column: "IsBlocked");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerContentVpSetting_ScopeKey",
                table: "CustomerContentVpSetting",
                column: "ScopeKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerContentVpSetting");

            migrationBuilder.CreateTable(
                name: "CustomerVpSetting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DailyAnalysisVideoGenerationLimit = table.Column<int>(type: "integer", nullable: false),
                    DailyDirectVideoGenerationLimit = table.Column<int>(type: "integer", nullable: false),
                    DailyTrendVideoGenerationLimit = table.Column<int>(type: "integer", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DomainName = table.Column<string>(type: "character varying(253)", maxLength: 253, nullable: false),
                    IsAnalysisSourcesEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    IsBlocked = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    NormalizedDomainName = table.Column<string>(type: "character varying(253)", maxLength: 253, nullable: false),
                    ScopeKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerVpSetting", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerVpSetting_IsBlocked",
                table: "CustomerVpSetting",
                column: "IsBlocked");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerVpSetting_ScopeKey",
                table: "CustomerVpSetting",
                column: "ScopeKey");
        }
    }
}
