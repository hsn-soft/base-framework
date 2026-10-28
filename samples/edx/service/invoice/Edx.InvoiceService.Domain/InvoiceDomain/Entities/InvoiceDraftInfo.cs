namespace Edx.InvoiceService.Domain.InvoiceDomain.Entities;

/// <summary>What the UBL-TR invoice of a draft says of itself, in the words of the domain: the header, the lines and what the invoice amounts to (the payable amount of the document).</summary>
public sealed record InvoiceDraftInfo(
    DateTime IssueDate,
    string Currency,
    string SupplierName,
    string SupplierRegisterNumber,
    string CustomerName,
    string CustomerRegisterNumber,
    IReadOnlyList<InvoiceDraftLine> Lines,
    decimal Total,
    string Profile = "TEMELFATURA");

/// <summary>A line of a draft: its total is its amount with its tax, as the document rounded them.</summary>
public sealed record InvoiceDraftLine(string Description, decimal Quantity, decimal UnitPrice, decimal VatRate, decimal Total);
