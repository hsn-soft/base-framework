using Edx.Integration.FirmInvoice.Application.Contracts.FirmInvoiceDomain.Dtos;
using Edx.Integration.FirmInvoice.Application.Contracts.FirmInvoiceDomain.Dtos.Filters;
using Edx.Integration.FirmInvoice.Application.Contracts.FirmInvoiceDomain.Services;
using Edx.Integration.FirmInvoice.Http.Host.Controllers.Base;
using Edx.Shared.Helper;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edx.Integration.FirmInvoice.Http.Host.Controllers;

/// <summary>The queue: what was sent and what became of it. A sender asks by the id of the queue item it was given, or by the uuid of the invoice; the back office lists it. A caller sees only its own tenant's.</summary>
[Route("api/firm-invoice-service/v1/commercial/queue")]
public sealed class QueueController(IFirmInvoiceAppService firmInvoice) : BaseServiceController
{
    [HttpPost("get/{id:guid}")]
    [PermissionAuthorize(EdxEndpointPermissions.Queue.Read)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<QueueItemDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => firmInvoice.GetAsync(id, cancellationToken);

    /// <summary>What is in the queue of an invoice: the item that got through when there is one, otherwise all of them (the ones that failed or were rejected), the newest first.</summary>
    [HttpPost("by-invoice/{invoiceId:guid}")]
    [PermissionAuthorize(EdxEndpointPermissions.Queue.Read)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<List<QueueItemDto>> GetByInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken = default)
        => firmInvoice.GetByInvoiceAsync(invoiceId, cancellationToken);

    [HttpPost("paged-list")]
    [PermissionAuthorize(EdxEndpointPermissions.Queue.Read)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<PagedDataResultDto<QueueItemDto>> GetPagedListAsync([FromBody] GetQueueItemsPaged pagedInput, CancellationToken cancellationToken = default)
        => firmInvoice.GetPagedListAsync(pagedInput, cancellationToken);

    /// <summary>The file as it was received.</summary>
    [HttpGet("{id:guid}/download")]
    [Produces("application/octet-stream")]
    [PermissionAuthorize(EdxEndpointPermissions.Queue.Download)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DownloadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        StoredFileContent file = await firmInvoice.OpenAsync(id, cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }
}
