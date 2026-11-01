using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hsx.ContentService.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnalysisContent",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScopeKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AnalysisDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    NormalizeStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    VideoStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    VideoCdnUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CurrentMilestone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    LastError = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_AnalysisContent", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AnalysisContentItem",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScopeKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AnalysisContentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerContentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    SourceTitle = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    SourceDescription = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SourceImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalysisContentItem", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerContent",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScopeKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ContentKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NormalizedContentKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SlugKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NormalizeStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    VideoStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    VideoCdnUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CurrentMilestone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    LastError = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_CustomerContent", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerContentVisit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScopeKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CustomerContentId = table.Column<Guid>(type: "uuid", nullable: false),
                    VisitTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    VisitResponse = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerContentVisit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerVpSetting",
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
                    table.PrimaryKey("PK_CustomerVpSetting", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisContent_ScopeKey",
                table: "AnalysisContent",
                column: "ScopeKey");

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisContentItem_AnalysisContentId",
                table: "AnalysisContentItem",
                column: "AnalysisContentId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisContentItem_CustomerContentId",
                table: "AnalysisContentItem",
                column: "CustomerContentId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisContentItem_ScopeKey",
                table: "AnalysisContentItem",
                column: "ScopeKey");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerContent_ScopeKey",
                table: "CustomerContent",
                column: "ScopeKey");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerContentVisit_CustomerContentId",
                table: "CustomerContentVisit",
                column: "CustomerContentId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerContentVisit_ScopeKey",
                table: "CustomerContentVisit",
                column: "ScopeKey");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerVpSetting_IsBlocked",
                table: "CustomerVpSetting",
                column: "IsBlocked");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerVpSetting_ScopeKey",
                table: "CustomerVpSetting",
                column: "ScopeKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnalysisContent");

            migrationBuilder.DropTable(
                name: "AnalysisContentItem");

            migrationBuilder.DropTable(
                name: "CustomerContent");

            migrationBuilder.DropTable(
                name: "CustomerContentVisit");

            migrationBuilder.DropTable(
                name: "CustomerVpSetting");
        }
    }
}
