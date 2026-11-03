namespace Hsx.Shared.Helper;

/// <summary>
/// The permissions that let a user into a page of the back office: <c>page.{page}.view</c>. They are the visibility permission of a menu item: a page without it is not in the menu of the user and its address does not open in the front end. It is for the
/// front end only; what the page reads or does is the endpoint permissions' to decide.
/// </summary>
public static class HsxPagePermissions
{
    public static class Identity
    {
        public static class Tenants
        {
            public const string View = "page.tenants.view";
        }

        public static class Users
        {
            public const string View = "page.users.view";
        }

        public static class Roles
        {
            public const string View = "page.roles.view";
        }

        public static class Companies
        {
            public const string View = "page.companies.view";
        }

        public static class Customers
        {
            public const string View = "page.customers.view";
        }

        public static class Subscriptions
        {
            public const string View = "page.subscriptions.view";
        }
    }

    public static class Content
    {
        public static class CustomerContents
        {
            public const string View = "page.customer-contents.view";
        }

        public static class AnalysisContents
        {
            public const string View = "page.analysis-contents.view";
        }

        public static class CustomerContentVisits
        {
            public const string View = "page.customer-content-visits.view";
        }

        public static class CustomerContentVpSettings
        {
            public const string View = "page.customer-content-vp-settings.view";
        }
    }

    public static class TextNormalizer
    {
        public static class NormalizeRequests
        {
            public const string View = "page.normalize-requests.view";
        }
    }

    public static class VideoGenerator
    {
        public static class VideoRequests
        {
            public const string View = "page.video-requests.view";
        }
    }
}
