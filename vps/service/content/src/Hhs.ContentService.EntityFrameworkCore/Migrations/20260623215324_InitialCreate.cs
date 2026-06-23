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
                name: "analysis_contents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CorrelationId = table.Column<string>(type: "text", nullable: true),
                    ScopeKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DomainName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    NormalizeStatus = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    NormalizeRequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    VideoStatus = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    VideoRequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    FinalVideoUrl = table.Column<string>(type: "text", nullable: true),
                    LastFacility = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    LastError = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_analysis_contents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "content_inbox_messages",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: true),
                    EventName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_inbox_messages", x => x.EventId);
                });

            migrationBuilder.CreateTable(
                name: "customer_contents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CorrelationId = table.Column<string>(type: "text", nullable: true),
                    ScopeKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DomainName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ContentKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SlugKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    NormalizeStatus = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    NormalizeRequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    VideoStatus = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    VideoRequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    AudioRequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    FinalVideoUrl = table.Column<string>(type: "text", nullable: true),
                    LastFacility = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    LastError = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_contents", x => x.Id);
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
                name: "analysis_content_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AnalysisContentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerContentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_analysis_content_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_analysis_content_items_analysis_contents_AnalysisContentId",
                        column: x => x.AnalysisContentId,
                        principalTable: "analysis_contents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_analysis_content_items_customer_contents_CustomerContentId",
                        column: x => x.CustomerContentId,
                        principalTable: "customer_contents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                name: "IX_analysis_content_items_AnalysisContentId_CustomerContentId",
                table: "analysis_content_items",
                columns: new[] { "AnalysisContentId", "CustomerContentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_analysis_content_items_AnalysisContentId_SortOrder",
                table: "analysis_content_items",
                columns: new[] { "AnalysisContentId", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_analysis_content_items_CustomerContentId",
                table: "analysis_content_items",
                column: "CustomerContentId");

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
                name: "analysis_content_items");

            migrationBuilder.DropTable(
                name: "content_inbox_messages");

            migrationBuilder.DropTable(
                name: "ContentVideoGenerationLimits");

            migrationBuilder.DropTable(
                name: "analysis_contents");

            migrationBuilder.DropTable(
                name: "customer_contents");

            migrationBuilder.DropTable(
                name: "CustomerVpSettings");
        }
    }
}
