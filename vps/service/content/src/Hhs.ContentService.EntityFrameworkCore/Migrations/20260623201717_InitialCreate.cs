using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hhs.ContentService.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomerVpSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ScopeKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DomainName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsBlocked = table.Column<bool>(type: "boolean", nullable: false),
                    DailyDirectVideoGenerationStartedUtcHour = table.Column<int>(type: "integer", nullable: false),
                    DailyDirectVideoGenerationLimit = table.Column<int>(type: "integer", nullable: false),
                    DailyTrendVideoGenerationStartedUtcHour = table.Column<int>(type: "integer", nullable: false),
                    DailyTrendVideoGenerationLimit = table.Column<int>(type: "integer", nullable: false),
                    DailyTrendVideoWaitStatisticHour = table.Column<int>(type: "integer", nullable: false),
                    DailyTrendVideoMinVisitCount = table.Column<int>(type: "integer", nullable: false),
                    DailyAnalysisVideoGenerationStartedUtcHour = table.Column<int>(type: "integer", nullable: false),
                    DailyAnalysisVideoGenerationLimit = table.Column<int>(type: "integer", nullable: false),
                    IncludePathFilters = table.Column<string>(type: "jsonb", nullable: true),
                    ExcludePathFilters = table.Column<string>(type: "jsonb", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerVpSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContentVideoGenerationLimits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScopeKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    VideoGenerationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    VideoGenerationType = table.Column<int>(type: "integer", nullable: false),
                    ContentReferenceIds = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CustomerVpSettingId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentVideoGenerationLimits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentVideoGenerationLimits_CustomerVpSettings_CustomerVpS~",
                        column: x => x.CustomerVpSettingId,
                        principalTable: "CustomerVpSettings",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContentVideoGenerationLimits_CustomerVpSettingId",
                table: "ContentVideoGenerationLimits",
                column: "CustomerVpSettingId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentVideoGenerationLimits_ScopeKey_VideoGenerationType",
                table: "ContentVideoGenerationLimits",
                columns: new[] { "ScopeKey", "VideoGenerationType" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerVpSettings_DomainName",
                table: "CustomerVpSettings",
                column: "DomainName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerVpSettings_ScopeKey",
                table: "CustomerVpSettings",
                column: "ScopeKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContentVideoGenerationLimits");

            migrationBuilder.DropTable(
                name: "CustomerVpSettings");
        }
    }
}
