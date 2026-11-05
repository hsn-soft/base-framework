namespace Hsx.Shared.Contracts.Permissions;

/// <summary>The permissions of the text-normalizer service.</summary>
public static class TextNormalizerPermissions
{
    /// <summary>The pages of the back office of this service: <c>page.{page}.view</c>, the visibility permission of a menu item (the front end only).</summary>
    public static class Pages
    {
        public static class NormalizeRequests
        {
            /// <summary>Lets a user into the page.</summary>
            public const string View = "page.normalize-requests.view";
        }
    }

    /// <summary>One permission for each action of a controller: <c>endpoint.{resource}.{action}</c>, the argument of <c>[PermissionAuthorize]</c>.</summary>
    public static class Endpoints
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
}
