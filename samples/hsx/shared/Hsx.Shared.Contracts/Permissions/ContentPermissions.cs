namespace Hsx.Shared.Contracts.Permissions;

/// <summary>The permissions of the content service.</summary>
public static class ContentPermissions
{
    /// <summary>The pages of the back office of this service: <c>page.{page}.view</c>, the visibility permission of a menu item (the front end only).</summary>
    public static class Pages
    {
        public static class CustomerContents
        {
            /// <summary>Lets a user into the page.</summary>
            public const string View = "page.customer-contents.view";
        }

        public static class AnalysisContents
        {
            /// <summary>Lets a user into the page.</summary>
            public const string View = "page.analysis-contents.view";
        }

        public static class CustomerContentVisits
        {
            /// <summary>Lets a user into the page.</summary>
            public const string View = "page.customer-content-visits.view";
        }

        public static class CustomerContentVpSettings
        {
            /// <summary>Lets a user into the page.</summary>
            public const string View = "page.customer-content-vp-settings.view";
        }
    }

    /// <summary>One permission for each action of a controller: <c>endpoint.{resource}.{action}</c>, the argument of <c>[PermissionAuthorize]</c>.</summary>
    public static class Endpoints
    {
        public static class CustomerContents
        {
            /// <summary>Read (get and the lists).</summary>
            public const string Read = "endpoint.customer-contents.read";

            /// <summary>Create.</summary>
            public const string Create = "endpoint.customer-contents.create";

            /// <summary>Update.</summary>
            public const string Update = "endpoint.customer-contents.update";

            /// <summary>Delete.</summary>
            public const string Delete = "endpoint.customer-contents.delete";
        }

        public static class AnalysisContents
        {
            /// <summary>Read (get and the lists).</summary>
            public const string Read = "endpoint.analysis-contents.read";

            /// <summary>Create.</summary>
            public const string Create = "endpoint.analysis-contents.create";

            /// <summary>Update.</summary>
            public const string Update = "endpoint.analysis-contents.update";

            /// <summary>Delete.</summary>
            public const string Delete = "endpoint.analysis-contents.delete";
        }

        public static class AnalysisContentItems
        {
            /// <summary>Read (get and the lists).</summary>
            public const string Read = "endpoint.analysis-content-items.read";

            /// <summary>Create.</summary>
            public const string Create = "endpoint.analysis-content-items.create";

            /// <summary>Update.</summary>
            public const string Update = "endpoint.analysis-content-items.update";

            /// <summary>Delete.</summary>
            public const string Delete = "endpoint.analysis-content-items.delete";
        }

        public static class CustomerContentVisits
        {
            /// <summary>Read (get and the lists).</summary>
            public const string Read = "endpoint.customer-content-visits.read";

            /// <summary>Create.</summary>
            public const string Create = "endpoint.customer-content-visits.create";

            /// <summary>Update.</summary>
            public const string Update = "endpoint.customer-content-visits.update";

            /// <summary>Delete.</summary>
            public const string Delete = "endpoint.customer-content-visits.delete";
        }

        public static class CustomerContentVpSettings
        {
            /// <summary>Read (get and the lists).</summary>
            public const string Read = "endpoint.customer-content-vp-settings.read";

            /// <summary>Create.</summary>
            public const string Create = "endpoint.customer-content-vp-settings.create";

            /// <summary>Update.</summary>
            public const string Update = "endpoint.customer-content-vp-settings.update";

            /// <summary>Delete.</summary>
            public const string Delete = "endpoint.customer-content-vp-settings.delete";
        }
    }
}
