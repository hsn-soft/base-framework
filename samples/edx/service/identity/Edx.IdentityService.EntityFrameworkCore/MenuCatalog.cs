using Edx.IdentityService.Domain.IdentityDomain.Entities;
using Edx.Shared.Helper;

namespace Edx.IdentityService.EntityFrameworkCore;

/// <summary>A permission of a menu item and what it is to the item.</summary>
public sealed record MenuPermissionDefinition(string Permission, MenuPermissionRelationType Relation);

/// <summary>An item of the menu as the code defines it. The items are kept in step with the table at every start (<c>PermissionDatabase.EnsurePermissionsAsync</c>).</summary>
public sealed record MenuDefinition(string Code, MenuNodeType NodeType, string Title, string? Route, string? Icon, int SortOrder, string? Parent, params MenuPermissionDefinition[] Permissions);

/// <summary>
/// The menu of the back office (its titles are what the people of the back office read: Turkish): what a user may go to, and for each page the permissions it needs. A leaf has its visibility permission (the page opens and is in the menu with it), what it reads with (required), its buttons (actions), what it hides field by field (operations) and
/// its limits (constraints); the front end asks for the menu of the user and gets only the items it may see, with these as granted or not.
/// </summary>
public static class MenuCatalog
{
    public const string InvoiceEntry = "invoice-entry";
    public const string InvoiceManagement = "invoice-management";
    public const string Integration = "integration";
    public const string IdentityManagement = "identity-management";

    public static IReadOnlyList<MenuDefinition> BackOffice { get; } =
    [
        new("caption.business", MenuNodeType.Caption, "İş", null, null, 10, null),
        new(InvoiceEntry, MenuNodeType.Node, "Fatura girişi", null, "upload", 20, null),
        new(
            $"{InvoiceEntry}.create", MenuNodeType.Leaf, "Fatura oluştur", "/invoice-create", "create", 10, InvoiceEntry,
            Visibility(EdxPagePermissions.InvoiceCreate.View),
            Required(EdxEndpointPermissions.FirmInvoice.BackOfficeCreate),
            Constraint(EdxConstraintPermissions.Invoice.MaxCreateAmount)),
        new(
            $"{InvoiceEntry}.upload", MenuNodeType.Leaf, "Dosya yükle", "/invoice-upload", "upload", 20, InvoiceEntry,
            Visibility(EdxPagePermissions.InvoiceUpload.View),
            Required(EdxEndpointPermissions.FirmInvoice.BackOfficeUpload),
            Constraint(EdxConstraintPermissions.FirmInvoice.UploadMaxBytes),
            Constraint(EdxConstraintPermissions.Invoice.MaxCreateAmount)),
        new(
            $"{InvoiceEntry}.queue", MenuNodeType.Leaf, "Kuyruk", "/queue", "queue", 30, InvoiceEntry,
            Visibility(EdxPagePermissions.Queue.View),
            Required(EdxEndpointPermissions.Queue.Read),
            Action(EdxEndpointPermissions.Queue.Download),
            Operation(EdxOperationPermissions.FirmInvoice.FileStorageLocationView)),
        new(InvoiceManagement, MenuNodeType.Node, "Fatura yönetimi", null, "invoice", 30, null),
        new(
            $"{InvoiceManagement}.invoices", MenuNodeType.Leaf, "Satış faturaları", "/invoices", "invoice", 10, InvoiceManagement,
            Visibility(EdxPagePermissions.Invoice.View),
            Required(EdxEndpointPermissions.Invoice.Invoices.Read),
            Action(EdxEndpointPermissions.Invoice.Invoices.Xml),
            Operation(EdxOperationPermissions.Invoice.PriceView),
            Operation(EdxOperationPermissions.Invoice.TaxIdView),
            Constraint(EdxConstraintPermissions.Invoice.MaxViewAmount)),
        new(
            $"{InvoiceManagement}.purchase", MenuNodeType.Leaf, "Alış faturaları", "/purchase-invoices", "purchase", 15, InvoiceManagement,
            Visibility(EdxPagePermissions.PurchaseInvoice.View),
            Required(EdxEndpointPermissions.Invoice.PurchaseInvoices.Read),
            Action(EdxEndpointPermissions.Invoice.PurchaseInvoices.Xml),
            Action(EdxEndpointPermissions.Invoice.PurchaseInvoices.Respond),
            Operation(EdxOperationPermissions.Invoice.PriceView),
            Operation(EdxOperationPermissions.Invoice.TaxIdView),
            Constraint(EdxConstraintPermissions.Invoice.MaxViewAmount)),
        new(
            $"{InvoiceManagement}.drafts", MenuNodeType.Leaf, "Taslak onayı", "/invoice-drafts", "draft", 20, InvoiceManagement,
            Visibility(EdxPagePermissions.InvoiceDraft.View),
            Required(EdxEndpointPermissions.Invoice.Invoices.Read),
            Action(EdxEndpointPermissions.Invoice.Invoices.Approve),
            Action(EdxEndpointPermissions.Invoice.Invoices.Reject),
            Action(EdxEndpointPermissions.Invoice.Invoices.Xml),
            Operation(EdxOperationPermissions.Invoice.PriceView),
            Operation(EdxOperationPermissions.Invoice.TaxIdView),
            Constraint(EdxConstraintPermissions.Invoice.MaxViewAmount),
            Constraint(EdxConstraintPermissions.Invoice.MaxApproveAmount)),
        new(
            $"{InvoiceManagement}.reports", MenuNodeType.Leaf, "Fatura raporları", "/invoice-reports", "report", 30, InvoiceManagement,
            Visibility(EdxPagePermissions.InvoiceReport.View),
            Required(EdxEndpointPermissions.Invoice.InvoiceReports.Read),
            Action(EdxEndpointPermissions.Invoice.InvoiceReports.Generate),
            Action(EdxEndpointPermissions.Invoice.InvoiceReports.Delete),
            Operation(EdxOperationPermissions.InvoiceReport.TotalView),
            Constraint(EdxConstraintPermissions.InvoiceReport.MaxDays)),
        new(Integration, MenuNodeType.Node, "Entegrasyon", null, "integration", 40, null),
        new(
            $"{Integration}.envelopes", MenuNodeType.Leaf, "Zarflar", "/envelopes", "envelope", 10, Integration,
            Visibility(EdxPagePermissions.IntegrationEnvelope.View),
            Required(EdxEndpointPermissions.Integration.Envelopes.Read)),
        new("caption.administration", MenuNodeType.Caption, "Yönetim", null, null, 50, null),
        new(IdentityManagement, MenuNodeType.Node, "Kimlik yönetimi", null, "identity", 60, null),
        new(
            $"{IdentityManagement}.users", MenuNodeType.Leaf, "Kullanıcılar", "/identity/users", "users", 10, IdentityManagement,
            Visibility(EdxPagePermissions.Users.View),
            Required(EdxEndpointPermissions.Identity.Users.Read),
            Required(EdxEndpointPermissions.Identity.Roles.Read),
            Action(EdxEndpointPermissions.Identity.Users.Create),
            Action(EdxEndpointPermissions.Identity.Users.Update),
            Action(EdxEndpointPermissions.Identity.Users.Delete),
            Operation(EdxOperationPermissions.Identity.UserEmailView)),
        new(
            $"{IdentityManagement}.roles", MenuNodeType.Leaf, "Roller", "/identity/roles", "roles", 20, IdentityManagement,
            Visibility(EdxPagePermissions.Roles.View),
            Required(EdxEndpointPermissions.Identity.Roles.Read),
            Required(EdxEndpointPermissions.Identity.Permissions.Read),
            Required(EdxEndpointPermissions.Identity.RolePermissions.Read),
            Action(EdxEndpointPermissions.Identity.Roles.Create),
            Action(EdxEndpointPermissions.Identity.Roles.Delete),
            Action(EdxEndpointPermissions.Identity.RolePermissions.Update)),
    ];

    private static MenuPermissionDefinition Visibility(string permission) => new(permission, MenuPermissionRelationType.Visibility);

    private static MenuPermissionDefinition Required(string permission) => new(permission, MenuPermissionRelationType.Required);

    private static MenuPermissionDefinition Action(string permission) => new(permission, MenuPermissionRelationType.Action);

    private static MenuPermissionDefinition Operation(string permission) => new(permission, MenuPermissionRelationType.Operation);

    private static MenuPermissionDefinition Constraint(string permission) => new(permission, MenuPermissionRelationType.Constraint);
}
