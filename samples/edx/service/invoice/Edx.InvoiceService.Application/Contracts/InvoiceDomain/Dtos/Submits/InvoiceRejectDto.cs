using System.ComponentModel.DataAnnotations;

namespace Edx.InvoiceService.Application.Contracts.InvoiceDomain.Dtos.Submits;

public sealed class InvoiceRejectDto
{
    /// <summary>Why the draft is rejected: what the sender reads in the queue.</summary>
    [Required]
    [StringLength(2000)]
    public string Reason { get; set; } = string.Empty;
}
