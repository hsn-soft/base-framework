using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hhs.AdministrationService.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppMenus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    UniqueCode = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ChannelType = table.Column<int>(type: "integer", nullable: false),
                    DockType = table.Column<int>(type: "integer", nullable: false),
                    NodeType = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Route = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Icon = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppMenus", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppMenus_AppMenus_ParentId",
                        column: x => x.ParentId,
                        principalTable: "AppMenus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EventInboxMessages",
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
                name: "PermissionGrants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ProviderName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProviderKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionGrants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UniqueCode = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    PermissionType = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppMenuPermissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AppMenuId = table.Column<Guid>(type: "uuid", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RelationType = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppMenuPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppMenuPermissions_AppMenus_AppMenuId",
                        column: x => x.AppMenuId,
                        principalTable: "AppMenus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppMenuPermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppRolePermissionConstraints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AppRoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppRolePermissionConstraints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppRolePermissionConstraints_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppRolePermissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AppRoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppRolePermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppRolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PermissionDependencies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    DependsOnPermissionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionDependencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PermissionDependencies_Permissions_DependsOnPermissionId",
                        column: x => x.DependsOnPermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PermissionDependencies_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppMenuPermissions_AppMenuId_PermissionId",
                table: "AppMenuPermissions",
                columns: new[] { "AppMenuId", "PermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppMenuPermissions_PermissionId",
                table: "AppMenuPermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_AppMenus_ParentId",
                table: "AppMenus",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_AppMenus_UniqueCode",
                table: "AppMenus",
                column: "UniqueCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppRolePermissionConstraints_AppRoleId_PermissionId",
                table: "AppRolePermissionConstraints",
                columns: new[] { "AppRoleId", "PermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppRolePermissionConstraints_PermissionId",
                table: "AppRolePermissionConstraints",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_AppRolePermissions_AppRoleId_PermissionId",
                table: "AppRolePermissions",
                columns: new[] { "AppRoleId", "PermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppRolePermissions_PermissionId",
                table: "AppRolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_EventInboxMessages_Status",
                table: "EventInboxMessages",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionDependencies_DependsOnPermissionId",
                table: "PermissionDependencies",
                column: "DependsOnPermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionDependencies_PermissionId_DependsOnPermissionId",
                table: "PermissionDependencies",
                columns: new[] { "PermissionId", "DependsOnPermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PermissionGrants_Name_ProviderName_ProviderKey",
                table: "PermissionGrants",
                columns: new[] { "Name", "ProviderName", "ProviderKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_UniqueCode",
                table: "Permissions",
                column: "UniqueCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppMenuPermissions");

            migrationBuilder.DropTable(
                name: "AppRolePermissionConstraints");

            migrationBuilder.DropTable(
                name: "AppRolePermissions");

            migrationBuilder.DropTable(
                name: "EventInboxMessages");

            migrationBuilder.DropTable(
                name: "PermissionDependencies");

            migrationBuilder.DropTable(
                name: "PermissionGrants");

            migrationBuilder.DropTable(
                name: "AppMenus");

            migrationBuilder.DropTable(
                name: "Permissions");
        }
    }
}
