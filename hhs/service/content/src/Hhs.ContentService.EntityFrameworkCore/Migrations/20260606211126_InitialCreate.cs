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
                name: "AppContentVisits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    AppContentId = table.Column<Guid>(type: "uuid", nullable: false),
                    VisitTimeLine = table.Column<long>(type: "bigint", nullable: false),
                    VisitResponse = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppContentVisits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Clients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ResponseStatistics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResponseStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ResponseTime = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    ResponseCount = table.Column<decimal>(type: "numeric(20,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResponseStatistics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AnalysisContents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    AnalysisDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OperationStatus = table.Column<int>(type: "integer", nullable: false),
                    OperationStatusDescription = table.Column<string>(type: "text", nullable: true),
                    NormalizedAnalysisId = table.Column<Guid>(type: "uuid", nullable: true),
                    VideoRequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    StorageVideoUrl = table.Column<string>(type: "text", nullable: true),
                    CorrelationId = table.Column<string>(type: "text", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalysisContents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnalysisContents_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AppContents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    SlugKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    OperationStatus = table.Column<int>(type: "integer", nullable: false),
                    OperationStatusDescription = table.Column<string>(type: "text", nullable: true),
                    NormalizedRequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReleaseTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VideoRequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    StorageVideoUrl = table.Column<string>(type: "text", nullable: true),
                    CorrelationId = table.Column<string>(type: "text", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppContents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppContents_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClientPathFilters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    PathFilterName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ClientFilterType = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientPathFilters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientPathFilters_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClientVideoGenerationHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    VideoGenerationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    VideoGenerationType = table.Column<int>(type: "integer", nullable: false),
                    ContentReferenceIds = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientVideoGenerationHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientVideoGenerationHistories_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisContents_ClientId",
                table: "AnalysisContents",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisContents_IsDeleted",
                table: "AnalysisContents",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisContents_OperationStatus",
                table: "AnalysisContents",
                column: "OperationStatus");

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisContents_TenantId",
                table: "AnalysisContents",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AppContents_ClientId",
                table: "AppContents",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_AppContents_IsDeleted",
                table: "AppContents",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_AppContents_IsDeleted_ClientId_SlugKey",
                table: "AppContents",
                columns: new[] { "IsDeleted", "ClientId", "SlugKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppContents_OperationStatus",
                table: "AppContents",
                column: "OperationStatus");

            migrationBuilder.CreateIndex(
                name: "IX_AppContents_TenantId",
                table: "AppContents",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AppContentVisits_ClientId_AppContentId",
                table: "AppContentVisits",
                columns: new[] { "ClientId", "AppContentId" });

            migrationBuilder.CreateIndex(
                name: "IX_AppContentVisits_ClientId_VisitResponse",
                table: "AppContentVisits",
                columns: new[] { "ClientId", "VisitResponse" });

            migrationBuilder.CreateIndex(
                name: "IX_AppContentVisits_ClientId_VisitTimeLine",
                table: "AppContentVisits",
                columns: new[] { "ClientId", "VisitTimeLine" });

            migrationBuilder.CreateIndex(
                name: "IX_ClientPathFilters_ClientId",
                table: "ClientPathFilters",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_IsDeleted",
                table: "Clients",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_TenantId_DomainName",
                table: "Clients",
                columns: new[] { "TenantId", "DomainName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClientVideoGenerationHistories_ClientId_VideoGenerationType",
                table: "ClientVideoGenerationHistories",
                columns: new[] { "ClientId", "VideoGenerationType" });

            migrationBuilder.CreateIndex(
                name: "IX_ResponseStatistics_ClientId_ResponseStatus",
                table: "ResponseStatistics",
                columns: new[] { "ClientId", "ResponseStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_ResponseStatistics_ClientId_ResponseTime",
                table: "ResponseStatistics",
                columns: new[] { "ClientId", "ResponseTime" });

            migrationBuilder.CreateIndex(
                name: "IX_ResponseStatistics_TenantId",
                table: "ResponseStatistics",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnalysisContents");

            migrationBuilder.DropTable(
                name: "AppContents");

            migrationBuilder.DropTable(
                name: "AppContentVisits");

            migrationBuilder.DropTable(
                name: "ClientPathFilters");

            migrationBuilder.DropTable(
                name: "ClientVideoGenerationHistories");

            migrationBuilder.DropTable(
                name: "ResponseStatistics");

            migrationBuilder.DropTable(
                name: "Clients");
        }
    }
}
