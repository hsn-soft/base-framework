using Edx.Integration.GibInvoice.Application.Contracts.EnvelopeDomain.Dtos;
using Edx.Integration.GibInvoice.Application.Contracts.EnvelopeDomain.Dtos.Filters;
using HsnSoft.Base.Application.Dtos;

namespace Edx.Integration.GibInvoice.Application.Contracts.EnvelopeDomain.Services;

/// <summary>The envelopes of a tenant, in both directions: what the service handed to the center for it and what the center delivered to its post box. A caller sees its own tenant's only.</summary>
public interface IEnvelopeAppService
{
    Task<OutboundEnvelopeDto> GetOutboundAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>The envelope this tenant sent, by its uuid (what an invoice says of its envelope).</summary>
    Task<OutboundEnvelopeDto> GetOutboundByEnvelopeAsync(Guid envelopeId, CancellationToken cancellationToken = default);

    Task<PagedDataResultDto<OutboundEnvelopeDto>> GetOutboundPagedListAsync(GetOutboundEnvelopesPaged pagedInput, CancellationToken cancellationToken = default);

    Task<InboundEnvelopeDto> GetInboundAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>The envelope that came in for this tenant, by its uuid (what a purchase invoice says of its envelope).</summary>
    Task<InboundEnvelopeDto> GetInboundByEnvelopeAsync(Guid envelopeId, CancellationToken cancellationToken = default);

    Task<PagedDataResultDto<InboundEnvelopeDto>> GetInboundPagedListAsync(GetInboundEnvelopesPaged pagedInput, CancellationToken cancellationToken = default);
}
