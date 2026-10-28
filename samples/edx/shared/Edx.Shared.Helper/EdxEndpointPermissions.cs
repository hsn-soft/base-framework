namespace Edx.Shared.Helper;

/// <summary>
/// The permissions of the endpoints: one for each action of a controller (and of a gRPC or WCF operation), named <c>endpoint.{resource}.{action}</c>. They are the argument of <c>[PermissionAuthorize]</c> and the only list of them: the identity service makes its
/// catalogue from these constants, so an endpoint can only ask for a permission that can be granted. A page asks for the ones it reads with (<c>read</c>, the menu calls them required) and for the ones of its buttons (<c>create</c>, <c>delete</c>, the menu calls
/// them actions).
/// </summary>
public static class EdxEndpointPermissions
{
    public static class Identity
    {
        public static class Users
        {
            public const string Read = "endpoint.users.read";
            public const string Create = "endpoint.users.create";
            public const string Update = "endpoint.users.update";
            public const string Delete = "endpoint.users.delete";
        }

        public static class Roles
        {
            public const string Read = "endpoint.roles.read";
            public const string Create = "endpoint.roles.create";
            public const string Delete = "endpoint.roles.delete";
        }

        public static class Tenants
        {
            public const string Read = "endpoint.tenants.read";
        }

        public static class Permissions
        {
            public const string Read = "endpoint.permissions.read";
        }

        public static class RolePermissions
        {
            public const string Read = "endpoint.role-permissions.read";
            public const string Update = "endpoint.role-permissions.update";
        }
    }

    public static class Intake
    {
        /// <summary>Send a zip with an invoice file to the API (REST, gRPC, WCF).</summary>
        public const string Upload = "endpoint.intake.upload";

        /// <summary>Send the model of an invoice to the API.</summary>
        public const string SubmitModel = "endpoint.intake.submit-model";

        /// <summary>The upload page of the back office.</summary>
        public const string BackOfficeUpload = "endpoint.intake-back-office.upload";

        /// <summary>The create page of the back office.</summary>
        public const string BackOfficeCreate = "endpoint.intake-back-office.create";
    }

    public static class Queue
    {
        public const string Read = "endpoint.queue.read";
        public const string Download = "endpoint.queue.download";
    }

    public static class Invoice
    {
        public static class Invoices
        {
            public const string Read = "endpoint.invoice.read";
            public const string Approve = "endpoint.invoice.approve";
            public const string Reject = "endpoint.invoice.reject";

            /// <summary>Read the XML of an invoice (its draft, or the signed one): it holds what the operations hide, so it needs those operations too.</summary>
            public const string Xml = "endpoint.invoice.xml";
        }

        public static class InvoiceReports
        {
            public const string Read = "endpoint.invoice-report.read";
            public const string Generate = "endpoint.invoice-report.generate";
            public const string Delete = "endpoint.invoice-report.delete";
        }
    }
}
