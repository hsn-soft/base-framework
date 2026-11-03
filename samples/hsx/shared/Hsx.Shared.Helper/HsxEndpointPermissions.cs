namespace Hsx.Shared.Helper;

/// <summary>
/// The permissions of the endpoints: one for each action of a controller, named <c>endpoint.{resource}.{action}</c>. They are the argument of <c>[PermissionAuthorize]</c> and the identity service makes its catalogue from these constants, so an endpoint can only ask for a
/// permission that can be granted. A page reads with the <c>read</c> of its resource (the menu calls it required) and offers a button for each of the other actions.
/// </summary>
public static class HsxEndpointPermissions
{
    public static class Identity
    {
        public static class Tenants
        {
            /// <summary>Read (get and the lists).</summary>
            public const string Read = "endpoint.tenants.read";

            /// <summary>Create.</summary>
            public const string Create = "endpoint.tenants.create";

            /// <summary>Update.</summary>
            public const string Update = "endpoint.tenants.update";

            /// <summary>Delete.</summary>
            public const string Delete = "endpoint.tenants.delete";

            /// <summary>Move a tenant, with the tenants under it, to another parent.</summary>
            public const string Move = "endpoint.tenants.move";
        }

        public static class AppUsers
        {
            /// <summary>Read (get and the lists).</summary>
            public const string Read = "endpoint.app-users.read";

            /// <summary>Create.</summary>
            public const string Create = "endpoint.app-users.create";

            /// <summary>Update.</summary>
            public const string Update = "endpoint.app-users.update";

            /// <summary>Delete.</summary>
            public const string Delete = "endpoint.app-users.delete";

            /// <summary>Set the password of a user.</summary>
            public const string SetPassword = "endpoint.app-users.set-password";

            /// <summary>Give a user exactly these roles.</summary>
            public const string SetRoles = "endpoint.app-users.set-roles";
        }

        public static class AppRoles
        {
            /// <summary>Read (get and the lists).</summary>
            public const string Read = "endpoint.app-roles.read";

            /// <summary>Create.</summary>
            public const string Create = "endpoint.app-roles.create";

            /// <summary>Update.</summary>
            public const string Update = "endpoint.app-roles.update";

            /// <summary>Delete.</summary>
            public const string Delete = "endpoint.app-roles.delete";

            /// <summary>Give a role exactly these subscriptions.</summary>
            public const string SetSubscriptions = "endpoint.app-roles.set-subscriptions";
        }

        public static class Companies
        {
            /// <summary>Read (get and the lists).</summary>
            public const string Read = "endpoint.companies.read";

            /// <summary>Create.</summary>
            public const string Create = "endpoint.companies.create";

            /// <summary>Update.</summary>
            public const string Update = "endpoint.companies.update";

            /// <summary>Delete.</summary>
            public const string Delete = "endpoint.companies.delete";
        }

        public static class Customers
        {
            /// <summary>Read (get and the lists).</summary>
            public const string Read = "endpoint.customers.read";

            /// <summary>Create.</summary>
            public const string Create = "endpoint.customers.create";

            /// <summary>Update.</summary>
            public const string Update = "endpoint.customers.update";

            /// <summary>Delete.</summary>
            public const string Delete = "endpoint.customers.delete";
        }

        public static class Subscriptions
        {
            /// <summary>Read (get and the lists).</summary>
            public const string Read = "endpoint.subscriptions.read";

            /// <summary>Create.</summary>
            public const string Create = "endpoint.subscriptions.create";

            /// <summary>Update.</summary>
            public const string Update = "endpoint.subscriptions.update";

            /// <summary>Delete.</summary>
            public const string Delete = "endpoint.subscriptions.delete";
        }

        public static class Permissions
        {
            /// <summary>Read (get and the lists).</summary>
            public const string Read = "endpoint.permissions.read";
        }

        public static class RolePermissions
        {
            /// <summary>Read (get and the lists).</summary>
            public const string Read = "endpoint.role-permissions.read";

            /// <summary>Update.</summary>
            public const string Update = "endpoint.role-permissions.update";
        }
    }

    public static class Content
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

    public static class TextNormalizer
    {
        public static class CustomerContentNormalizedRequests
        {
            /// <summary>Read (get and the lists).</summary>
            public const string Read = "endpoint.customer-content-normalized-requests.read";

            /// <summary>Create.</summary>
            public const string Create = "endpoint.customer-content-normalized-requests.create";

            /// <summary>Update.</summary>
            public const string Update = "endpoint.customer-content-normalized-requests.update";

            /// <summary>Delete.</summary>
            public const string Delete = "endpoint.customer-content-normalized-requests.delete";
        }
    }

    public static class VideoGenerator
    {
        public static class VideoRequests
        {
            /// <summary>Read (get and the lists).</summary>
            public const string Read = "endpoint.video-requests.read";

            /// <summary>Create.</summary>
            public const string Create = "endpoint.video-requests.create";

            /// <summary>Update.</summary>
            public const string Update = "endpoint.video-requests.update";

            /// <summary>Delete.</summary>
            public const string Delete = "endpoint.video-requests.delete";
        }
    }
}
