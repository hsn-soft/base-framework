using Hhs.AdministrationService.Domain.Enums;
using Hhs.AdministrationService.Domain.MenuDomain.Entities;
using Hhs.AdministrationService.Domain.PermissionDomain.Entities;
using Hhs.AdministrationService.EntityFrameworkCore.Context;
using Hhs.Shared.Helper.Permissions;
using Hhs.Shared.Helper.Utils;
using HsnSoft.Base.Logging.Abstracts;
using HsnSoft.Base.Text;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Hhs.AdministrationService.EntityFrameworkCore.Setup;

public static class AppMenuSeeder
{
    public static async Task SeedAsync(AdministrationServiceDbContext db, IAppConsoleLogger logger)
    {
        #region Mobile Menus

        #region Top Menus

        _ = await GetOrCreateAppMenuAsync(db, logger, channelType: ChannelTypes.MobileApp, dockType: MenuDockTypes.TopMenu,
            title: "Profile", icon: null, sortOrder: 0,
            nodeType: MenuNodeTypes.Leaf,
            uniqueCodeSuffix: "profile",
            route: "/profile");

        #endregion

        #region Left Menus

        var mobileDashboardMenu = await GetOrCreateAppMenuAsync(db, logger, channelType: ChannelTypes.MobileApp, dockType: MenuDockTypes.LeftMenu,
            title: "Dashboards", icon: null, sortOrder: 0,
            nodeType: MenuNodeTypes.Node,
            uniqueCodeSuffix: "dashboards");

        _ = await GetOrCreateAppMenuAsync(db, logger, channelType: ChannelTypes.MobileApp, dockType: MenuDockTypes.LeftMenu,
            title: "Summary", icon: null, sortOrder: 0,
            nodeType: MenuNodeTypes.Leaf,
            uniqueCodeSuffix: "dashboards.summary",
            route: "/dashboards/summary",
            parentId: mobileDashboardMenu.Id);

        _ = await GetOrCreateAppMenuAsync(db, logger, channelType: ChannelTypes.MobileApp, dockType: MenuDockTypes.LeftMenu,
            title: "Traffic", icon: null, sortOrder: 0,
            nodeType: MenuNodeTypes.Leaf,
            uniqueCodeSuffix: "dashboards.traffic",
            route: "/dashboards/traffic",
            parentId: mobileDashboardMenu.Id);

        #endregion

        #endregion

        #region Back-Office Menus

        #region Top Menus

        _ = await GetOrCreateAppMenuAsync(db, logger, channelType: ChannelTypes.BackOffice, dockType: MenuDockTypes.TopMenu,
            title: "Profile", icon: null, sortOrder: 0,
            nodeType: MenuNodeTypes.Leaf,
            uniqueCodeSuffix: "profile",
            route: "/profile");

        #endregion

        #region Left Menus

        #region Dashboards

        var dashboardMenu = await GetOrCreateAppMenuAsync(db, logger, channelType: ChannelTypes.BackOffice, dockType: MenuDockTypes.LeftMenu,
            title: "Dashboards", icon: null, sortOrder: 0,
            nodeType: MenuNodeTypes.Node,
            uniqueCodeSuffix: "dashboards");

        _ = await GetOrCreateAppMenuAsync(db, logger, channelType: ChannelTypes.BackOffice, dockType: MenuDockTypes.LeftMenu,
            title: "Summary", icon: null, sortOrder: 0,
            nodeType: MenuNodeTypes.Leaf,
            uniqueCodeSuffix: "dashboards.summary",
            route: "/dashboards/summary",
            parentId: dashboardMenu.Id);

        _ = await GetOrCreateAppMenuAsync(db, logger, channelType: ChannelTypes.BackOffice, dockType: MenuDockTypes.LeftMenu,
            title: "Traffic", icon: null, sortOrder: 0,
            nodeType: MenuNodeTypes.Leaf,
            uniqueCodeSuffix: "dashboards.traffic",
            route: "/dashboards/traffic",
            parentId: dashboardMenu.Id);

        #endregion

        _ = await GetOrCreateAppMenuAsync(db, logger, channelType: ChannelTypes.BackOffice, dockType: MenuDockTypes.LeftMenu,
            title: "Products", icon: null, sortOrder: 0,
            nodeType: MenuNodeTypes.Caption,
            uniqueCodeSuffix: "products");

        #region Video Platform

        var videoPlatformMenu = await GetOrCreateAppMenuAsync(db, logger, channelType: ChannelTypes.BackOffice, dockType: MenuDockTypes.LeftMenu,
            title: "Video Platform", icon: null, sortOrder: 0,
            nodeType: MenuNodeTypes.Node,
            uniqueCodeSuffix: "video-platform");

        _ = await GetOrCreateAppMenuAsync(db, logger, channelType: ChannelTypes.BackOffice, dockType: MenuDockTypes.LeftMenu,
            title: "Overview", icon: null, sortOrder: 0,
            nodeType: MenuNodeTypes.Leaf,
            uniqueCodeSuffix: "video-platform.overview",
            route: "/video-platform/overview",
            parentId: videoPlatformMenu.Id);

        _ = await GetOrCreateAppMenuAsync(db, logger, channelType: ChannelTypes.BackOffice, dockType: MenuDockTypes.LeftMenu,
            title: "Analysis Video", icon: null, sortOrder: 0,
            nodeType: MenuNodeTypes.Leaf,
            uniqueCodeSuffix: "video-platform.analysis-video",
            route: "/video-platform/analysis-video",
            parentId: videoPlatformMenu.Id);

        _ = await GetOrCreateAppMenuAsync(db, logger, channelType: ChannelTypes.BackOffice, dockType: MenuDockTypes.LeftMenu,
            title: "Special Video", icon: null, sortOrder: 0,
            nodeType: MenuNodeTypes.Leaf,
            uniqueCodeSuffix: "video-platform.special-video",
            route: "/video-platform/special-video",
            parentId: videoPlatformMenu.Id);

        _ = await GetOrCreateAppMenuAsync(db, logger, channelType: ChannelTypes.BackOffice, dockType: MenuDockTypes.LeftMenu,
            title: "Create Video", icon: null, sortOrder: 0,
            nodeType: MenuNodeTypes.Leaf,
            uniqueCodeSuffix: "video-platform.create-video",
            route: "/video-platform/create-video",
            parentId: videoPlatformMenu.Id);

        _ = await GetOrCreateAppMenuAsync(db, logger, channelType: ChannelTypes.BackOffice, dockType: MenuDockTypes.LeftMenu,
            title: "Reporting", icon: null, sortOrder: 0,
            nodeType: MenuNodeTypes.Leaf,
            uniqueCodeSuffix: "video-platform.reporting",
            route: "/video-platform/reporting",
            parentId: videoPlatformMenu.Id);

        #endregion

        _ = await GetOrCreateAppMenuAsync(db, logger, channelType: ChannelTypes.BackOffice, dockType: MenuDockTypes.LeftMenu,
            title: "Vertical Video", icon: null, sortOrder: 0,
            nodeType: MenuNodeTypes.Leaf,
            uniqueCodeSuffix: "vertical-video",
            route: "/vertical-video");

        _ = await GetOrCreateAppMenuAsync(db, logger, channelType: ChannelTypes.BackOffice, dockType: MenuDockTypes.LeftMenu,
            title: "Podcast", icon: null, sortOrder: 0,
            nodeType: MenuNodeTypes.Leaf,
            uniqueCodeSuffix: "podcast",
            route: "/podcast");

        _ = await GetOrCreateAppMenuAsync(db, logger, channelType: ChannelTypes.BackOffice, dockType: MenuDockTypes.LeftMenu,
            title: "Ad Wall", icon: null, sortOrder: 0,
            nodeType: MenuNodeTypes.Leaf,
            uniqueCodeSuffix: "ad-wall",
            route: "/ad-wall");

        _ = await GetOrCreateAppMenuAsync(db, logger, channelType: ChannelTypes.BackOffice, dockType: MenuDockTypes.LeftMenu,
            title: "Bidding Tech", icon: null, sortOrder: 0,
            nodeType: MenuNodeTypes.Leaf,
            uniqueCodeSuffix: "bidding-tech",
            route: "/bidding-tech");

        _ = await GetOrCreateAppMenuAsync(db, logger, channelType: ChannelTypes.BackOffice, dockType: MenuDockTypes.LeftMenu,
            title: "Publishers", icon: null, sortOrder: 0,
            nodeType: MenuNodeTypes.Caption,
            uniqueCodeSuffix: "publishers");

        _ = await GetOrCreateAppMenuAsync(db, logger, channelType: ChannelTypes.BackOffice, dockType: MenuDockTypes.LeftMenu,
            title: "Contents", icon: null, sortOrder: 0,
            nodeType: MenuNodeTypes.Leaf,
            uniqueCodeSuffix: "contents",
            route: "/contents",
            requiredEndpointPermissionUniqueCode: ContentServicePermissions.Contents.PagedList
        );

        #region Inventory

        var inventoryMenu = await GetOrCreateAppMenuAsync(db, logger, channelType: ChannelTypes.BackOffice, dockType: MenuDockTypes.LeftMenu,
            title: "Inventory", icon: null, sortOrder: 0,
            nodeType: MenuNodeTypes.Node,
            uniqueCodeSuffix: "inventory");

        _ = await GetOrCreateAppMenuAsync(db, logger, channelType: ChannelTypes.BackOffice, dockType: MenuDockTypes.LeftMenu,
            title: "Codes", icon: null, sortOrder: 0,
            nodeType: MenuNodeTypes.Leaf,
            uniqueCodeSuffix: "inventory.codes",
            route: "/inventory/codes",
            parentId: inventoryMenu.Id);

        #endregion

        #region Payments

        var paymentsMenu = await GetOrCreateAppMenuAsync(db, logger, channelType: ChannelTypes.BackOffice, dockType: MenuDockTypes.LeftMenu,
            title: "Payments", icon: null, sortOrder: 0,
            nodeType: MenuNodeTypes.Node,
            uniqueCodeSuffix: "payments");

        _ = await GetOrCreateAppMenuAsync(db, logger, channelType: ChannelTypes.BackOffice, dockType: MenuDockTypes.LeftMenu,
            title: "Payment List", icon: null, sortOrder: 0,
            nodeType: MenuNodeTypes.Leaf,
            uniqueCodeSuffix: "payments.payment-list",
            route: "/payments/payment-list",
            parentId: paymentsMenu.Id);

        _ = await GetOrCreateAppMenuAsync(db, logger, channelType: ChannelTypes.BackOffice, dockType: MenuDockTypes.LeftMenu,
            title: "Payment Details", icon: null, sortOrder: 0,
            nodeType: MenuNodeTypes.Leaf,
            uniqueCodeSuffix: "payments.payment-details",
            route: "/payments/payment-details",
            parentId: paymentsMenu.Id);

        #endregion

        #region Invoices

        var invoicesMenu = await GetOrCreateAppMenuAsync(db, logger, channelType: ChannelTypes.BackOffice, dockType: MenuDockTypes.LeftMenu,
            title: "Invoices", icon: null, sortOrder: 0,
            nodeType: MenuNodeTypes.Node,
            uniqueCodeSuffix: "invoices");

        _ = await GetOrCreateAppMenuAsync(db, logger, channelType: ChannelTypes.BackOffice, dockType: MenuDockTypes.LeftMenu,
            title: "Invoice List", icon: null, sortOrder: 0,
            nodeType: MenuNodeTypes.Leaf,
            uniqueCodeSuffix: "invoices.invoice-list",
            route: "/invoices/invoice-list",
            parentId: invoicesMenu.Id);

        _ = await GetOrCreateAppMenuAsync(db, logger, channelType: ChannelTypes.BackOffice, dockType: MenuDockTypes.LeftMenu,
            title: "Invoice Details", icon: null, sortOrder: 0,
            nodeType: MenuNodeTypes.Leaf,
            uniqueCodeSuffix: "invoices.invoice-details",
            route: "/invoices/invoice-details",
            parentId: invoicesMenu.Id);

        #endregion

        _ = await GetOrCreateAppMenuAsync(db, logger, channelType: ChannelTypes.BackOffice, dockType: MenuDockTypes.LeftMenu,
            title: "Administration", icon: null, sortOrder: 0,
            nodeType: MenuNodeTypes.Caption,
            uniqueCodeSuffix: "administration");

        #region Publisher Management

        var publisherManagementMenu = await GetOrCreateAppMenuAsync(db, logger, channelType: ChannelTypes.BackOffice, dockType: MenuDockTypes.LeftMenu,
            title: "Publisher Management", icon: null, sortOrder: 0,
            nodeType: MenuNodeTypes.Node,
            uniqueCodeSuffix: "publisher-management");

        _ = await GetOrCreateAppMenuAsync(db, logger, channelType: ChannelTypes.BackOffice, dockType: MenuDockTypes.LeftMenu,
            title: "Publisher List", icon: null, sortOrder: 0,
            nodeType: MenuNodeTypes.Leaf,
            uniqueCodeSuffix: "publisher-management.publisher-list",
            route: "/publisher-management/publisher-list",
            parentId: publisherManagementMenu.Id);

        _ = await GetOrCreateAppMenuAsync(db, logger, channelType: ChannelTypes.BackOffice, dockType: MenuDockTypes.LeftMenu,
            title: "Publisher Settings", icon: null, sortOrder: 0,
            nodeType: MenuNodeTypes.Leaf,
            uniqueCodeSuffix: "publisher-management.publisher-settings",
            route: "/publisher-management/publisher-settings",
            parentId: publisherManagementMenu.Id);

        _ = await GetOrCreateAppMenuAsync(db, logger, channelType: ChannelTypes.BackOffice, dockType: MenuDockTypes.LeftMenu,
            title: "Add Publisher", icon: null, sortOrder: 0,
            nodeType: MenuNodeTypes.Leaf,
            uniqueCodeSuffix: "publisher-management.add-publisher",
            route: "/publisher-management/add-publisher",
            parentId: publisherManagementMenu.Id);

        _ = await GetOrCreateAppMenuAsync(db, logger, channelType: ChannelTypes.BackOffice, dockType: MenuDockTypes.LeftMenu,
            title: "Add User", icon: null, sortOrder: 0,
            nodeType: MenuNodeTypes.Leaf,
            uniqueCodeSuffix: "publisher-management.add-user",
            route: "/publisher-management/add-user",
            parentId: publisherManagementMenu.Id);

        #endregion

        #region Integration Management

        var integrationManagementMenu = await GetOrCreateAppMenuAsync(db, logger, channelType: ChannelTypes.BackOffice, dockType: MenuDockTypes.LeftMenu,
            title: "Integration Management", icon: null, sortOrder: 0,
            nodeType: MenuNodeTypes.Node,
            uniqueCodeSuffix: "integration-management");

        _ = await GetOrCreateAppMenuAsync(db, logger, channelType: ChannelTypes.BackOffice, dockType: MenuDockTypes.LeftMenu,
            title: "Domain & Codes", icon: null, sortOrder: 0,
            nodeType: MenuNodeTypes.Leaf,
            uniqueCodeSuffix: "integration-management.domain-codes",
            route: "/integration-management/domain-codes",
            parentId: integrationManagementMenu.Id);

        #endregion

        #region Tenant Management

        var tenantManagementMenu = await GetOrCreateAppMenuAsync(db, logger, channelType: ChannelTypes.BackOffice, dockType: MenuDockTypes.LeftMenu,
            title: "Tenant Management", icon: null, sortOrder: 0,
            nodeType: MenuNodeTypes.Node,
            uniqueCodeSuffix: "tenant-management");

        _ = await GetOrCreateAppMenuAsync(db, logger, channelType: ChannelTypes.BackOffice, dockType: MenuDockTypes.LeftMenu,
            title: "Tenants", icon: null, sortOrder: 0,
            nodeType: MenuNodeTypes.Leaf,
            uniqueCodeSuffix: "tenant-management.tenants",
            route: "/tenant-management/tenants",
            parentId: tenantManagementMenu.Id,
            requiredEndpointPermissionUniqueCode: IdentityServicePermissions.Tenants.PagedList,
            actionEndpointPermissionUniqueCodes:
            [
                IdentityServicePermissions.Tenants.Create,
                IdentityServicePermissions.Tenants.Update,
                IdentityServicePermissions.Tenants.Delete,
            ]
        );

        _ = await GetOrCreateAppMenuAsync(db, logger, channelType: ChannelTypes.BackOffice, dockType: MenuDockTypes.LeftMenu,
            title: "Clients", icon: null, sortOrder: 0,
            nodeType: MenuNodeTypes.Leaf,
            uniqueCodeSuffix: "tenant-management.clients",
            route: "/tenant-management/clients",
            parentId: tenantManagementMenu.Id,
            requiredEndpointPermissionUniqueCode: ContentServicePermissions.Clients.PagedList,
            actionEndpointPermissionUniqueCodes:
            [
                ContentServicePermissions.Clients.Create,
                ContentServicePermissions.Clients.Update,
                ContentServicePermissions.Clients.Delete,
            ]
        );

        #endregion

        #region Identity Management

        var identityManagementMenu = await GetOrCreateAppMenuAsync(db, logger, channelType: ChannelTypes.BackOffice, dockType: MenuDockTypes.LeftMenu,
            title: "Identity Management", icon: null, sortOrder: 0,
            nodeType: MenuNodeTypes.Node,
            uniqueCodeSuffix: "identity-management");

        _ = await GetOrCreateAppMenuAsync(db, logger, channelType: ChannelTypes.BackOffice, dockType: MenuDockTypes.LeftMenu,
            title: "Roles", icon: null, sortOrder: 0,
            nodeType: MenuNodeTypes.Leaf,
            uniqueCodeSuffix: "identity-management.roles",
            route: "/identity-management/roles",
            parentId: identityManagementMenu.Id,
            requiredEndpointPermissionUniqueCode: IdentityServicePermissions.AppRoles.PagedList,
            actionEndpointPermissionUniqueCodes:
            [
                IdentityServicePermissions.AppRoles.Create,
                IdentityServicePermissions.AppRoles.Update,
                IdentityServicePermissions.AppRoles.Delete,
            ]
        );

        _ = await GetOrCreateAppMenuAsync(db, logger, channelType: ChannelTypes.BackOffice, dockType: MenuDockTypes.LeftMenu,
            title: "Role Permissions", icon: null, sortOrder: 0,
            nodeType: MenuNodeTypes.Leaf,
            uniqueCodeSuffix: "identity-management.role-permissions",
            route: "/identity-management/role-permissions",
            parentId: identityManagementMenu.Id,
            requiredEndpointPermissionUniqueCode: AdministrationServicePermissions.RolePermissions.Read,
            actionEndpointPermissionUniqueCodes:
            [
                AdministrationServicePermissions.RolePermissions.Update,
            ]
        );

        _ = await GetOrCreateAppMenuAsync(db, logger, channelType: ChannelTypes.BackOffice, dockType: MenuDockTypes.LeftMenu,
            title: "Users", icon: null, sortOrder: 0,
            nodeType: MenuNodeTypes.Leaf,
            uniqueCodeSuffix: "identity-management.users",
            route: "/identity-management/users",
            parentId: identityManagementMenu.Id,
            requiredEndpointPermissionUniqueCode: IdentityServicePermissions.AppUsers.PagedList,
            actionEndpointPermissionUniqueCodes:
            [
                IdentityServicePermissions.AppUsers.Create,
                IdentityServicePermissions.AppUsers.Update,
                IdentityServicePermissions.AppUsers.Delete,
            ],
            operationPermissionUniqueCodes:
            [
                IdentityOperationPermissions.AppUsers.EmailUpdate,
                IdentityOperationPermissions.AppUsers.PhoneUpdate,
                IdentityOperationPermissions.AppUsers.PhoneView,
                IdentityOperationPermissions.AppUsers.IsBlockUpdate
            ]
        );


        _ = await GetOrCreateAppMenuAsync(db, logger, channelType: ChannelTypes.BackOffice, dockType: MenuDockTypes.LeftMenu,
            title: "Audit Logs", icon: null, sortOrder: 0,
            nodeType: MenuNodeTypes.Leaf,
            uniqueCodeSuffix: "identity-management.audit-logs",
            route: "/identity-management/audit-logs",
            parentId: identityManagementMenu.Id,
            requiredEndpointPermissionUniqueCode: IdentityServicePermissions.AuditLogs.PagedList,
            constraintPermissionUniqueCodes:
            [
                IdentityConstraintPermissions.AuditLogs.MaxDays
            ]);

        #endregion

        #endregion

        #endregion

        // update changes
        await db.SaveChangesAsync();
    }

    private static async Task<AppMenu> GetOrCreateAppMenuAsync(AdministrationServiceDbContext db, IAppConsoleLogger logger,
        [NotNull] string uniqueCodeSuffix,
        ChannelTypes channelType,
        MenuDockTypes dockType,
        MenuNodeTypes nodeType,
        [CanBeNull] string title = null,
        [CanBeNull] string route = null,
        [CanBeNull] string icon = null,
        int sortOrder = 0,
        Guid? parentId = null,
        [CanBeNull] string requiredEndpointPermissionUniqueCode = null,
        List<string> actionEndpointPermissionUniqueCodes = null,
        List<string> operationPermissionUniqueCodes = null,
        List<string> constraintPermissionUniqueCodes = null)
    {
        #region Type Control

        switch (channelType)
        {
            case ChannelTypes.MobileApp:
            case ChannelTypes.BackOffice:
                {
                    break;
                }
            case ChannelTypes.Unknown:
            default:
                {
                    throw new Exception("Unknown menu channel type: " + channelType.ToDisplayName());
                }
        }

        switch (dockType)
        {
            case MenuDockTypes.TopMenu:
            case MenuDockTypes.LeftMenu:
            case MenuDockTypes.RightMenu:
            case MenuDockTypes.BottomMenu:
                {
                    break;
                }
            case MenuDockTypes.Unknown:
            default:
                {
                    throw new Exception("Unknown menu dock type: " + dockType.ToDisplayName());
                }
        }

        switch (nodeType)
        {
            case MenuNodeTypes.Caption:
            case MenuNodeTypes.Node:
            case MenuNodeTypes.Leaf:
                {
                    break;
                }
            case MenuNodeTypes.Unknown:
            default:
                {
                    throw new Exception("Unknown menu node type: " + nodeType.ToDisplayName());
                }
        }

        #endregion

        string normalizedUniqueCodeSuffix = StringHelper.Minimize(StringHelper.ReplaceInvalidChars(uniqueCodeSuffix));

        string normalizedUniqueCode = $"{channelType.ToPrompt()}.{nodeType.ToPrompt()}.{normalizedUniqueCodeSuffix}";

        var appMenuItem = await db.AppMenus.FirstOrDefaultAsync(x => x.UniqueCode == normalizedUniqueCode);

        if (appMenuItem is null)
        {
            if (nodeType == MenuNodeTypes.Leaf)
            {
                #region Path Control

                if (string.IsNullOrWhiteSpace(route))
                {
                    throw new Exception("Empty or null menu page path");
                }

                if (!route.StartsWith('/'))
                {
                    throw new Exception("Invalid menu page path: " + route);
                }

                route = StringHelper.Minimize(route);

                #endregion
            }
            else
            {
                route = null;
            }

            appMenuItem = new AppMenu(
                parentId: parentId,
                uniqueCode: normalizedUniqueCode,
                channelType: channelType,
                dockType: dockType,
                nodeType: nodeType,
                title: title,
                route: route,
                icon: icon,
                sortOrder: sortOrder
            );

            db.AppMenus.Add(appMenuItem);
            await db.SaveChangesAsync();

            logger.LogDebug("{WorkerName} | SEED APP_MENU -> {UniqueCode} added", nameof(EfCoreSeederService), normalizedUniqueCode);
        }

        var visibilityPermission = await GetOrCreateVisibilityPermissionsAsync(db, logger, leafMenuUniqueCode: appMenuItem.UniqueCode);

        bool hasVisibilityPermission = await db.AppMenuPermissions.AnyAsync(x =>
            x.AppMenuId == appMenuItem.Id &&
            x.PermissionId == visibilityPermission.Id &&
            x.RelationType == MenuPermissionRelationTypes.Visibility);

        if (!hasVisibilityPermission)
        {
            db.AppMenuPermissions.Add(new AppMenuPermission(
                appMenuId: appMenuItem.Id,
                permissionId: visibilityPermission.Id,
                relationType: MenuPermissionRelationTypes.Visibility,
                sortOrder: 0));

            await db.SaveChangesAsync();
        }

        if (!string.IsNullOrWhiteSpace(requiredEndpointPermissionUniqueCode))
        {
            await CreateIfNotExistAppMenuPermissionAsync(db, logger,
                appMenuItemId: appMenuItem.Id,
                permissionUniqueCode: requiredEndpointPermissionUniqueCode,
                permissionType: PermissionTypes.Service,
                relationType: MenuPermissionRelationTypes.RequiredService
            );
        }

        if (actionEndpointPermissionUniqueCodes is { Count: > 0 })
        {
            foreach (string actionPermissionUniqueCode in actionEndpointPermissionUniqueCodes)
            {
                await CreateIfNotExistAppMenuPermissionAsync(db, logger,
                    appMenuItemId: appMenuItem.Id,
                    permissionUniqueCode: actionPermissionUniqueCode,
                    permissionType: PermissionTypes.Service,
                    relationType: MenuPermissionRelationTypes.ActionService
                );
            }
        }

        if (operationPermissionUniqueCodes is { Count: > 0 })
        {
            foreach (string operationPermissionUniqueCode in operationPermissionUniqueCodes)
            {
                await CreateIfNotExistAppMenuPermissionAsync(db, logger,
                    appMenuItemId: appMenuItem.Id,
                    permissionUniqueCode: operationPermissionUniqueCode,
                    permissionType: PermissionTypes.OperationAccess,
                    relationType: MenuPermissionRelationTypes.OperationAccess
                );
            }
        }

        if (constraintPermissionUniqueCodes is { Count: > 0 })
        {
            foreach (string constraintPermissionUniqueCode in constraintPermissionUniqueCodes)
            {
                await CreateIfNotExistAppMenuPermissionAsync(db, logger,
                    appMenuItemId: appMenuItem.Id,
                    permissionUniqueCode: constraintPermissionUniqueCode,
                    permissionType: PermissionTypes.Constraint,
                    relationType: MenuPermissionRelationTypes.Constraint
                );
            }
        }

        return appMenuItem;
    }

    private static async Task<Permission> GetOrCreateVisibilityPermissionsAsync(AdministrationServiceDbContext db, IAppConsoleLogger logger,
        [NotNull] string leafMenuUniqueCode,
        [CanBeNull] string name = null,
        [CanBeNull] string description = null)
    {
        string normalizedLeafMenuUniqueCode = StringHelper.Minimize(StringHelper.ReplaceInvalidChars(leafMenuUniqueCode));

        string normalizedUniqueCode = $"{normalizedLeafMenuUniqueCode}.view";

        var permissionItem = await db.Permissions.FirstOrDefaultAsync(x => x.UniqueCode == normalizedUniqueCode);

        if (permissionItem is not null)
            return permissionItem;

        permissionItem = new Permission(
            uniqueCode: normalizedUniqueCode,
            permissionType: PermissionTypes.Page,
            name: name,
            description: description
        );

        db.Permissions.Add(permissionItem);
        await db.SaveChangesAsync();

        logger.LogDebug("{WorkerName} | SEED VISIBILITY PERMISSION -> {UniqueCode} added", nameof(EfCoreSeederService), normalizedUniqueCode);

        return permissionItem;
    }

    private static async Task CreateIfNotExistAppMenuPermissionAsync(AdministrationServiceDbContext db, IAppConsoleLogger logger,
        Guid appMenuItemId,
        [NotNull] string permissionUniqueCode,
        PermissionTypes permissionType,
        MenuPermissionRelationTypes relationType)
    {
        string normalizedPermissionUniqueCode = StringHelper.Minimize(StringHelper.ReplaceInvalidChars(permissionUniqueCode));

        // Permission created on PermissionSeeder -> get permission
        var permissionItem = await db.Permissions.FirstOrDefaultAsync(x => x.UniqueCode == normalizedPermissionUniqueCode);

        if (permissionItem is null)
            throw new Exception("Not found menu permission: " + normalizedPermissionUniqueCode);

        if (permissionItem.PermissionType != permissionType)
            throw new Exception("Invalid menu permission type: " + permissionType.ToDisplayName());

        bool hasPermission = await db.AppMenuPermissions.AnyAsync(x =>
            x.AppMenuId == appMenuItemId &&
            x.PermissionId == permissionItem.Id &&
            x.RelationType == relationType);

        if (!hasPermission)
        {
            db.AppMenuPermissions.Add(new AppMenuPermission(
                appMenuId: appMenuItemId,
                permissionId: permissionItem.Id,
                relationType: relationType,
                sortOrder: 0));

            await db.SaveChangesAsync();
        }
    }
}