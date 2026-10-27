namespace Edx.Shared.Helper;

/// <summary>
/// The permissions of the endpoints of the Edx system: one for each action of a controller (and of a gRPC or WCF operation), named <c>endpoint.{service}.{controller}.{action}</c>. They are the argument of <c>[PermissionAuthorize]</c> and the
/// only list of them: the identity service makes its catalogue from these constants, so an endpoint can only ask for a permission that can be granted.
/// </summary>
public static class EdxEndpointPermissions
{
    public static class Identity
    {
        private const string Service = "endpoint.identity";

        public static class Users
        {
            public const string Get = $"{Service}.users.get";
            public const string PagedList = $"{Service}.users.paged-list";
        }

        public static class Roles
        {
            public const string FilterList = $"{Service}.roles.filter-list";
        }

        public static class Tenants
        {
            public const string FilterList = $"{Service}.tenants.filter-list";
        }

        public static class Permissions
        {
            public const string FilterList = $"{Service}.permissions.filter-list";
        }

        public static class RolePermissions
        {
            public const string Read = $"{Service}.role-permissions.read";
            public const string Update = $"{Service}.role-permissions.update";
        }

        public static class UserPermissions
        {
            public const string Read = $"{Service}.user-permissions.read";
            public const string Update = $"{Service}.user-permissions.update";
        }
    }

    public static class Intake
    {
        private const string Service = "endpoint.intake";

        public static class Archives
        {
            public const string UploadXml = $"{Service}.archives.upload-xml";
            public const string UploadTabular = $"{Service}.archives.upload-tabular";
        }

        public static class Models
        {
            public const string SubmitInvoice = $"{Service}.models.submit-invoice";
        }

        public static class Files
        {
            public const string Get = $"{Service}.files.get";
            public const string PagedList = $"{Service}.files.paged-list";
            public const string Download = $"{Service}.files.download";
        }
    }

    public static class Invoice
    {
        private const string Service = "endpoint.invoice";

        public static class ReceivedFiles
        {
            public const string Get = $"{Service}.received-files.get";
            public const string PagedList = $"{Service}.received-files.paged-list";
        }
    }
}
