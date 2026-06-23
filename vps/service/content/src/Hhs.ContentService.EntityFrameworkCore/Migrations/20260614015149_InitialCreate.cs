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
                name: "AnalysisContents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ScopeKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    AnalysisDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OperationStatus = table.Column<int>(type: "integer", nullable: false),
                    OperationStatusDescription = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    NormalizedRequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    VideoRequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    StorageVideoUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalysisContents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerContents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ScopeKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SlugKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    OperationStatus = table.Column<int>(type: "integer", nullable: false),
                    OperationStatusDescription = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    NormalizedRequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReleaseTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VideoRequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    StorageVideoUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerContents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerContentVisits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScopeKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CustomerContentId = table.Column<Guid>(type: "uuid", nullable: false),
                    VisitTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    VisitResponse = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerContentVisits", x => x.Id);
                });

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
                name: "IX_AnalysisContents_AnalysisDate",
                table: "AnalysisContents",
                column: "AnalysisDate");

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisContents_OperationStatus",
                table: "AnalysisContents",
                column: "OperationStatus");

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisContents_ScopeKey",
                table: "AnalysisContents",
                column: "ScopeKey");

            migrationBuilder.CreateIndex(
                name: "IX_ContentVideoGenerationLimits_CustomerVpSettingId",
                table: "ContentVideoGenerationLimits",
                column: "CustomerVpSettingId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentVideoGenerationLimits_ScopeKey_VideoGenerationType",
                table: "ContentVideoGenerationLimits",
                columns: new[] { "ScopeKey", "VideoGenerationType" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerContents_OperationStatus",
                table: "CustomerContents",
                column: "OperationStatus");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerContents_ScopeKey_SlugKey_IsDeleted",
                table: "CustomerContents",
                columns: new[] { "ScopeKey", "SlugKey", "IsDeleted" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerContentVisits_CustomerContentId",
                table: "CustomerContentVisits",
                column: "CustomerContentId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerContentVisits_ScopeKey",
                table: "CustomerContentVisits",
                column: "ScopeKey");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerContentVisits_VisitResponse",
                table: "CustomerContentVisits",
                column: "VisitResponse");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerContentVisits_VisitTime",
                table: "CustomerContentVisits",
                column: "VisitTime");

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
                name: "AnalysisContents");

            migrationBuilder.DropTable(
                name: "ContentVideoGenerationLimits");

            migrationBuilder.DropTable(
                name: "CustomerContents");

            migrationBuilder.DropTable(
                name: "CustomerContentVisits");

            migrationBuilder.DropTable(
                name: "CustomerVpSettings");
        }
    }
}
