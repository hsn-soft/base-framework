using Edx.Integration.GibInvoice.Application.Contracts.EnvelopeDomain.Dtos;
using Edx.Integration.GibInvoice.Application.Contracts.EnvelopeDomain.Dtos.Filters;
using Edx.Integration.GibInvoice.Application.Contracts.EnvelopeDomain.Services;
using Edx.Integration.GibInvoice.Http.Host.Controllers.Base;
using Edx.Shared.Helper;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edx.Integration.GibInvoice.Http.Host.Controllers;

/// <summary>The envelopes of the tenant, in both directions: what was handed to the center for it (invoices, and the answers of its post box) and what the center delivered to its post box (invoices for it, system responses about its envelopes). A caller sees its own tenant's.</summary>
[Route("api/integration-service/v1/commercial/envelopes")]
public sealed class EnvelopesController(IEnvelopeAppService envelopes) : BaseServiceController
{
    [HttpPost("outbound/get/{id:guid}")]
    [PermissionAuthorize(EdxEndpointPermissions.Integration.Envelopes.Read)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<OutboundEnvelopeDto> GetOutboundAsync(Guid id, CancellationToken cancellationToken = default)
        => envelopes.GetOutboundAsync(id, cancellationToken);

    [HttpPost("outbound/by-envelope/{envelopeId:guid}")]
    [PermissionAuthorize(EdxEndpointPermissions.Integration.Envelopes.Read)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<OutboundEnvelopeDto> GetOutboundByEnvelopeAsync(Guid envelopeId, CancellationToken cancellationToken = default)
        => envelopes.GetOutboundByEnvelopeAsync(envelopeId, cancellationToken);

    [HttpPost("outbound/paged-list")]
    [PermissionAuthorize(EdxEndpointPermissions.Integration.Envelopes.Read)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<PagedDataResultDto<OutboundEnvelopeDto>> GetOutboundPagedListAsync([FromBody] GetOutboundEnvelopesPaged pagedInput, CancellationToken cancellationToken = default)
        => envelopes.GetOutboundPagedListAsync(pagedInput, cancellationToken);

    [HttpPost("inbound/get/{id:guid}")]
    [PermissionAuthorize(EdxEndpointPermissions.Integration.Envelopes.Read)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<InboundEnvelopeDto> GetInboundAsync(Guid id, CancellationToken cancellationToken = default)
        => envelopes.GetInboundAsync(id, cancellationToken);

    [HttpPost("inbound/by-envelope/{envelopeId:guid}")]
    [PermissionAuthorize(EdxEndpointPermissions.Integration.Envelopes.Read)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<InboundEnvelopeDto> GetInboundByEnvelopeAsync(Guid envelopeId, CancellationToken cancellationToken = default)
        => envelopes.GetInboundByEnvelopeAsync(envelopeId, cancellationToken);

    [HttpPost("inbound/paged-list")]
    [PermissionAuthorize(EdxEndpointPermissions.Integration.Envelopes.Read)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<PagedDataResultDto<InboundEnvelopeDto>> GetInboundPagedListAsync([FromBody] GetInboundEnvelopesPaged pagedInput, CancellationToken cancellationToken = default)
        => envelopes.GetInboundPagedListAsync(pagedInput, cancellationToken);
}
