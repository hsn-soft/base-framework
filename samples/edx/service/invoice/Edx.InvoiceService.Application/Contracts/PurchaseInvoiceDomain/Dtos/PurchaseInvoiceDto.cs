using Edx.InvoiceService.Application.Contracts.InvoiceDomain.Dtos;
using HsnSoft.Base.Application.Dtos;

namespace Edx.InvoiceService.Application.Contracts.PurchaseInvoiceDomain.Dtos;

/// <summary>
/// An invoice that was issued to the tenant, as the caller may see it. What the caller was not granted the operation to see is left out: the amounts (<c>operation.invoice.invoice.amounts.view</c>), which are then <see langword="null"/>.
/// </summary>
public sealed class PurchaseInvoiceDto : AuditedEntityDto<Guid>
{
    /// <summary>The uuid of the invoice (its ETTN).</summary>
    public Guid InvoiceUuid { get; init; }

    /// <summary>The uuid of the envelope it came in.</summary>
    public Guid EnvelopeId { get; init; }

    public string Number { get; init; } = string.Empty;

    public DateTime IssueDate { get; init; }

    public string Currency { get; init; } = string.Empty;

    /// <summary><c>TEMELFATURA</c> or <c>TICARIFATURA</c> (this tenant may answer it).</summary>
    public string Profile { get; init; } = string.Empty;

    public string SupplierRegisterNumber { get; init; } = string.Empty;

    public string SupplierName { get; init; } = string.Empty;

    public string CustomerRegisterNumber { get; init; } = string.Empty;

    public string CustomerName { get; init; } = string.Empty;

    public int LineCount { get; init; }

    public decimal? TaxExclusiveAmount { get; init; }

    public decimal? TaxInclusiveAmount { get; init; }

    public decimal? PayableAmount { get; init; }

    public DateTimeOffset ReceivedAt { get; init; }

    /// <summary>What the center says of the envelope the invoice came in (1200, 1220, 1300...). The envelope itself is the GİB invoice integration's: ask it with <see cref="EnvelopeId"/>.</summary>
    public int EnvelopeStatusCode { get; init; }

    public string EnvelopeStatusText { get; init; } = string.Empty;

    public DateTimeOffset EnvelopeStatusAt { get; init; }

    public IReadOnlyList<InvoiceHistoryDto> History { get; init; } = [];

    public IReadOnlyList<InvoiceResponseDto> Responses { get; init; } = [];

    public Guid TenantId { get; init; }
}
