namespace Edx.Shared.Helper;

/// <summary>
/// The permissions of what happens inside an endpoint, which the endpoint's own permission does not say: <c>operation.{resource}.{field or step}.{what}</c> (who may see the price of an invoice, where a file is kept). The code that does it asks the
/// <c>IPermissionChecker</c>, and what the caller may not see is masked before it leaves the service.
/// </summary>
public static class EdxOperationPermissions
{
    public static class Identity
    {
        public const string UserEmailView = "operation.users.email.view";
    }

    public static class FirmInvoice
    {
        /// <summary>See where the file of a queue item is kept (the container, the object name and the folder), not only that it was received.</summary>
        public const string FileStorageLocationView = "operation.firm-invoice.file.storage-location.view";
    }

    public static class Invoice
    {
        /// <summary>See the prices of the lines of an invoice and its total (hidden from whoever was not granted it).</summary>
        public const string PriceView = "operation.invoice.price.view";

        /// <summary>See the tax ids of the supplier and of the customer of an invoice (left out for whoever was not granted it).</summary>
        public const string TaxIdView = "operation.invoice.tax-id.view";
    }

    public static class InvoiceReport
    {
        /// <summary>See what a report amounts to (hidden from whoever was not granted it).</summary>
        public const string TotalView = "operation.invoice-report.total.view";
    }
}
