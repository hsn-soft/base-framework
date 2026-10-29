using System.ComponentModel.DataAnnotations;
using HsnSoft.Base.Application.Dtos;

namespace Edx.Integration.GibInvoice.Application.Contracts.EnvelopeDomain.Dtos.Filters;

public sealed class GetOutboundEnvelopesPaged : PagedDataRequestDto
{
    /// <summary><c>ToSend</c>, <c>Sent</c>, <c>Delivered</c>, <c>Refused</c> or <c>Failed</c>; all when it is not given.</summary>
    [StringLength(16)]
    public string? State { get; set; }

    /// <summary><c>Invoice</c> or <c>SystemResponse</c>; all when it is not given.</summary>
    [StringLength(16)]
    public string? Kind { get; set; }

    /// <summary>A part of the number of the invoice.</summary>
    [StringLength(64)]
    public string? InvoiceNumber { get; set; }
}

public sealed class GetInboundEnvelopesPaged : PagedDataRequestDto
{
    /// <summary><c>Queued</c>, <c>Accepted</c>, <c>Refused</c>, <c>Applied</c> or <c>Ignored</c>; all when it is not given.</summary>
    [StringLength(16)]
    public string? State { get; set; }

    /// <summary><c>SENDERENVELOPE</c> or <c>SYSTEMENVELOPE</c>; all when it is not given.</summary>
    [StringLength(32)]
    public string? DocumentType { get; set; }
}
