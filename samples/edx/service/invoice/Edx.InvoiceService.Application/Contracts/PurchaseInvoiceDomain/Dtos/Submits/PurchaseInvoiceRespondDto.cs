using System.ComponentModel.DataAnnotations;

namespace Edx.InvoiceService.Application.Contracts.PurchaseInvoiceDomain.Dtos.Submits;

/// <summary>The answer of the tenant to a commercial invoice that was issued to it.</summary>
public sealed class PurchaseInvoiceRespondDto
{
    /// <summary><see langword="true"/> accepts the invoice, <see langword="false"/> refuses it (the reason is required then).</summary>
    public bool Accept { get; set; }

    /// <summary>What is said to the supplier; required when the invoice is refused.</summary>
    [StringLength(500)]
    public string? Note { get; set; }
}
