using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hhs.FeedRService.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.CreateTable(
                name: "AdNetworks",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NetworkCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdNetworks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EventInboxMessages",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: true),
                    EventName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProcessedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventInboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MongoToPostgresMappings",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MongoDbDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    DerivedRecordIds = table.Column<string>(type: "text", nullable: true),
                    DerivedRecordCount = table.Column<int>(type: "integer", nullable: false),
                    DerivationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DerivationStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MongoToPostgresMappings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AdUnitTopLevels",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AdNetworkId = table.Column<Guid>(type: "uuid", nullable: false),
                    AdUnitTopLevelCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdUnitTopLevels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdUnitTopLevels_AdNetworks_AdNetworkId",
                        column: x => x.AdNetworkId,
                        principalSchema: "public",
                        principalTable: "AdNetworks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AdUnitClients",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    AdUnitTopLevelId = table.Column<Guid>(type: "uuid", nullable: false),
                    AdUnitCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdUnitClients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdUnitClients_AdUnitTopLevels_AdUnitTopLevelId",
                        column: x => x.AdUnitTopLevelId,
                        principalSchema: "public",
                        principalTable: "AdUnitTopLevels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DailyReportResponses",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    AdUnitClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportDate = table.Column<DateTime>(type: "date", nullable: false),
                    DemandChannel = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    DemandSubchannelName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    OrderId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    OrderName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    CodeServedCount = table.Column<long>(type: "bigint", nullable: false),
                    Impressions = table.Column<long>(type: "bigint", nullable: false),
                    Revenue = table.Column<double>(type: "double precision", nullable: false),
                    ActiveViewEligibleImpressions = table.Column<long>(type: "bigint", nullable: false),
                    AverageEcpm = table.Column<double>(type: "double precision", nullable: false),
                    SourceRowCount = table.Column<int>(type: "integer", nullable: false),
                    SourceMongoDbId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyReportResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyReportResponses_AdUnitClients_AdUnitClientId",
                        column: x => x.AdUnitClientId,
                        principalSchema: "public",
                        principalTable: "AdUnitClients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdNetworks_NetworkCode",
                schema: "public",
                table: "AdNetworks",
                column: "NetworkCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdUnitClients_AdUnitTopLevelId_AdUnitCode",
                schema: "public",
                table: "AdUnitClients",
                columns: new[] { "AdUnitTopLevelId", "AdUnitCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdUnitClients_ClientId",
                schema: "public",
                table: "AdUnitClients",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_AdUnitClients_TenantId",
                schema: "public",
                table: "AdUnitClients",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AdUnitTopLevels_AdNetworkId_AdUnitTopLevelCode",
                schema: "public",
                table: "AdUnitTopLevels",
                columns: new[] { "AdNetworkId", "AdUnitTopLevelCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdUnitTopLevels_AdUnitTopLevelCode",
                schema: "public",
                table: "AdUnitTopLevels",
                column: "AdUnitTopLevelCode");

            migrationBuilder.CreateIndex(
                name: "IX_DailyReportResponses_AdUnitClientId_ReportDate_DemandChanne~",
                schema: "public",
                table: "DailyReportResponses",
                columns: new[] { "AdUnitClientId", "ReportDate", "DemandChannel", "DemandSubchannelName", "OrderId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyReportResponses_ClientId",
                schema: "public",
                table: "DailyReportResponses",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyReportResponses_ClientId_ReportDate",
                schema: "public",
                table: "DailyReportResponses",
                columns: new[] { "ClientId", "ReportDate" });

            migrationBuilder.CreateIndex(
                name: "IX_DailyReportResponses_ReportDate",
                schema: "public",
                table: "DailyReportResponses",
                column: "ReportDate");

            migrationBuilder.CreateIndex(
                name: "IX_DailyReportResponses_TenantId",
                schema: "public",
                table: "DailyReportResponses",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_EventInboxMessages_Status",
                schema: "public",
                table: "EventInboxMessages",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_MongoToPostgresMappings_DerivationStatus",
                schema: "public",
                table: "MongoToPostgresMappings",
                column: "DerivationStatus");

            migrationBuilder.CreateIndex(
                name: "IX_MongoToPostgresMappings_MongoDbDocumentId",
                schema: "public",
                table: "MongoToPostgresMappings",
                column: "MongoDbDocumentId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyReportResponses",
                schema: "public");

            migrationBuilder.DropTable(
                name: "EventInboxMessages",
                schema: "public");

            migrationBuilder.DropTable(
                name: "MongoToPostgresMappings",
                schema: "public");

            migrationBuilder.DropTable(
                name: "AdUnitClients",
                schema: "public");

            migrationBuilder.DropTable(
                name: "AdUnitTopLevels",
                schema: "public");

            migrationBuilder.DropTable(
                name: "AdNetworks",
                schema: "public");
        }
    }
}
