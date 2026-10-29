namespace Edx.Shared.Helper;

/// <summary>
/// The permissions that let a user into a page of a back office: <c>page.{page}.view</c>. They are the visibility permission of a menu item: a page without it is not in the menu of the user and its address leads to the forbidden page. They concern the
/// front end only; what the page reads or does is the endpoint permissions' (and the operations' and constraints') to decide.
/// </summary>
public static class EdxPagePermissions
{
    public static class Invoice
    {
        public const string View = "page.invoice.view";
    }

    public static class InvoiceDraft
    {
        public const string View = "page.invoice-draft.view";
    }

    public static class InvoiceCreate
    {
        public const string View = "page.invoice-create.view";
    }

    public static class InvoiceUpload
    {
        public const string View = "page.invoice-upload.view";
    }

    public static class Queue
    {
        public const string View = "page.queue.view";
    }

    public static class PurchaseInvoice
    {
        public const string View = "page.purchase-invoice.view";
    }

    public static class IntegrationEnvelope
    {
        public const string View = "page.integration-envelope.view";
    }

    public static class InvoiceReport
    {
        public const string View = "page.invoice-report.view";
    }

    public static class Users
    {
        public const string View = "page.users.view";
    }

    public static class Roles
    {
        public const string View = "page.roles.view";
    }
}
