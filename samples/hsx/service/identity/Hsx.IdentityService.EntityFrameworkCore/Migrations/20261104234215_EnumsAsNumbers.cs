using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hsx.IdentityService.EntityFrameworkCore.Migrations
{
    /// <summary>Every enum is stored by its number and not by its name (framework ADR-0148). The rows that are there are converted, in both directions.</summary>
    public partial class EnumsAsNumbers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Tenant.TenantType: the name of the member becomes its number (Npgsql does not write the USING that a change of the type of a column needs)
            migrationBuilder.Sql("UPDATE \"Tenant\" SET \"TenantType\" = CASE \"TenantType\" WHEN 'Unknown' THEN '0' WHEN 'System' THEN '1' WHEN 'Reseller' THEN '2' ELSE \"TenantType\" END;");
            migrationBuilder.Sql("ALTER TABLE \"Tenant\" ALTER COLUMN \"TenantType\" DROP DEFAULT;");
            migrationBuilder.Sql("ALTER TABLE \"Tenant\" ALTER COLUMN \"TenantType\" TYPE integer USING \"TenantType\"::integer;");

            // Subscription.ProductType: the name of the member becomes its number (Npgsql does not write the USING that a change of the type of a column needs)
            migrationBuilder.Sql("UPDATE \"Subscription\" SET \"ProductType\" = CASE \"ProductType\" WHEN 'Unknown' THEN '0' WHEN 'VideoPlatform' THEN '11' WHEN 'VerticalVideo' THEN '12' WHEN 'Podcast' THEN '13' WHEN 'EInvoice' THEN '14' ELSE \"ProductType\" END;");
            migrationBuilder.Sql("ALTER TABLE \"Subscription\" ALTER COLUMN \"ProductType\" DROP DEFAULT;");
            migrationBuilder.Sql("ALTER TABLE \"Subscription\" ALTER COLUMN \"ProductType\" TYPE integer USING \"ProductType\"::integer;");

            // Permission.Type: the name of the member becomes its number (Npgsql does not write the USING that a change of the type of a column needs)
            migrationBuilder.Sql("UPDATE \"Permission\" SET \"Type\" = CASE \"Type\" WHEN 'Page' THEN '1' WHEN 'Endpoint' THEN '2' WHEN 'Operation' THEN '3' ELSE \"Type\" END;");
            migrationBuilder.Sql("ALTER TABLE \"Permission\" ALTER COLUMN \"Type\" DROP DEFAULT;");
            migrationBuilder.Sql("ALTER TABLE \"Permission\" ALTER COLUMN \"Type\" TYPE integer USING \"Type\"::integer;");

            // MenuPermissionMap.RelationType: the name of the member becomes its number (Npgsql does not write the USING that a change of the type of a column needs)
            migrationBuilder.Sql("UPDATE \"MenuPermissionMap\" SET \"RelationType\" = CASE \"RelationType\" WHEN 'Visibility' THEN '1' WHEN 'Required' THEN '2' WHEN 'Action' THEN '4' WHEN 'Operation' THEN '5' ELSE \"RelationType\" END;");
            migrationBuilder.Sql("ALTER TABLE \"MenuPermissionMap\" ALTER COLUMN \"RelationType\" DROP DEFAULT;");
            migrationBuilder.Sql("ALTER TABLE \"MenuPermissionMap\" ALTER COLUMN \"RelationType\" TYPE integer USING \"RelationType\"::integer;");

            // Menu.NodeType: the name of the member becomes its number (Npgsql does not write the USING that a change of the type of a column needs)
            migrationBuilder.Sql("UPDATE \"Menu\" SET \"NodeType\" = CASE \"NodeType\" WHEN 'Caption' THEN '1' WHEN 'Node' THEN '2' WHEN 'Leaf' THEN '3' ELSE \"NodeType\" END;");
            migrationBuilder.Sql("ALTER TABLE \"Menu\" ALTER COLUMN \"NodeType\" DROP DEFAULT;");
            migrationBuilder.Sql("ALTER TABLE \"Menu\" ALTER COLUMN \"NodeType\" TYPE integer USING \"NodeType\"::integer;");

            // Menu.DockType: the name of the member becomes its number (Npgsql does not write the USING that a change of the type of a column needs)
            migrationBuilder.Sql("UPDATE \"Menu\" SET \"DockType\" = CASE \"DockType\" WHEN 'SidebarMenu' THEN '1' WHEN 'TopbarMenu' THEN '2' ELSE \"DockType\" END;");
            migrationBuilder.Sql("ALTER TABLE \"Menu\" ALTER COLUMN \"DockType\" DROP DEFAULT;");
            migrationBuilder.Sql("ALTER TABLE \"Menu\" ALTER COLUMN \"DockType\" TYPE integer USING \"DockType\"::integer;");

            // Menu.ChannelType: the name of the member becomes its number (Npgsql does not write the USING that a change of the type of a column needs)
            migrationBuilder.Sql("UPDATE \"Menu\" SET \"ChannelType\" = CASE \"ChannelType\" WHEN 'Mobile' THEN '1' WHEN 'BackOffice' THEN '2' ELSE \"ChannelType\" END;");
            migrationBuilder.Sql("ALTER TABLE \"Menu\" ALTER COLUMN \"ChannelType\" DROP DEFAULT;");
            migrationBuilder.Sql("ALTER TABLE \"Menu\" ALTER COLUMN \"ChannelType\" TYPE integer USING \"ChannelType\"::integer;");

            // AppRole.RoleType: the name of the member becomes its number (Npgsql does not write the USING that a change of the type of a column needs)
            migrationBuilder.Sql("UPDATE \"AppRole\" SET \"RoleType\" = CASE \"RoleType\" WHEN 'System' THEN '1' WHEN 'Tenant' THEN '2' WHEN 'Company' THEN '3' WHEN 'Customer' THEN '4' ELSE \"RoleType\" END;");
            migrationBuilder.Sql("ALTER TABLE \"AppRole\" ALTER COLUMN \"RoleType\" DROP DEFAULT;");
            migrationBuilder.Sql("ALTER TABLE \"AppRole\" ALTER COLUMN \"RoleType\" TYPE integer USING \"RoleType\"::integer;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE \"Tenant\" ALTER COLUMN \"TenantType\" TYPE character varying(30) USING \"TenantType\"::text;");
            migrationBuilder.Sql("UPDATE \"Tenant\" SET \"TenantType\" = CASE \"TenantType\" WHEN '0' THEN 'Unknown' WHEN '1' THEN 'System' WHEN '2' THEN 'Reseller' ELSE \"TenantType\" END;");

            migrationBuilder.Sql("ALTER TABLE \"Subscription\" ALTER COLUMN \"ProductType\" TYPE character varying(30) USING \"ProductType\"::text;");
            migrationBuilder.Sql("UPDATE \"Subscription\" SET \"ProductType\" = CASE \"ProductType\" WHEN '0' THEN 'Unknown' WHEN '11' THEN 'VideoPlatform' WHEN '12' THEN 'VerticalVideo' WHEN '13' THEN 'Podcast' WHEN '14' THEN 'EInvoice' ELSE \"ProductType\" END;");

            migrationBuilder.Sql("ALTER TABLE \"Permission\" ALTER COLUMN \"Type\" TYPE character varying(20) USING \"Type\"::text;");
            migrationBuilder.Sql("UPDATE \"Permission\" SET \"Type\" = CASE \"Type\" WHEN '1' THEN 'Page' WHEN '2' THEN 'Endpoint' WHEN '3' THEN 'Operation' ELSE \"Type\" END;");

            migrationBuilder.Sql("ALTER TABLE \"MenuPermissionMap\" ALTER COLUMN \"RelationType\" TYPE character varying(20) USING \"RelationType\"::text;");
            migrationBuilder.Sql("UPDATE \"MenuPermissionMap\" SET \"RelationType\" = CASE \"RelationType\" WHEN '1' THEN 'Visibility' WHEN '2' THEN 'Required' WHEN '4' THEN 'Action' WHEN '5' THEN 'Operation' ELSE \"RelationType\" END;");

            migrationBuilder.Sql("ALTER TABLE \"Menu\" ALTER COLUMN \"NodeType\" TYPE character varying(20) USING \"NodeType\"::text;");
            migrationBuilder.Sql("UPDATE \"Menu\" SET \"NodeType\" = CASE \"NodeType\" WHEN '1' THEN 'Caption' WHEN '2' THEN 'Node' WHEN '3' THEN 'Leaf' ELSE \"NodeType\" END;");

            migrationBuilder.Sql("ALTER TABLE \"Menu\" ALTER COLUMN \"DockType\" TYPE character varying(20) USING \"DockType\"::text;");
            migrationBuilder.Sql("UPDATE \"Menu\" SET \"DockType\" = CASE \"DockType\" WHEN '1' THEN 'SidebarMenu' WHEN '2' THEN 'TopbarMenu' ELSE \"DockType\" END;");

            migrationBuilder.Sql("ALTER TABLE \"Menu\" ALTER COLUMN \"ChannelType\" TYPE character varying(20) USING \"ChannelType\"::text;");
            migrationBuilder.Sql("UPDATE \"Menu\" SET \"ChannelType\" = CASE \"ChannelType\" WHEN '1' THEN 'Mobile' WHEN '2' THEN 'BackOffice' ELSE \"ChannelType\" END;");

            migrationBuilder.Sql("ALTER TABLE \"AppRole\" ALTER COLUMN \"RoleType\" TYPE character varying(20) USING \"RoleType\"::text;");
            migrationBuilder.Sql("UPDATE \"AppRole\" SET \"RoleType\" = CASE \"RoleType\" WHEN '1' THEN 'System' WHEN '2' THEN 'Tenant' WHEN '3' THEN 'Company' WHEN '4' THEN 'Customer' ELSE \"RoleType\" END;");
        }
    }
}
