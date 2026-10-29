using System.ComponentModel.DataAnnotations;
using HsnSoft.Base.Application.Dtos;

namespace Edx.InvoiceService.Application.Contracts.InvoiceDomain.Dtos.Filters;

public sealed class GetSalesInvoicesPaged : PagedDataRequestDto
{
    /// <summary>The records of one invoice (its uuid), the failed ones too.</summary>
    public Guid? InvoiceId { get; set; }

    /// <summary>A part of the number.</summary>
    [StringLength(64)]
    public string? Number { get; set; }

    /// <summary>A part of the name of the supplier or of the customer.</summary>
    [StringLength(200)]
    public string? PartyName { get; set; }

    /// <summary><c>Processing</c>, <c>Failed</c>, <c>DraftCreated</c>, <c>Rejected</c>, <c>Approved</c>, <c>Signed</c>, <c>Sent</c>, <c>Delivered</c> or <c>DeliveryFailed</c>; all when it is not given.</summary>
    [StringLength(16)]
    public string? Status { get; set; }

    /// <summary><c>None</c>, <c>ToSend</c>, <c>Sent</c>, <c>Delivered</c>, <c>Refused</c> or <c>Failed</c>; all when it is not given.</summary>
    [StringLength(16)]
    public string? EnvelopeState { get; set; }
}
