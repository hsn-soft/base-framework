namespace Hsx.Shared.Contracts.Permissions;

/// <summary>The permissions of the identity service: the tenants, users, roles, companies, customers, subscriptions and the grants.</summary>
public static class IdentityPermissions
{
    /// <summary>The pages of the back office of this service: <c>page.{page}.view</c>, the visibility permission of a menu item (the front end only).</summary>
    public static class Pages
    {
        public static class Tenants
        {
            /// <summary>Lets a user into the page.</summary>
            public const string View = "page.tenants.view";
        }

        public static class Users
        {
            /// <summary>Lets a user into the page.</summary>
            public const string View = "page.users.view";
        }

        public static class Roles
        {
            /// <summary>Lets a user into the page.</summary>
            public const string View = "page.roles.view";
        }

        public static class Companies
        {
            /// <summary>Lets a user into the page.</summary>
            public const string View = "page.companies.view";
        }

        public static class Customers
        {
            /// <summary>Lets a user into the page.</summary>
            public const string View = "page.customers.view";
        }

        public static class Subscriptions
        {
            /// <summary>Lets a user into the page.</summary>
            public const string View = "page.subscriptions.view";
        }
    }

    /// <summary>One permission for each action of a controller: <c>endpoint.{resource}.{action}</c>, the argument of <c>[PermissionAuthorize]</c>.</summary>
    public static class Endpoints
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

            /// <summary>Transfer a subscription to another owner.</summary>
            public const string Transfer = "endpoint.subscriptions.transfer";
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

        public static class TenantManagement
        {
            /// <summary>Create a reseller: its tenant, its administrator role and its first user.</summary>
            public const string CreateReseller = "endpoint.tenant-management.create-reseller";
        }

        public static class AuditLogs
        {
            /// <summary>Read (get and the lists).</summary>
            public const string Read = "endpoint.audit-logs.read";
        }
    }

    /// <summary>Something that happens inside an endpoint: <c>operation.{resource}.{what}.{action}</c>, asked by the code that does it (a field is open or masked).</summary>
    public static class Operations
    {
        public static class AppUsers
        {
            /// <summary>Change the email of a user.</summary>
            public const string EmailUpdate = "operation.app-users.email.update";

            /// <summary>Change the phone of a user.</summary>
            public const string PhoneUpdate = "operation.app-users.phone.update";

            /// <summary>See the phone of a user (without it the field is masked).</summary>
            public const string PhoneView = "operation.app-users.phone.view";

            /// <summary>Block or unblock a user.</summary>
            public const string IsBlockUpdate = "operation.app-users.is-block.update";
        }
    }

    /// <summary>A limit a role is given a value for: <c>constraint.{resource}.{what}</c>. A caller whose roles were not given it is refused before anything is done (framework ADR-0083).</summary>
    public static class Constraints
    {
        public static class AuditLogs
        {
            /// <summary>How many days back the audit log may be read; the value is the number of days.</summary>
            public const string MaxDays = "constraint.audit-logs.max-days";
        }
    }
}
