namespace Edx.Shared.Helper;

/// <summary>
/// The permissions that limit a value: <c>constraint.{resource}.{limit}</c>. A grant carries the value (a number of bytes, an amount, a number of days) the role is held to, and a caller that was given none is refused (403) before the business is done: a constraint
/// is the most a role may do, and no constraint is not "no limit" but "not allowed". The code that is limited asks the <c>IPermissionChecker</c> (<c>RequireDecimalAsync</c>, ...).
/// </summary>
public static class EdxConstraintPermissions
{
    public static class FirmInvoice
    {
        /// <summary>The most bytes one upload of the caller may weigh; never more than <c>FirmInvoice:MaxUploadBytes</c>.</summary>
        public const string UploadMaxBytes = "constraint.firm-invoice.upload.max-bytes";
    }

    public static class Invoice
    {
        /// <summary>The most an invoice may amount to for the caller to see it: a list holds the invoices up to it, and an invoice above it is not found.</summary>
        public const string MaxViewAmount = "constraint.invoice.max-view-amount";

        /// <summary>The most an invoice may amount to for the caller to send it: checked when the invoice is imported, as the sender.</summary>
        public const string MaxCreateAmount = "constraint.invoice.max-create-amount";

        /// <summary>The most an invoice may amount to for the caller to approve it.</summary>
        public const string MaxApproveAmount = "constraint.invoice.max-approve-amount";
    }

    public static class InvoiceReport
    {
        /// <summary>How many days back the caller may look: it sees the reports of the last days only, and makes a report of a period no longer than that.</summary>
        public const string MaxDays = "constraint.invoice-report.max-days";
    }
}
