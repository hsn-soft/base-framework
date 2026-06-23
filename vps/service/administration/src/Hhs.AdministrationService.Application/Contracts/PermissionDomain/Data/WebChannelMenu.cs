namespace Hhs.AdministrationService.Application.Contracts.PermissionDomain.Data;

public static class WebChannelMenu
{
    public const string LeafPermissionName = ":ViewMenu";
    private const string GroupName = "Menu.";

    public static class Dashboards
    {
        public const string NodeName = $"{GroupName}{nameof(Dashboards)}";

        public const string Summary = $"{NodeName}:Summary";
        public const string Traffic = $"{NodeName}:Traffic";
    }

    // OPERATION
    public const string Operation = $"{GroupName}Operation"; // CAPTION

    public static class ContentManagement
    {
        public const string NodeName = $"{GroupName}{nameof(ContentManagement)}";

        public const string Contents = $"{NodeName}:Contents";
        public const string Visits = $"{NodeName}:Visits";
        public const string VideoPlatform = $"{NodeName}:VideoPlatform";
        public const string VideoPlatformOverview = $"{VideoPlatform}:Overview";
        public const string VideoPlatformAnalysisVideo = $"{VideoPlatform}:AnalysisVideo";
        public const string VideoPlatformSpecialVideo = $"{VideoPlatform}:SpecialVideo";
        public const string VideoPlatformCreateAVideo = $"{VideoPlatform}:CreateAVideo";
        public const string VideoPlatformReporting = $"{VideoPlatform}:Reporting";
        public const string VerticalVideo = $"{NodeName}:VerticalVideo";
        public const string Podcast = $"{NodeName}:Podcast";
        public const string AdWall = $"{NodeName}:AdWall";
        public const string BiddingTech = $"{NodeName}:BiddingTech";
    }

    public const string Contents = $"{GroupName}Contents";

    public static class Inventory
    {
        public const string NodeName = $"{GroupName}{nameof(Inventory)}";

        public const string Codes = $"{NodeName}:Codes";
    }

    public static class Payments
    {
        public const string NodeName = $"{GroupName}{nameof(Payments)}";

        public const string List = $"{NodeName}:List";
        public const string Detail = $"{NodeName}:Detail";
    }

    public static class Invoice
    {
        public const string NodeName = $"{GroupName}{nameof(Invoice)}";

        public const string List = $"{NodeName}:List";
        public const string Detail = $"{NodeName}:Detail";
    }

    public static class InvoiceManagement
    {
        public const string NodeName = $"{GroupName}{nameof(InvoiceManagement)}";

        public const string Invoices = $"{NodeName}:Invoices";
        public const string CostAnalysis = $"{NodeName}:CostAnalysis";
    }

    public static class Reporting
    {
        public const string NodeName = $"{GroupName}{nameof(Reporting)}";

        public const string TrendContentReport = $"{NodeName}:TrendContentReport";
        public const string AnalysisContentReport = $"{NodeName}:AnalysisContentReport";
    }

    // ADMINISTRATION
    public const string Administration = $"{GroupName}Administration"; // CAPTION

    public static class TenantManagement // Tenant Management for Only System Users
    {
        public const string NodeName = $"{GroupName}{nameof(TenantManagement)}";

        public const string Tenants = $"{NodeName}:Tenants";
        public const string Clients = $"{NodeName}:Clients";
    }

    public static class IdentityManagement
    {
        public const string NodeName = $"{GroupName}{nameof(IdentityManagement)}";

        public const string Roles = $"{NodeName}:Roles";
        public const string Users = $"{NodeName}:Users";
        public const string AuditLogs = $"{NodeName}:AuditLogs";
    }

    public static class PublisherManagement
    {
        public const string NodeName = $"{GroupName}{nameof(PublisherManagement)}";

        public const string List = $"{NodeName}:List";
        public const string SettingsMenu = $"{NodeName}:SettingsMenu";
        public const string AddPublisher = $"{NodeName}:AddPublisher";
        public const string AddUser = $"{NodeName}:AddUser";
    }

    public static class IntegrationManagement
    {
        public const string NodeName = $"{GroupName}{nameof(IntegrationManagement)}";

        public const string DomainCodes = $"{NodeName}:DomainCodes";
    }

    public static class Account
    {
        public const string NodeName = $"{GroupName}{nameof(Account)}";

        public const string Profile = $"{NodeName}:Profile";
        public const string Settings = $"{NodeName}:Settings";
    }
}