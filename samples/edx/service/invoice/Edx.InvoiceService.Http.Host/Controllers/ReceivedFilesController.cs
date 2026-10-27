using Edx.InvoiceService.Application.Contracts.ReceivedFileDomain.Dtos;
using Edx.InvoiceService.Application.Contracts.ReceivedFileDomain.Dtos.Filters;
using Edx.InvoiceService.Application.Contracts.ReceivedFileDomain.Services;
using Edx.InvoiceService.Http.Host.Controllers.Base;
using Edx.Shared.Helper;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edx.InvoiceService.Http.Host.Controllers;

/// <summary>What the invoice service knows of the files that were received: every action only calls the application service, which holds the rules. A customer sees only its own tenant's.</summary>
[Route("api/invoice-service/v1/commercial/received-files")]
public sealed class ReceivedFilesController(IReceivedFileAppService receivedFiles) : BaseServiceController
{
    [PermissionAuthorize(EdxEndpointPermissions.Invoice.ReceivedFiles.Get)]
    [HttpPost("get/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<ReceivedFileDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => receivedFiles.GetAsync(id, cancellationToken);

    [PermissionAuthorize(EdxEndpointPermissions.Invoice.ReceivedFiles.PagedList)]
    [HttpPost("paged-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<PagedDataResultDto<ReceivedFileDto>> GetPagedListAsync([FromBody] GetReceivedFilesPaged pagedInput, CancellationToken cancellationToken = default)
        => receivedFiles.GetPagedListAsync(pagedInput, cancellationToken);
}
