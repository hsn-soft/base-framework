using System.Globalization;

namespace Edx.InvoiceService.Application.Storage;

/// <summary>Where the invoice service keeps the documents of a tenant in its container: everything of a tenant is under <c>{tenant}/</c> (the ids are uuids without dashes, the month has two digits). See ADR-0096.</summary>
internal static class InvoicePaths
{
    /// <summary>The unsigned answer to a purchase invoice, until it is signed.</summary>
    public static string PurchaseAnswerDraft(Guid tenantId, Guid invoiceUuid) => $"{tenantId:N}/purchase/purchase_answer_draft_{invoiceUuid:N}.xml";

    /// <summary>The signed answer to a purchase invoice, in the folder of the month of the invoice, beside the invoice.</summary>
    public static string PurchaseAnswer(Guid tenantId, DateTime invoiceDate, Guid invoiceUuid) => $"{tenantId:N}/purchase/{Month(invoiceDate)}/purchase_answer_{invoiceUuid:N}.xml";

    /// <summary>The answer the receiver gave to a sales invoice, in the folder of the month of the invoice, beside the invoice.</summary>
    public static string SalesAnswer(Guid tenantId, DateTime invoiceDate, Guid invoiceUuid) => $"{tenantId:N}/sales/{Month(invoiceDate)}/sales_answer_{invoiceUuid:N}.xml";

    private static string Month(DateTime date) => $"{date.ToString("yyyy", CultureInfo.InvariantCulture)}/{date.ToString("MM", CultureInfo.InvariantCulture)}";
}
