using Edx.Integration.FirmInvoice.Application.Contracts.FirmInvoiceDomain.Dtos;
using Edx.Integration.FirmInvoice.Application.Contracts.FirmInvoiceDomain.Dtos.Submits;
using Edx.Integration.FirmInvoice.Application.Contracts.FirmInvoiceDomain.Services;
using Edx.Integration.FirmInvoice.Http.Host.Controllers.Base;
using Edx.Integration.FirmInvoice.Http.Host.Controllers.Forms;
using Edx.Shared.Contracts.Invoicing;
using Edx.Shared.Helper;
using HsnSoft.Base.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edx.Integration.FirmInvoice.Http.Host.Controllers;

/// <summary>
/// What the pages of the back office send: the upload page (a zip) and the create page (a model). They take the same path as what a customer sends and are queued by the same service; the route is what tells them apart, so the channel of the invoice is
/// <see cref="InvoiceChannelType.BackOfficeUploadPage"/> or <see cref="InvoiceChannelType.BackOfficeCreatePage"/>, and each has a permission of its own.
/// </summary>
[Route("api/firm-invoice-service/v1/back-office")]
public sealed class BackOfficeController(IFirmInvoiceAppService firmInvoice) : BaseServiceController
{
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [PermissionAuthorize(EdxEndpointPermissions.FirmInvoice.BackOfficeUpload)]
    [DisableRequestSizeLimit]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> UploadAsync([FromForm] InvoiceUploadForm form, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(form);

        await using Stream content = form.File!.OpenReadStream();
        QueueItemDto queued = await firmInvoice.QueueFileAsync(form.ToDto(), content, InvoiceChannelType.BackOfficeUploadPage, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, queued);
    }

    [HttpPost("create")]
    [PermissionAuthorize(EdxEndpointPermissions.FirmInvoice.BackOfficeCreate)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAsync([FromBody] InvoiceSubmissionDto input, CancellationToken cancellationToken = default)
    {
        QueueItemDto queued = await firmInvoice.QueueModelAsync(input, ModelFormat.Json, InvoiceChannelType.BackOfficeCreatePage, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, queued);
    }
}
