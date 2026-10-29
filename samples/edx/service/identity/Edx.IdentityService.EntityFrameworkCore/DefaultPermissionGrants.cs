using Edx.Shared.Helper;

namespace Edx.IdentityService.EntityFrameworkCore;

/// <summary>
/// What the four roles of every tenant of the sample are granted when they are made (<c>SampleIdentity</c>) or when a permission is first added (<c>PermissionDatabase.EnsurePermissionsAsync</c>): a starting point, not a rule. After it every grant is the identity service's data and is
/// changed through its endpoints (a tenant's own administrator changes the grants of its roles). The same four in every tenant, the system tenant's too:
/// the <c>admin</c> does all of it up to the largest values; the <c>manager</c> sees, reads the queue, approves or rejects drafts (up to 50 000) and sees the people; the <c>operator</c> makes invoices (the create page, the upload page, the API; up to 1 000) and
/// follows them in the queue; the <c>user</c> only reads (invoices up to 100 000, the queue, the reports). A constraint a role is not given is a thing the role may not do at all.
/// </summary>
public static class DefaultPermissionGrants
{
    private const string Largest = "2147483647";
    private const string Megabyte100 = "104857600";
    private const string Megabyte20 = "20971520";

    private static readonly string[] s_identityReads =
    [
        EdxEndpointPermissions.Identity.Users.Read,
        EdxEndpointPermissions.Identity.Roles.Read,
        EdxEndpointPermissions.Identity.Tenants.Read,
        EdxEndpointPermissions.Identity.Permissions.Read,
        EdxEndpointPermissions.Identity.RolePermissions.Read,
    ];

    private static readonly string[] s_identityWrites =
    [
        EdxEndpointPermissions.Identity.Users.Create,
        EdxEndpointPermissions.Identity.Users.Update,
        EdxEndpointPermissions.Identity.Users.Delete,
        EdxEndpointPermissions.Identity.Roles.Create,
        EdxEndpointPermissions.Identity.Roles.Delete,
        EdxEndpointPermissions.Identity.RolePermissions.Update,
    ];

    private static readonly string[] s_makeInvoices =
    [
        EdxEndpointPermissions.FirmInvoice.Upload,
        EdxEndpointPermissions.FirmInvoice.SubmitModel,
        EdxEndpointPermissions.FirmInvoice.BackOfficeUpload,
        EdxEndpointPermissions.FirmInvoice.BackOfficeCreate,
    ];

    private static readonly string[] s_queue = [EdxEndpointPermissions.Queue.Read, EdxEndpointPermissions.Queue.Download];

    private static readonly string[] s_reads = [EdxEndpointPermissions.Invoice.Invoices.Read, EdxEndpointPermissions.Invoice.InvoiceReports.Read];

    private static readonly string[] s_approval = [EdxEndpointPermissions.Invoice.Invoices.Approve, EdxEndpointPermissions.Invoice.Invoices.Reject, EdxEndpointPermissions.Invoice.Invoices.Xml];

    private static readonly string[] s_purchases = [EdxEndpointPermissions.Invoice.PurchaseInvoices.Read];

    private static readonly string[] s_purchaseXml = [EdxEndpointPermissions.Invoice.PurchaseInvoices.Xml];

    private static readonly string[] s_purchaseRespond = [EdxEndpointPermissions.Invoice.PurchaseInvoices.Respond];

    private static readonly string[] s_integration = [EdxEndpointPermissions.Integration.Envelopes.Read];

    private static readonly string[] s_reports = [EdxEndpointPermissions.Invoice.InvoiceReports.Generate, EdxEndpointPermissions.Invoice.InvoiceReports.Delete];

    /// <summary>The grants of a role of a tenant (by the code of the tenant and the name of the role), each as the code and its value (a constraint's; none for the others); nothing for a role that is not named here.</summary>
    public static IReadOnlyList<(string Code, string? Value)> For(string tenantCode, string roleName)
        => roleName switch
        {
            EdxRoles.Admin => Make(
                [Limit(EdxConstraintPermissions.FirmInvoice.UploadMaxBytes, Megabyte100), Limit(EdxConstraintPermissions.Invoice.MaxViewAmount, Largest), Limit(EdxConstraintPermissions.Invoice.MaxCreateAmount, "999999999"), Limit(EdxConstraintPermissions.Invoice.MaxApproveAmount, "999999999"), Limit(EdxConstraintPermissions.InvoiceReport.MaxDays, "3650")],
                [
                    EdxPagePermissions.Invoice.View, EdxPagePermissions.InvoiceDraft.View, EdxPagePermissions.InvoiceCreate.View, EdxPagePermissions.InvoiceUpload.View, EdxPagePermissions.Queue.View, EdxPagePermissions.InvoiceReport.View, EdxPagePermissions.Users.View, EdxPagePermissions.Roles.View,
                    EdxPagePermissions.PurchaseInvoice.View, EdxPagePermissions.IntegrationEnvelope.View,
                ],
                s_identityReads, s_identityWrites, s_makeInvoices, s_queue, s_reads, s_approval, s_reports, s_purchases, s_purchaseXml, s_purchaseRespond, s_integration,
                [EdxOperationPermissions.Identity.UserEmailView, EdxOperationPermissions.FirmInvoice.FileStorageLocationView, EdxOperationPermissions.Invoice.PriceView, EdxOperationPermissions.Invoice.TaxIdView, EdxOperationPermissions.InvoiceReport.TotalView]),
            EdxRoles.Manager => Make(
                [Limit(EdxConstraintPermissions.Invoice.MaxViewAmount, "100000"), Limit(EdxConstraintPermissions.Invoice.MaxApproveAmount, "50000"), Limit(EdxConstraintPermissions.InvoiceReport.MaxDays, "90")],
                [EdxPagePermissions.Invoice.View, EdxPagePermissions.InvoiceDraft.View, EdxPagePermissions.Queue.View, EdxPagePermissions.InvoiceReport.View, EdxPagePermissions.Users.View, EdxPagePermissions.PurchaseInvoice.View, EdxPagePermissions.IntegrationEnvelope.View],
                s_queue, s_reads, s_approval, s_purchases, s_purchaseXml, s_purchaseRespond, s_integration, [EdxEndpointPermissions.Identity.Users.Read, EdxEndpointPermissions.Identity.Roles.Read],
                [EdxOperationPermissions.Identity.UserEmailView, EdxOperationPermissions.FirmInvoice.FileStorageLocationView, EdxOperationPermissions.Invoice.PriceView, EdxOperationPermissions.Invoice.TaxIdView, EdxOperationPermissions.InvoiceReport.TotalView]),
            EdxRoles.Operator => Make(
                [Limit(EdxConstraintPermissions.FirmInvoice.UploadMaxBytes, Megabyte20), Limit(EdxConstraintPermissions.Invoice.MaxViewAmount, "1000"), Limit(EdxConstraintPermissions.Invoice.MaxCreateAmount, "1000"), Limit(EdxConstraintPermissions.InvoiceReport.MaxDays, "30")],
                [EdxPagePermissions.Invoice.View, EdxPagePermissions.InvoiceCreate.View, EdxPagePermissions.InvoiceUpload.View, EdxPagePermissions.Queue.View, EdxPagePermissions.InvoiceReport.View, EdxPagePermissions.PurchaseInvoice.View],
                s_makeInvoices, s_queue, s_reads, s_purchases,
                [EdxOperationPermissions.FirmInvoice.FileStorageLocationView, EdxOperationPermissions.Invoice.PriceView]),
            EdxRoles.User => Make(
                [Limit(EdxConstraintPermissions.Invoice.MaxViewAmount, "100000"), Limit(EdxConstraintPermissions.InvoiceReport.MaxDays, "30")],
                [EdxPagePermissions.Invoice.View, EdxPagePermissions.Queue.View, EdxPagePermissions.InvoiceReport.View, EdxPagePermissions.PurchaseInvoice.View],
                [EdxEndpointPermissions.Queue.Read], s_reads, s_purchases,
                [EdxOperationPermissions.Invoice.PriceView]),
            _ => [],
        };

    private static (string Code, string? Value) Limit(string code, string value) => (code, value);

    // the codes of the groups, and the limits (constraints, with their values)
    private static List<(string Code, string? Value)> Make((string Code, string? Value)[] limits, params string[][] groups)
    {
        List<(string Code, string? Value)> grants = [.. groups.SelectMany(group => group).Distinct().Select(code => (code, (string?)null))];
        grants.AddRange(limits);
        return grants;
    }
}
