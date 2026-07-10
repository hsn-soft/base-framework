using Hhs.AdministrationService.Domain.old;
using Hhs.AdministrationService.Domain.PermissionDomain.Entities;
using Hhs.Shared.Helper.Constants;
using Hhs.Shared.Helper.Permissions;

namespace Hhs.AdministrationService.Application.Contracts.PermissionDomain.Data;

public static class StaticData
{
    public static IEnumerable<string> DefaultUserPermissions()
    {
        var defaultUserPermissions = new List<string>
        {
            $"{WebChannelMenu.Dashboards.Summary}{WebChannelMenu.LeafPermissionName}",
            $"{WebChannelMenu.ContentManagement.Contents}{WebChannelMenu.LeafPermissionName}",
            $"{WebChannelMenu.ContentManagement.Visits}{WebChannelMenu.LeafPermissionName}",
            $"{WebChannelMenu.Contents}{WebChannelMenu.LeafPermissionName}"
        };

        // defaultUserPermissions.Add(ContentServicePermissions.Dashboards.ResponseStatisticView);

        // defaultUserPermissions.AddRange(ContentServicePermissions.GetAll());
        // defaultUserPermissions.AddRange(TextNormalizerServicePermissions.GetAll());
        // defaultUserPermissions.AddRange(VideoGeneratorServicePermissions.GetAll());
        // defaultUserPermissions.AddRange(EventManagerServicePermissions.GetAll());

        return defaultUserPermissions;
    }


    public static IEnumerable<string> AllPermissions()
    {
        var allServicePermissions = MenuPermissionMapDatas
            .Where(x => x.PermissionType == PermissionTypesold.LeafMenuPermission)
            .Select(x => x.PermissionUniqueName)
            .Distinct()
            .ToList();

        allServicePermissions.AddRange(AdministrationServicePermissions.GetAll());
        allServicePermissions.AddRange(IdentityServicePermissions.GetAll());
        allServicePermissions.AddRange(ContentServicePermissions.GetAll());
        allServicePermissions.AddRange(TextNormalizerServicePermissions.GetAll());
        allServicePermissions.AddRange(VideoGeneratorServicePermissions.GetAll());
        allServicePermissions.AddRange(EventManagerServicePermissions.GetAll());

        allServicePermissions.AddRange(AdministrationOperationPermissions.GetAll());
        allServicePermissions.AddRange(IdentityOperationPermissions.GetAll());
        allServicePermissions.AddRange(ContentOperationPermissions.GetAll());
        allServicePermissions.AddRange(TextNormalizerOperationPermissions.GetAll());
        allServicePermissions.AddRange(VideoGeneratorOperationPermissions.GetAll());
        allServicePermissions.AddRange(EventManagerOperationPermissions.GetAll());

        return allServicePermissions;
    }

    public static IEnumerable<OldMenuMap> MenuMapDatas => new OldMenuMap[]
    {
        new(AppUiNames.BackOfficeClientId, "dashboard", null, WebChannelMenu.Dashboards.NodeName, MenuMapType.Node, null, "ti ti-brand-google-home", 1, ""),
        new(AppUiNames.BackOfficeClientId, "dashboard", WebChannelMenu.Dashboards.NodeName, WebChannelMenu.Dashboards.Summary, MenuMapType.Leaf, "/dashboards/summary", "", 1, ""),
        new(AppUiNames.BackOfficeClientId, "dashboard", WebChannelMenu.Dashboards.NodeName, WebChannelMenu.Dashboards.Traffic, MenuMapType.Leaf, "/dashboards/traffic", "", 2, ""),
        new(AppUiNames.BackOfficeClientId, "dashboard", null, WebChannelMenu.Operation, MenuMapType.Caption, "", "", 2, ""),
        new(AppUiNames.BackOfficeClientId, "dashboard", null, WebChannelMenu.ContentManagement.NodeName, MenuMapType.Node, null, "ti ti-file-stack", orderNo: 3, ""),
        new(AppUiNames.BackOfficeClientId, "dashboard", WebChannelMenu.ContentManagement.NodeName, WebChannelMenu.ContentManagement.Contents, MenuMapType.Leaf, "/content-management/contents", "", orderNo: 1, ""),
        new(AppUiNames.BackOfficeClientId, "dashboard", WebChannelMenu.ContentManagement.NodeName, WebChannelMenu.ContentManagement.Visits, MenuMapType.Leaf, "/content-management/visits", "", orderNo: 2, ""),
        new(AppUiNames.BackOfficeClientId, "dashboard", WebChannelMenu.ContentManagement.NodeName, WebChannelMenu.ContentManagement.VideoPlatform, MenuMapType.Node, null, "ti ti-video", orderNo: 3, ""),
        new(AppUiNames.BackOfficeClientId, "dashboard", WebChannelMenu.ContentManagement.VideoPlatform, WebChannelMenu.ContentManagement.VideoPlatformOverview, MenuMapType.Leaf, "/products/video-platform/overview", "", orderNo: 1, ""),
        new(AppUiNames.BackOfficeClientId, "dashboard", WebChannelMenu.ContentManagement.VideoPlatform, WebChannelMenu.ContentManagement.VideoPlatformAnalysisVideo, MenuMapType.Leaf, "/products/video-platform/analysis-video", "", orderNo: 2, ""),
        new(AppUiNames.BackOfficeClientId, "dashboard", WebChannelMenu.ContentManagement.VideoPlatform, WebChannelMenu.ContentManagement.VideoPlatformSpecialVideo, MenuMapType.Leaf, "/products/video-platform/special-video", "", orderNo: 3, ""),
        new(AppUiNames.BackOfficeClientId, "dashboard", WebChannelMenu.ContentManagement.VideoPlatform, WebChannelMenu.ContentManagement.VideoPlatformCreateAVideo, MenuMapType.Leaf, "/products/video-platform/create-video", "", orderNo: 4, ""),
        new(AppUiNames.BackOfficeClientId, "dashboard", WebChannelMenu.ContentManagement.VideoPlatform, WebChannelMenu.ContentManagement.VideoPlatformReporting, MenuMapType.Leaf, "/products/video-platform/reporting", "", orderNo: 5, ""),
        new(AppUiNames.BackOfficeClientId, "dashboard", WebChannelMenu.ContentManagement.NodeName, WebChannelMenu.ContentManagement.VerticalVideo, MenuMapType.Leaf, "/products/vertical-video", "", orderNo: 4, ""),
        new(AppUiNames.BackOfficeClientId, "dashboard", WebChannelMenu.ContentManagement.NodeName, WebChannelMenu.ContentManagement.Podcast, MenuMapType.Leaf, "/products/podcast", "", orderNo: 5, ""),
        new(AppUiNames.BackOfficeClientId, "dashboard", WebChannelMenu.ContentManagement.NodeName, WebChannelMenu.ContentManagement.AdWall, MenuMapType.Leaf, "/products/ad-wall", "", orderNo: 6, ""),
        new(AppUiNames.BackOfficeClientId, "dashboard", WebChannelMenu.ContentManagement.NodeName, WebChannelMenu.ContentManagement.BiddingTech, MenuMapType.Leaf, "/products/bidding-tech", "", orderNo: 7, ""),
        new(AppUiNames.BackOfficeClientId, "dashboard", null, WebChannelMenu.InvoiceManagement.NodeName, MenuMapType.Node, null, "ti ti-file-invoice", orderNo: 4, ""),
        new(AppUiNames.BackOfficeClientId, "dashboard", WebChannelMenu.InvoiceManagement.NodeName, WebChannelMenu.InvoiceManagement.Invoices, MenuMapType.Leaf, "/invoice-management/invoices", "", orderNo: 1, ""),
        new(AppUiNames.BackOfficeClientId, "dashboard", WebChannelMenu.InvoiceManagement.NodeName, WebChannelMenu.InvoiceManagement.CostAnalysis, MenuMapType.Leaf, "/invoice-management/cost-analysis", "", orderNo: 2, ""),
        new(AppUiNames.BackOfficeClientId, "dashboard", null, WebChannelMenu.Reporting.NodeName, MenuMapType.Node, null, "ti ti-chart-donut", orderNo: 5, ""),
        new(AppUiNames.BackOfficeClientId, "dashboard", WebChannelMenu.Reporting.NodeName, WebChannelMenu.Reporting.TrendContentReport, MenuMapType.Leaf, "/reporting/trend-content-report", "", orderNo: 1, ""),
        new(AppUiNames.BackOfficeClientId, "dashboard", WebChannelMenu.Reporting.NodeName, WebChannelMenu.Reporting.AnalysisContentReport, MenuMapType.Leaf, "/reporting/analysis-content-report", "", orderNo: 2, ""),
        new(AppUiNames.BackOfficeClientId, "dashboard", null, WebChannelMenu.Contents, MenuMapType.Leaf, "/publishlers", "ti ti-file-stack", orderNo: 6, ""),
        new(AppUiNames.BackOfficeClientId, "dashboard", null, WebChannelMenu.Inventory.NodeName, MenuMapType.Node, null, "ti ti-archive", orderNo: 7, ""),
        new(AppUiNames.BackOfficeClientId, "dashboard", WebChannelMenu.Inventory.NodeName, WebChannelMenu.Inventory.Codes, MenuMapType.Leaf, "/publishlers/inventory/codes", "", orderNo: 1, ""),
        new(AppUiNames.BackOfficeClientId, "dashboard", null, WebChannelMenu.Payments.NodeName, MenuMapType.Node, null, "ti ti-credit-card", orderNo: 8, ""),
        new(AppUiNames.BackOfficeClientId, "dashboard", WebChannelMenu.Payments.NodeName, WebChannelMenu.Payments.List, MenuMapType.Leaf, "/publishlers/payments/list", "", orderNo: 1, ""),
        new(AppUiNames.BackOfficeClientId, "dashboard", WebChannelMenu.Payments.NodeName, WebChannelMenu.Payments.Detail, MenuMapType.Leaf, "/publishlers/payments/detail", "", orderNo: 2, ""),
        new(AppUiNames.BackOfficeClientId, "dashboard", null, WebChannelMenu.Invoice.NodeName, MenuMapType.Node, null, "ti ti-file-invoice", orderNo: 9, ""),
        new(AppUiNames.BackOfficeClientId, "dashboard", WebChannelMenu.Invoice.NodeName, WebChannelMenu.Invoice.List, MenuMapType.Leaf, "/publishlers/invoice/list", "", orderNo: 1, ""),
        new(AppUiNames.BackOfficeClientId, "dashboard", WebChannelMenu.Invoice.NodeName, WebChannelMenu.Invoice.Detail, MenuMapType.Leaf, "/publishlers/invoice/detail", "", orderNo: 2, ""),
        new(AppUiNames.BackOfficeClientId, "dashboard", null, WebChannelMenu.Administration, MenuMapType.Caption, "", "", 10, ""),
        new(AppUiNames.BackOfficeClientId, "dashboard", null, WebChannelMenu.PublisherManagement.NodeName, MenuMapType.Node, null, "ti ti-building", orderNo: 11, ""),
        new(AppUiNames.BackOfficeClientId, "dashboard", WebChannelMenu.PublisherManagement.NodeName, WebChannelMenu.PublisherManagement.List, MenuMapType.Leaf, "/publisher-management/list", "", orderNo: 1, ""),
        new(AppUiNames.BackOfficeClientId, "dashboard", WebChannelMenu.PublisherManagement.NodeName, WebChannelMenu.PublisherManagement.SettingsMenu, MenuMapType.Leaf, "/publisher-management/settings", "", orderNo: 2, ""),
        new(AppUiNames.BackOfficeClientId, "dashboard", WebChannelMenu.PublisherManagement.NodeName, WebChannelMenu.PublisherManagement.AddPublisher, MenuMapType.Leaf, "/publisher-management/add-publisher", "", orderNo: 3, ""),
        new(AppUiNames.BackOfficeClientId, "dashboard", WebChannelMenu.PublisherManagement.NodeName, WebChannelMenu.PublisherManagement.AddUser, MenuMapType.Leaf, "/publisher-management/add-user", "", orderNo: 4, ""),
        new(AppUiNames.BackOfficeClientId, "dashboard", null, WebChannelMenu.IntegrationManagement.NodeName, MenuMapType.Node, null, "ti ti-plug", orderNo: 12, ""),
        new(AppUiNames.BackOfficeClientId, "dashboard", WebChannelMenu.IntegrationManagement.NodeName, WebChannelMenu.IntegrationManagement.DomainCodes, MenuMapType.Leaf, "/integration-management/domain-codes", "", orderNo: 1, ""),
        new(AppUiNames.BackOfficeClientId, "dashboard", null, WebChannelMenu.TenantManagement.NodeName, MenuMapType.Node, null, "ti ti-address-book", 13, "", true),
        new(AppUiNames.BackOfficeClientId, "dashboard", WebChannelMenu.TenantManagement.NodeName, WebChannelMenu.TenantManagement.Tenants, MenuMapType.Leaf, "/tenant-management/tenants", "", 1, "", true),
        new(AppUiNames.BackOfficeClientId, "dashboard", WebChannelMenu.TenantManagement.NodeName, WebChannelMenu.TenantManagement.Clients, MenuMapType.Leaf, "/tenant-management/clients", "", 2, "", true),
        new(AppUiNames.BackOfficeClientId, "dashboard", null, WebChannelMenu.IdentityManagement.NodeName, MenuMapType.Node, null, "ti ti-user-circle", orderNo: 14, ""),
        new(AppUiNames.BackOfficeClientId, "dashboard", WebChannelMenu.IdentityManagement.NodeName, WebChannelMenu.IdentityManagement.Roles, MenuMapType.Leaf, "/identity-management/roles", "", orderNo: 1, ""),
        new(AppUiNames.BackOfficeClientId, "dashboard", WebChannelMenu.IdentityManagement.NodeName, WebChannelMenu.IdentityManagement.Users, MenuMapType.Leaf, "/identity-management/users", "", orderNo: 2, ""),
        new(AppUiNames.BackOfficeClientId, "dashboard", WebChannelMenu.IdentityManagement.NodeName, WebChannelMenu.IdentityManagement.AuditLogs, MenuMapType.Leaf, "/identity-management/audit-logs", "", orderNo: 3, ""),
        new(AppUiNames.BackOfficeClientId, "dashboard", null, WebChannelMenu.Account.Settings, MenuMapType.Leaf, "/account/settings", "ti ti-atom", orderNo: 15, ""),
    };

    public static IEnumerable<OldMenuPermissionMap> MenuPermissionMapDatas => new OldMenuPermissionMap[]
    {
        new(AppUiNames.BackOfficeClientId, WebChannelMenu.Dashboards.Summary, PermissionTypesold.LeafMenuPermission, $"{WebChannelMenu.Dashboards.Summary}{WebChannelMenu.LeafPermissionName}", 10),
        // new(AppUiNames.BackOfficeClientId, WebChannelMenu.Dashboards.Summary, PermissionTypes.ActionPermission, ContentServicePermissions.Dashboards.ResponseStatisticView, 11),

        new(AppUiNames.BackOfficeClientId, WebChannelMenu.Dashboards.Traffic, PermissionTypesold.LeafMenuPermission, $"{WebChannelMenu.Dashboards.Traffic}{WebChannelMenu.LeafPermissionName}", 10),

        new(AppUiNames.BackOfficeClientId, WebChannelMenu.ContentManagement.Contents, PermissionTypesold.LeafMenuPermission, $"{WebChannelMenu.ContentManagement.Contents}{WebChannelMenu.LeafPermissionName}", 10),

        new(AppUiNames.BackOfficeClientId, WebChannelMenu.ContentManagement.Visits, PermissionTypesold.LeafMenuPermission, $"{WebChannelMenu.ContentManagement.Visits}{WebChannelMenu.LeafPermissionName}", 10),

        new(AppUiNames.BackOfficeClientId, WebChannelMenu.ContentManagement.VideoPlatformOverview, PermissionTypesold.LeafMenuPermission, $"{WebChannelMenu.ContentManagement.VideoPlatformOverview}{WebChannelMenu.LeafPermissionName}", 10),
        new(AppUiNames.BackOfficeClientId, WebChannelMenu.ContentManagement.VideoPlatformAnalysisVideo, PermissionTypesold.LeafMenuPermission, $"{WebChannelMenu.ContentManagement.VideoPlatformAnalysisVideo}{WebChannelMenu.LeafPermissionName}", 10),
        new(AppUiNames.BackOfficeClientId, WebChannelMenu.ContentManagement.VideoPlatformSpecialVideo, PermissionTypesold.LeafMenuPermission, $"{WebChannelMenu.ContentManagement.VideoPlatformSpecialVideo}{WebChannelMenu.LeafPermissionName}", 10),
        new(AppUiNames.BackOfficeClientId, WebChannelMenu.ContentManagement.VideoPlatformCreateAVideo, PermissionTypesold.LeafMenuPermission, $"{WebChannelMenu.ContentManagement.VideoPlatformCreateAVideo}{WebChannelMenu.LeafPermissionName}", 10),
        new(AppUiNames.BackOfficeClientId, WebChannelMenu.ContentManagement.VideoPlatformReporting, PermissionTypesold.LeafMenuPermission, $"{WebChannelMenu.ContentManagement.VideoPlatformReporting}{WebChannelMenu.LeafPermissionName}", 10),
        new(AppUiNames.BackOfficeClientId, WebChannelMenu.ContentManagement.VerticalVideo, PermissionTypesold.LeafMenuPermission, $"{WebChannelMenu.ContentManagement.VerticalVideo}{WebChannelMenu.LeafPermissionName}", 10),
        new(AppUiNames.BackOfficeClientId, WebChannelMenu.ContentManagement.Podcast, PermissionTypesold.LeafMenuPermission, $"{WebChannelMenu.ContentManagement.Podcast}{WebChannelMenu.LeafPermissionName}", 10),
        new(AppUiNames.BackOfficeClientId, WebChannelMenu.ContentManagement.AdWall, PermissionTypesold.LeafMenuPermission, $"{WebChannelMenu.ContentManagement.AdWall}{WebChannelMenu.LeafPermissionName}", 10),
        new(AppUiNames.BackOfficeClientId, WebChannelMenu.ContentManagement.BiddingTech, PermissionTypesold.LeafMenuPermission, $"{WebChannelMenu.ContentManagement.BiddingTech}{WebChannelMenu.LeafPermissionName}", 10),

        new(AppUiNames.BackOfficeClientId, WebChannelMenu.InvoiceManagement.Invoices, PermissionTypesold.LeafMenuPermission, $"{WebChannelMenu.InvoiceManagement.Invoices}{WebChannelMenu.LeafPermissionName}", 10),

        new(AppUiNames.BackOfficeClientId, WebChannelMenu.InvoiceManagement.CostAnalysis, PermissionTypesold.LeafMenuPermission, $"{WebChannelMenu.InvoiceManagement.CostAnalysis}{WebChannelMenu.LeafPermissionName}", 10),

        new(AppUiNames.BackOfficeClientId, WebChannelMenu.Reporting.TrendContentReport, PermissionTypesold.LeafMenuPermission, $"{WebChannelMenu.Reporting.TrendContentReport}{WebChannelMenu.LeafPermissionName}", 10),

        new(AppUiNames.BackOfficeClientId, WebChannelMenu.Reporting.AnalysisContentReport, PermissionTypesold.LeafMenuPermission, $"{WebChannelMenu.Reporting.AnalysisContentReport}{WebChannelMenu.LeafPermissionName}", 10),

        new(AppUiNames.BackOfficeClientId, WebChannelMenu.Contents, PermissionTypesold.LeafMenuPermission, $"{WebChannelMenu.Contents}{WebChannelMenu.LeafPermissionName}", 10),

        new(AppUiNames.BackOfficeClientId, WebChannelMenu.Inventory.Codes, PermissionTypesold.LeafMenuPermission, $"{WebChannelMenu.Inventory.Codes}{WebChannelMenu.LeafPermissionName}", 10),

        new(AppUiNames.BackOfficeClientId, WebChannelMenu.Payments.List, PermissionTypesold.LeafMenuPermission, $"{WebChannelMenu.Payments.List}{WebChannelMenu.LeafPermissionName}", 10),
        new(AppUiNames.BackOfficeClientId, WebChannelMenu.Payments.Detail, PermissionTypesold.LeafMenuPermission, $"{WebChannelMenu.Payments.Detail}{WebChannelMenu.LeafPermissionName}", 10),

        new(AppUiNames.BackOfficeClientId, WebChannelMenu.Invoice.List, PermissionTypesold.LeafMenuPermission, $"{WebChannelMenu.Invoice.List}{WebChannelMenu.LeafPermissionName}", 10),
        new(AppUiNames.BackOfficeClientId, WebChannelMenu.Invoice.Detail, PermissionTypesold.LeafMenuPermission, $"{WebChannelMenu.Invoice.Detail}{WebChannelMenu.LeafPermissionName}", 10),

        // new(AppUiNames.BackOfficeClientId, WebChannelMenu.TenantManagement.Tenants, PermissionTypes.LeafMenuPermission, $"{WebChannelMenu.TenantManagement.Tenants}{WebChannelMenu.LeafPermissionName}", 10),
        // new(AppUiNames.BackOfficeClientId, WebChannelMenu.TenantManagement.Tenants, PermissionTypes.ActionPermission, TenantServicePermissions.Tenants.PageView, 11),
        // new(AppUiNames.BackOfficeClientId, WebChannelMenu.TenantManagement.Tenants, PermissionTypes.ActionPermission, TenantServicePermissions.Tenants.Detail, 12),
        // new(AppUiNames.BackOfficeClientId, WebChannelMenu.TenantManagement.Tenants, PermissionTypes.OperationPermission, TenantOperationPermissions.Tenants.DetailEmail, 13),
        // new(AppUiNames.BackOfficeClientId, WebChannelMenu.TenantManagement.Tenants, PermissionTypes.OperationPermission, TenantOperationPermissions.Tenants.DetailPhone, 14),
        // new(AppUiNames.BackOfficeClientId, WebChannelMenu.TenantManagement.Tenants, PermissionTypes.ActionPermission, TenantServicePermissions.Tenants.Create, 15),
        // new(AppUiNames.BackOfficeClientId, WebChannelMenu.TenantManagement.Tenants, PermissionTypes.ActionPermission, TenantServicePermissions.Tenants.Update, 16),
        // new(AppUiNames.BackOfficeClientId, WebChannelMenu.TenantManagement.Tenants, PermissionTypes.OperationPermission, TenantOperationPermissions.Tenants.UpdateEmail, 17),
        // new(AppUiNames.BackOfficeClientId, WebChannelMenu.TenantManagement.Tenants, PermissionTypes.OperationPermission, TenantOperationPermissions.Tenants.UpdatePhone, 18),
        // new(AppUiNames.BackOfficeClientId, WebChannelMenu.TenantManagement.Tenants, PermissionTypes.OperationPermission, TenantOperationPermissions.Tenants.UpdateIdentityCode, 19),
        // new(AppUiNames.BackOfficeClientId, WebChannelMenu.TenantManagement.Tenants, PermissionTypes.ActionPermission, TenantServicePermissions.Tenants.Delete, 20),

        new(AppUiNames.BackOfficeClientId, WebChannelMenu.TenantManagement.Clients, PermissionTypesold.LeafMenuPermission, $"{WebChannelMenu.TenantManagement.Clients}{WebChannelMenu.LeafPermissionName}", 10),
        new(AppUiNames.BackOfficeClientId, WebChannelMenu.TenantManagement.Clients, PermissionTypesold.ActionPermission, ContentServicePermissions.Clients.PagedList, 11),
        new(AppUiNames.BackOfficeClientId, WebChannelMenu.TenantManagement.Clients, PermissionTypesold.ActionPermission, ContentServicePermissions.Clients.Create, 12),
        new(AppUiNames.BackOfficeClientId, WebChannelMenu.TenantManagement.Clients, PermissionTypesold.ActionPermission, ContentServicePermissions.Clients.Update, 13),
        new(AppUiNames.BackOfficeClientId, WebChannelMenu.TenantManagement.Clients, PermissionTypesold.ActionPermission, ContentServicePermissions.Clients.Delete, 14),

        new(AppUiNames.BackOfficeClientId, WebChannelMenu.IdentityManagement.Roles, PermissionTypesold.LeafMenuPermission, $"{WebChannelMenu.IdentityManagement.Roles}{WebChannelMenu.LeafPermissionName}", 10),
        new(AppUiNames.BackOfficeClientId, WebChannelMenu.IdentityManagement.Roles, PermissionTypesold.ActionPermission, IdentityServicePermissions.AppRoles.PagedList, 11),
        new(AppUiNames.BackOfficeClientId, WebChannelMenu.IdentityManagement.Roles, PermissionTypesold.ActionPermission, IdentityServicePermissions.AppRoles.Create, 12),
        new(AppUiNames.BackOfficeClientId, WebChannelMenu.IdentityManagement.Roles, PermissionTypesold.ActionPermission, IdentityServicePermissions.AppRoles.Update, 13),
        new(AppUiNames.BackOfficeClientId, WebChannelMenu.IdentityManagement.Roles, PermissionTypesold.ActionPermission, IdentityServicePermissions.AppRoles.Delete, 14),
        new(AppUiNames.BackOfficeClientId, WebChannelMenu.IdentityManagement.Roles, PermissionTypesold.ActionPermission, AdministrationServicePermissions.RolePermissions.Read, 15),
        new(AppUiNames.BackOfficeClientId, WebChannelMenu.IdentityManagement.Roles, PermissionTypesold.ActionPermission, AdministrationServicePermissions.RolePermissions.Update, 16),

        new(AppUiNames.BackOfficeClientId, WebChannelMenu.IdentityManagement.Users, PermissionTypesold.LeafMenuPermission, $"{WebChannelMenu.IdentityManagement.Users}{WebChannelMenu.LeafPermissionName}", 10),
        new(AppUiNames.BackOfficeClientId, WebChannelMenu.IdentityManagement.Users, PermissionTypesold.ActionPermission, IdentityServicePermissions.AppUsers.PagedList, 11),
        new(AppUiNames.BackOfficeClientId, WebChannelMenu.IdentityManagement.Users, PermissionTypesold.ActionPermission, IdentityServicePermissions.AppUsers.Create, 12),
        new(AppUiNames.BackOfficeClientId, WebChannelMenu.IdentityManagement.Users, PermissionTypesold.ActionPermission, IdentityServicePermissions.AppUsers.Update, 13),
        new(AppUiNames.BackOfficeClientId, WebChannelMenu.IdentityManagement.Users, PermissionTypesold.ActionPermission, IdentityServicePermissions.AppUsers.Delete, 17),

        new(AppUiNames.BackOfficeClientId, WebChannelMenu.IdentityManagement.AuditLogs, PermissionTypesold.LeafMenuPermission, $"{WebChannelMenu.IdentityManagement.AuditLogs}{WebChannelMenu.LeafPermissionName}", 10),
        new(AppUiNames.BackOfficeClientId, WebChannelMenu.IdentityManagement.AuditLogs, PermissionTypesold.ActionPermission, IdentityServicePermissions.AuditLogs.PagedList, 11),

        new(AppUiNames.BackOfficeClientId, WebChannelMenu.PublisherManagement.List, PermissionTypesold.LeafMenuPermission, $"{WebChannelMenu.PublisherManagement.List}{WebChannelMenu.LeafPermissionName}", 10),
        new(AppUiNames.BackOfficeClientId, WebChannelMenu.PublisherManagement.SettingsMenu, PermissionTypesold.LeafMenuPermission, $"{WebChannelMenu.PublisherManagement.SettingsMenu}{WebChannelMenu.LeafPermissionName}", 10),
        new(AppUiNames.BackOfficeClientId, WebChannelMenu.PublisherManagement.AddPublisher, PermissionTypesold.LeafMenuPermission, $"{WebChannelMenu.PublisherManagement.AddPublisher}{WebChannelMenu.LeafPermissionName}", 10),
        new(AppUiNames.BackOfficeClientId, WebChannelMenu.PublisherManagement.AddUser, PermissionTypesold.LeafMenuPermission, $"{WebChannelMenu.PublisherManagement.AddUser}{WebChannelMenu.LeafPermissionName}", 10),

        new(AppUiNames.BackOfficeClientId, WebChannelMenu.IntegrationManagement.DomainCodes, PermissionTypesold.LeafMenuPermission, $"{WebChannelMenu.IntegrationManagement.DomainCodes}{WebChannelMenu.LeafPermissionName}", 10),

        new(AppUiNames.BackOfficeClientId, WebChannelMenu.Account.Settings, PermissionTypesold.LeafMenuPermission, $"{WebChannelMenu.Account.Settings}{WebChannelMenu.LeafPermissionName}", 10),
    };

    // public static IEnumerable<MenuMap> CommercialWebMenuMaps => new MenuMap[]
    // {
    //     new("commercial-web", null, "Menu:Dashboard", MenuMapType.NodeLeafItem, "/main/dashboard", "pi pi-fw pi-chart-line", 1, "", "DashboardService.Main.View"),
    //     new("commercial-web", null, "Menu:Administration", MenuMapType.NodeMenu, "", "pi pi-fw pi-wrench", 2, "", null),
    //     new("commercial-web", "Menu:Administration", "Menu:IdentityManagement", MenuMapType.NodeMenu, "", "pi pi-fw pi-users", 1, "", null),
    //     new("commercial-web", "Menu:IdentityManagement", "Menu:Roles", MenuMapType.NodeLeafMenu, "/administration/identity/roles", "pi pi-fw pi-minus", 1, "", IdentityServicePermissions.AppRoles.View),
    //     new("commercial-web", "Menu:Roles", "Menu:Roles:Default", MenuMapType.NodeLeafActionPermission, "", "", 1, "", IdentityServicePermissions.AppRoles.View),
    //     new("commercial-web", "Menu:Roles", "Menu:Roles:Create", MenuMapType.NodeLeafActionPermission, "", "", 2, "", IdentityServicePermissions.AppRoles.Create),
    //     new("commercial-web", "Menu:Roles", "Menu:Roles:Update", MenuMapType.NodeLeafActionPermission, "", "", 3, "", IdentityServicePermissions.AppRoles.Update),
    //     new("commercial-web", "Menu:Roles", "Menu:Roles:Delete", MenuMapType.NodeLeafActionPermission, "", "", 4, "", IdentityServicePermissions.AppRoles.Delete),
    //     new("commercial-web", "Menu:Roles", "Menu:Roles:Permissions", MenuMapType.NodeLeafActionMenu, "", "", 5, "", AdministrationServicePermissions.RolePermissions.View),
    //     new("commercial-web", "Menu:Roles:Permissions", "Menu:Roles:Permissions:Default", MenuMapType.NodeLeafActionPermission, "", "", 1, "", AdministrationServicePermissions.RolePermissions.View),
    //     new("commercial-web", "Menu:Roles:Permissions", "Menu:Roles:Permissions:Manage", MenuMapType.NodeLeafActionPermission, "", "", 2, "", AdministrationServicePermissions.RolePermissions.Manage),
    //     new("commercial-web", "Menu:IdentityManagement", "Menu:Users", MenuMapType.NodeLeafMenu, "/administration/identity/users", "pi pi-fw pi-minus", 2, "", IdentityServicePermissions.AppUsers.View),
    //     new("commercial-web", "Menu:Users", "Menu:Users:Default", MenuMapType.NodeLeafActionPermission, "", "", 1, "", IdentityServicePermissions.AppUsers.View),
    //     new("commercial-web", "Menu:Users", "Menu:Users:Create", MenuMapType.NodeLeafActionPermission, "", "", 2, "", IdentityServicePermissions.AppUsers.Create),
    //     new("commercial-web", "Menu:Users", "Menu:Users:Update", MenuMapType.NodeLeafActionMenu, "", "", 3, "", IdentityServicePermissions.AppUsers.Update),
    //     new("commercial-web", "Menu:Users:Update", "Menu:Users:Update:Default", MenuMapType.NodeLeafActionPermission, "", "", 1, "", IdentityServicePermissions.AppUsers.Update),
    //     new("commercial-web", "Menu:Users:Update", "Menu:Users:Update:Email", MenuMapType.NodeLeafActionPermission, "", "", 2, "", IdentityServicePermissions.AppUsers.UpdateFields.Email),
    //     new("commercial-web", "Menu:Users:Update", "Menu:Users:Update:Phone", MenuMapType.NodeLeafActionPermission, "", "", 3, "", IdentityServicePermissions.AppUsers.UpdateFields.Phone),
    //     new("commercial-web", "Menu:Users:Update", "Menu:Users:Update:IsBlock", MenuMapType.NodeLeafActionPermission, "", "", 4, "", IdentityServicePermissions.AppUsers.UpdateFields.IsBlock),
    //     new("commercial-web", "Menu:Users", "Menu:Users:Delete", MenuMapType.NodeLeafActionPermission, "", "", 4, "", IdentityServicePermissions.AppUsers.Delete),
    //     new("commercial-web", "Menu:IdentityManagement", "Menu:SecurityLogs", MenuMapType.NodeLeafItem, "/administration/identity/security-logs", "pi pi-fw pi-minus", 3, "", IdentityServicePermissions.SecurityLogs.View),
    //     new("commercial-web", "Menu:Administration", "Menu:AuditLogs", MenuMapType.NodeLeafItem, "/administration/audit-logs", "pi pi-fw pi-prime", 2, "", AdministrationServicePermissions.AuditLogs.View),
    //     new("commercial-web", "Menu:Administration", "Menu:Settings", MenuMapType.NodeLeafItem, "/administration/settings", "pi pi-fw pi-cog", 3, "", AdministrationServicePermissions.Settings.View),
    //
    //     new("commercial-web", null, "Menu:TenantManagement", MenuMapType.NodeMenu, "", "pi pi-fw pi-building", 3, "", null),
    //     new("commercial-web", "Menu:TenantManagement", "Menu:Tenants", MenuMapType.NodeLeafMenu, "/tenant/tenants", "pi pi-fw pi-id-card", 1, "", TenantServicePermissions.Tenants.View, true),
    //     new("commercial-web", "Menu:Tenants", "Menu:Tenants:Default", MenuMapType.NodeLeafActionPermission, "", "", 1, "", TenantServicePermissions.Tenants.View),
    //     new("commercial-web", "Menu:Tenants", "Menu:Tenants:Detail", MenuMapType.NodeLeafActionMenu, "", "", 2, "", TenantServicePermissions.Tenants.Detail),
    //     new("commercial-web", "Menu:Tenants:Detail", "Menu:Tenants:Detail:Default", MenuMapType.NodeLeafActionPermission, "", "", 1, "", TenantServicePermissions.Tenants.Detail),
    //     new("commercial-web", "Menu:Tenants:Detail", "Menu:Tenants:Detail:Email", MenuMapType.NodeLeafActionPermission, "", "", 2, "", TenantServicePermissions.Tenants.DetailFields.Email),
    //     new("commercial-web", "Menu:Tenants:Detail", "Menu:Tenants:Detail:Phone", MenuMapType.NodeLeafActionPermission, "", "", 3, "", TenantServicePermissions.Tenants.DetailFields.Phone),
    //     new("commercial-web", "Menu:Tenants", "Menu:Tenants:Create", MenuMapType.NodeLeafActionPermission, "", "", 3, "", TenantServicePermissions.Tenants.Create),
    //     new("commercial-web", "Menu:Tenants", "Menu:Tenants:Update", MenuMapType.NodeLeafActionMenu, "", "", 4, "", TenantServicePermissions.Tenants.Update),
    //     new("commercial-web", "Menu:Tenants:Update", "Menu:Tenants:Update:Default", MenuMapType.NodeLeafActionPermission, "", "", 1, "", TenantServicePermissions.Tenants.Update),
    //     new("commercial-web", "Menu:Tenants:Update", "Menu:Tenants:Update:IdentityCode", MenuMapType.NodeLeafActionPermission, "", "", 2, "", TenantServicePermissions.Tenants.UpdateFields.IdentityCode),
    //     new("commercial-web", "Menu:Tenants:Update", "Menu:Tenants:Update:Email", MenuMapType.NodeLeafActionPermission, "", "", 3, "", TenantServicePermissions.Tenants.UpdateFields.Email),
    //     new("commercial-web", "Menu:Tenants:Update", "Menu:Tenants:Update:Phone", MenuMapType.NodeLeafActionPermission, "", "", 4, "", TenantServicePermissions.Tenants.UpdateFields.Phone),
    //     new("commercial-web", "Menu:Tenants", "Menu:Tenants:Delete", MenuMapType.NodeLeafActionPermission, "", "", 5, "", TenantServicePermissions.Tenants.Delete),
    //
    //     // new("commercial-web", null, "Menu:CatalogManagement", MenuMapType.NodeMenu, "", "pi pi-fw pi-sitemap", 4, "", null),
    //     //
    //     // new("commercial-web", "Menu:CatalogManagement", "Menu:CategoryManagement", MenuMapType.NodeMenu, "", "pi pi-fw pi-tags", 1, "", null),
    //     // new("commercial-web", "Menu:CategoryManagement", "Menu:Categories", MenuMapType.NodeLeafMenu, "/catalog/category/categories", "pi pi-fw pi-minus", 1, "", CatalogServicePermissions.Categories.View, true),
    //     // new("commercial-web", "Menu:Categories", "Menu:Categories:Default", MenuMapType.NodeLeafActionPermission, "", "", 1, "", CatalogServicePermissions.Categories.View),
    //     // new("commercial-web", "Menu:Categories", "Menu:Categories:Create", MenuMapType.NodeLeafActionPermission, "", "", 2, "", CatalogServicePermissions.Categories.Create),
    //     // new("commercial-web", "Menu:Categories", "Menu:Categories:Update", MenuMapType.NodeLeafActionPermission, "", "", 3, "", CatalogServicePermissions.Categories.Update),
    //     // new("commercial-web", "Menu:Categories", "Menu:Categories:Delete", MenuMapType.NodeLeafActionPermission, "", "", 4, "", CatalogServicePermissions.Categories.Delete),
    //     // new("commercial-web", "Menu:CategoryManagement", "Menu:CategoryBrands", MenuMapType.NodeLeafMenu, "/catalog/category/category-brands", "pi pi-fw pi-minus", 2, "", CatalogServicePermissions.CategoryBrands.View, true),
    //     // new("commercial-web", "Menu:CategoryBrands", "Menu:CategoryBrands:Default", MenuMapType.NodeLeafActionPermission, "", "", 1, "", CatalogServicePermissions.CategoryBrands.View),
    //     // new("commercial-web", "Menu:CategoryBrands", "Menu:CategoryBrands:Create", MenuMapType.NodeLeafActionPermission, "", "", 2, "", CatalogServicePermissions.CategoryBrands.Create),
    //     // new("commercial-web", "Menu:CategoryBrands", "Menu:CategoryBrands:Update", MenuMapType.NodeLeafActionPermission, "", "", 3, "", CatalogServicePermissions.CategoryBrands.Update),
    //     // new("commercial-web", "Menu:CategoryBrands", "Menu:CategoryBrands:Delete", MenuMapType.NodeLeafActionPermission, "", "", 4, "", CatalogServicePermissions.CategoryBrands.Delete),
    //     // new("commercial-web", "Menu:CategoryManagement", "Menu:CategorySpecs", MenuMapType.NodeLeafMenu, "/catalog/category/category-specs", "pi pi-fw pi-minus", 3, "", CatalogServicePermissions.CategorySpecs.View, true),
    //     // new("commercial-web", "Menu:CategorySpecs", "Menu:CategorySpecs:Default", MenuMapType.NodeLeafActionPermission, "", "", 1, "", CatalogServicePermissions.CategorySpecs.View),
    //     // new("commercial-web", "Menu:CategorySpecs", "Menu:CategorySpecs:Create", MenuMapType.NodeLeafActionPermission, "", "", 2, "", CatalogServicePermissions.CategorySpecs.Create),
    //     // new("commercial-web", "Menu:CategorySpecs", "Menu:CategorySpecs:Update", MenuMapType.NodeLeafActionPermission, "", "", 3, "", CatalogServicePermissions.CategorySpecs.Update),
    //     // new("commercial-web", "Menu:CategorySpecs", "Menu:CategorySpecs:Delete", MenuMapType.NodeLeafActionPermission, "", "", 4, "", CatalogServicePermissions.CategorySpecs.Delete),
    //     //
    //     // new("commercial-web", "Menu:CatalogManagement", "Menu:ProductManagement", MenuMapType.NodeMenu, "", "pi pi-fw pi-tags", 2, "", null),
    //     //
    //     // new("commercial-web", "Menu:ProductManagement", "Menu:Products", MenuMapType.NodeLeafMenu, "/catalog/product/products", "pi pi-fw pi-minus", 1, "", CatalogServicePermissions.Products.View),
    //     // new("commercial-web", "Menu:Products", "Menu:Products:Default", MenuMapType.NodeLeafActionPermission, "", "", 1, "", CatalogServicePermissions.Products.View),
    //     // new("commercial-web", "Menu:Products", "Menu:Products:Create", MenuMapType.NodeLeafActionPermission, "", "", 2, "", CatalogServicePermissions.Products.Create),
    //     // new("commercial-web", "Menu:Products", "Menu:Products:Update", MenuMapType.NodeLeafActionPermission, "", "", 3, "", CatalogServicePermissions.Products.Update),
    //     // new("commercial-web", "Menu:Products", "Menu:Products:Delete", MenuMapType.NodeLeafActionPermission, "", "", 4, "", CatalogServicePermissions.Products.Delete),
    //     // new("commercial-web", "Menu:ProductManagement", "Menu:ProductFiles", MenuMapType.NodeLeafMenu, "/catalog/product/product-files", "pi pi-fw pi-minus", 2, "", CatalogServicePermissions.ProductFiles.View),
    //     // new("commercial-web", "Menu:ProductFiles", "Menu:ProductFiles:Default", MenuMapType.NodeLeafActionPermission, "", "", 1, "", CatalogServicePermissions.ProductFiles.View),
    //     // new("commercial-web", "Menu:ProductFiles", "Menu:ProductFiles:Create", MenuMapType.NodeLeafActionPermission, "", "", 2, "", CatalogServicePermissions.ProductFiles.Create),
    //     // new("commercial-web", "Menu:ProductFiles", "Menu:ProductFiles:Update", MenuMapType.NodeLeafActionPermission, "", "", 3, "", CatalogServicePermissions.ProductFiles.Update),
    //     // new("commercial-web", "Menu:ProductFiles", "Menu:ProductFiles:Delete", MenuMapType.NodeLeafActionPermission, "", "", 4, "", CatalogServicePermissions.ProductFiles.Delete),
    //     // new("commercial-web", "Menu:ProductManagement", "Menu:ProductCategorySpecs", MenuMapType.NodeLeafMenu, "/catalog/product/product-category-specs", "pi pi-fw pi-minus", 3, "", CatalogServicePermissions.ProductCategorySpecs.View),
    //     // new("commercial-web", "Menu:ProductCategorySpecs", "Menu:ProductCategorySpecs:Default", MenuMapType.NodeLeafActionPermission, "", "", 1, "", CatalogServicePermissions.ProductCategorySpecs.View),
    //     // new("commercial-web", "Menu:ProductCategorySpecs", "Menu:ProductCategorySpecs:Create", MenuMapType.NodeLeafActionPermission, "", "", 2, "", CatalogServicePermissions.ProductCategorySpecs.Create),
    //     // new("commercial-web", "Menu:ProductCategorySpecs", "Menu:ProductCategorySpecs:Update", MenuMapType.NodeLeafActionPermission, "", "", 3, "", CatalogServicePermissions.ProductCategorySpecs.Update),
    //     // new("commercial-web", "Menu:ProductCategorySpecs", "Menu:ProductCategorySpecs:Delete", MenuMapType.NodeLeafActionPermission, "", "", 4, "", CatalogServicePermissions.ProductCategorySpecs.Delete),
    //     //
    //     //
    //     // new("commercial-web", "Menu:CatalogManagement", "Menu:StoreManagement", MenuMapType.NodeMenu, "", "pi pi-fw pi-tags", 3, "", null),
    //     // new("commercial-web", "Menu:StoreManagement", "Menu:StoreItems", MenuMapType.NodeLeafMenu, "/catalog/store/store-items", "pi pi-fw pi-minus", 1, "", CatalogServicePermissions.StoreItems.View),
    //     // new("commercial-web", "Menu:StoreItems", "Menu:StoreItems:Default", MenuMapType.NodeLeafActionPermission, "", "", 1, "", CatalogServicePermissions.StoreItems.View),
    //     // new("commercial-web", "Menu:StoreItems", "Menu:StoreItems:Create", MenuMapType.NodeLeafActionPermission, "", "", 2, "", CatalogServicePermissions.StoreItems.Create),
    //     // new("commercial-web", "Menu:StoreItems", "Menu:StoreItems:Update", MenuMapType.NodeLeafActionPermission, "", "", 3, "", CatalogServicePermissions.StoreItems.Update),
    //     // new("commercial-web", "Menu:StoreItems", "Menu:StoreItems:Delete", MenuMapType.NodeLeafActionPermission, "", "", 4, "", CatalogServicePermissions.StoreItems.Delete),
    //     // new("commercial-web", "Menu:StoreManagement", "Menu:StoreItemDiscounts", MenuMapType.NodeLeafMenu, "/catalog/store/store-item-discounts", "pi pi-fw pi-minus", 2, "", CatalogServicePermissions.StoreItemDiscounts.View),
    //     // new("commercial-web", "Menu:StoreItemDiscounts", "Menu:StoreItemDiscounts:Default", MenuMapType.NodeLeafActionPermission, "", "", 1, "", CatalogServicePermissions.StoreItemDiscounts.View),
    //     // new("commercial-web", "Menu:StoreItemDiscounts", "Menu:StoreItemDiscounts:Create", MenuMapType.NodeLeafActionPermission, "", "", 2, "", CatalogServicePermissions.StoreItemDiscounts.Create),
    //     // new("commercial-web", "Menu:StoreItemDiscounts", "Menu:StoreItemDiscounts:Update", MenuMapType.NodeLeafActionPermission, "", "", 3, "", CatalogServicePermissions.StoreItemDiscounts.Update),
    //     // new("commercial-web", "Menu:StoreItemDiscounts", "Menu:StoreItemDiscounts:Delete", MenuMapType.NodeLeafActionPermission, "", "", 4, "", CatalogServicePermissions.StoreItemDiscounts.Delete),
    //     // new("commercial-web", "Menu:StoreManagement", "Menu:StoreItemEvaluations", MenuMapType.NodeLeafMenu, "/catalog/store/store-item-evaluations", "pi pi-fw pi-minus", 3, "", CatalogServicePermissions.StoreItemEvaluations.View),
    //     // new("commercial-web", "Menu:StoreItemEvaluations", "Menu:StoreItemEvaluations:Default", MenuMapType.NodeLeafActionPermission, "", "", 1, "", CatalogServicePermissions.StoreItemEvaluations.View),
    //     // new("commercial-web", "Menu:StoreItemEvaluations", "Menu:StoreItemEvaluations:Create", MenuMapType.NodeLeafActionPermission, "", "", 2, "", CatalogServicePermissions.StoreItemEvaluations.Create),
    //     // new("commercial-web", "Menu:StoreItemEvaluations", "Menu:StoreItemEvaluations:Update", MenuMapType.NodeLeafActionPermission, "", "", 3, "", CatalogServicePermissions.StoreItemEvaluations.Update),
    //     // new("commercial-web", "Menu:StoreItemEvaluations", "Menu:StoreItemEvaluations:Delete", MenuMapType.NodeLeafActionPermission, "", "", 4, "", CatalogServicePermissions.StoreItemEvaluations.Delete),
    //     //
    //     // new("commercial-web", null, "Menu:BasketManagement", MenuMapType.NodeMenu, "", "pi pi-fw pi-shopping-cart", 5, "", null),
    //     // new("commercial-web", "Menu:BasketManagement", "Menu:Baskets", MenuMapType.NodeLeafMenu, "/basket/baskets", "pi pi-fw pi-shopping-bag", 1, "", BasketServicePermissions.Baskets.View),
    //     // new("commercial-web", "Menu:Baskets", "Menu:Baskets:Default", MenuMapType.NodeLeafActionPermission, "", "", 1, "", BasketServicePermissions.Baskets.View),
    //     // new("commercial-web", "Menu:Baskets", "Menu:Baskets:Update", MenuMapType.NodeLeafActionPermission, "", "", 2, "", BasketServicePermissions.Baskets.Update),
    //     // new("commercial-web", "Menu:Baskets", "Menu:Baskets:Delete", MenuMapType.NodeLeafActionPermission, "", "", 3, "", BasketServicePermissions.Baskets.Delete),
    //     //
    //     // new("commercial-web", null, "Menu:CustomerManagement", MenuMapType.NodeMenu, "", "pi pi-fw pi-briefcase", 6, "", null),
    //     // new("commercial-web", "Menu:CustomerManagement", "Menu:Customers", MenuMapType.NodeLeafMenu, "/customer/customers", "pi pi-fw pi-wallet", 1, "", CustomerServicePermissions.Customers.View),
    //     // new("commercial-web", "Menu:Customers", "Menu:Customers:Default", MenuMapType.NodeLeafActionPermission, "", "", 1, "", CustomerServicePermissions.Customers.View),
    //     // new("commercial-web", "Menu:Customers", "Menu:Customers:Create", MenuMapType.NodeLeafActionPermission, "", "", 2, "", CustomerServicePermissions.Customers.Create),
    //     // new("commercial-web", "Menu:Customers", "Menu:Customers:Update", MenuMapType.NodeLeafActionMenu, "", "", 3, "", CustomerServicePermissions.Customers.Update),
    //     // new("commercial-web", "Menu:Customers:Update", "Menu:Customers:Update:Default", MenuMapType.NodeLeafActionPermission, "", "", 1, "", CustomerServicePermissions.Customers.Update),
    //     // new("commercial-web", "Menu:Customers:Update", "Menu:Customers:Update:Address", MenuMapType.NodeLeafActionPermission, "", "", 2, "", CustomerServicePermissions.Customers.UpdateFields.Address),
    //     // new("commercial-web", "Menu:Customers:Update", "Menu:Customers:Update:Email", MenuMapType.NodeLeafActionPermission, "", "", 3, "", CustomerServicePermissions.Customers.UpdateFields.Email),
    //     // new("commercial-web", "Menu:Customers:Update", "Menu:Customers:Update:Phone", MenuMapType.NodeLeafActionPermission, "", "", 4, "", CustomerServicePermissions.Customers.UpdateFields.Phone),
    //     // new("commercial-web", "Menu:Customers", "Menu:Customers:Delete", MenuMapType.NodeLeafActionPermission, "", "", 4, "", CustomerServicePermissions.Customers.Delete),
    //     //
    //     // new("commercial-web", null, "Menu:OrderManagement", MenuMapType.NodeMenu, "", "pi pi-fw pi-globe", 7, "", null),
    //     // new("commercial-web", "Menu:OrderManagement", "Menu:Orders", MenuMapType.NodeLeafMenu, "/order/orders", "pi pi-fw pi-inbox", 1, "", OrderServicePermissions.Orders.View),
    //     // new("commercial-web", "Menu:Orders", "Menu:Orders:Default", MenuMapType.NodeLeafActionPermission, "", "", 1, "", OrderServicePermissions.Orders.View),
    //     // new("commercial-web", "Menu:Orders", "Menu:Orders:Detail", MenuMapType.NodeLeafActionMenu, "", "", 2, "", OrderServicePermissions.Orders.Detail),
    //     // new("commercial-web", "Menu:Orders:Detail", "Menu:Orders:Detail:Default", MenuMapType.NodeLeafActionPermission, "", "", 1, "", OrderServicePermissions.Orders.Detail),
    //     // new("commercial-web", "Menu:Orders:Detail", "Menu:Orders:Detail:Address", MenuMapType.NodeLeafActionPermission, "", "", 2, "", OrderServicePermissions.Orders.DetailFields.Address),
    //     // new("commercial-web", "Menu:Orders:Detail", "Menu:Orders:Detail:Price", MenuMapType.NodeLeafActionPermission, "", "", 3, "", OrderServicePermissions.Orders.DetailFields.Price),
    //     // new("commercial-web", "Menu:Orders", "Menu:Orders:Create", MenuMapType.NodeLeafActionPermission, "", "", 3, "", OrderServicePermissions.Orders.Create),
    //     // new("commercial-web", "Menu:Orders", "Menu:Orders:Update", MenuMapType.NodeLeafActionMenu, "", "", 4, "", OrderServicePermissions.Orders.Update),
    //     // new("commercial-web", "Menu:Orders:Update", "Menu:Orders:Update:Default", MenuMapType.NodeLeafActionPermission, "", "", 1, "", OrderServicePermissions.Orders.Update),
    //     // new("commercial-web", "Menu:Orders:Update", "Menu:Orders:Update:OrderDate", MenuMapType.NodeLeafActionPermission, "", "", 2, "", OrderServicePermissions.Orders.UpdateFields.OrderDate),
    //     // new("commercial-web", "Menu:Orders:Update", "Menu:Orders:Update:Address", MenuMapType.NodeLeafActionPermission, "", "", 3, "", OrderServicePermissions.Orders.UpdateFields.Address),
    //     // new("commercial-web", "Menu:Orders:Update", "Menu:Orders:Update:OrderItems", MenuMapType.NodeLeafActionPermission, "", "", 4, "", OrderServicePermissions.Orders.UpdateFields.OrderItems),
    //     // new("commercial-web", "Menu:Orders", "Menu:Orders:Delete", MenuMapType.NodeLeafActionPermission, "", "", 5, "", OrderServicePermissions.Orders.Delete),
    //     //
    //     // new("commercial-web", null, "Menu:PaymentManagement", MenuMapType.NodeMenu, "", "pi pi-fw pi-money-bill", 8, "", null),
    //     // new("commercial-web", "Menu:PaymentManagement", "Menu:PaymentRequests", MenuMapType.NodeLeafMenu, "/payment/payment-requests", "pi pi-fw pi-file-import", 1, "", PaymentServicePermissions.PaymentRequests.View),
    //     // new("commercial-web", "Menu:PaymentRequests", "Menu:PaymentRequests:Default", MenuMapType.NodeLeafActionMenu, "", "", 1, "", PaymentServicePermissions.PaymentRequests.View),
    //     // new("commercial-web", "Menu:PaymentRequests", "Menu:PaymentRequests:Detail", MenuMapType.NodeLeafActionMenu, "", "", 2, "", PaymentServicePermissions.PaymentRequests.Detail),
    //     // new("commercial-web", "Menu:PaymentRequests:Detail", "Menu:PaymentRequests:Detail:Default", MenuMapType.NodeLeafActionPermission, "", "", 1, "", PaymentServicePermissions.PaymentRequests.Detail),
    //     // new("commercial-web", "Menu:PaymentRequests:Detail", "Menu:PaymentRequests:Detail:Price", MenuMapType.NodeLeafActionPermission, "", "", 2, "", PaymentServicePermissions.PaymentRequests.DetailFields.Price),
    //     // new("commercial-web", "Menu:PaymentRequests", "Menu:PaymentRequests:Update", MenuMapType.NodeLeafActionPermission, "", "", 3, "", PaymentServicePermissions.PaymentRequests.Update),
    //     // new("commercial-web", "Menu:PaymentRequests", "Menu:PaymentRequests:Delete", MenuMapType.NodeLeafActionPermission, "", "", 4, "", PaymentServicePermissions.PaymentRequests.Delete)
    // };
}