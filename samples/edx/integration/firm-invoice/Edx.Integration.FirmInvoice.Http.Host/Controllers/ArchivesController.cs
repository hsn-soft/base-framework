using Edx.Integration.FirmInvoice.Application.Contracts.FirmInvoiceDomain.Dtos;
using Edx.Integration.FirmInvoice.Application.Contracts.FirmInvoiceDomain.Services;
using Edx.Integration.FirmInvoice.Http.Host.Controllers.Base;
using Edx.Integration.FirmInvoice.Http.Host.Controllers.Forms;
using Edx.Shared.Contracts.Invoicing;
using Edx.Shared.Helper;
using HsnSoft.Base.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edx.Integration.FirmInvoice.Http.Host.Controllers;

/// <summary>The zips a customer sends over REST: a form with the zip (<c>file</c>), the uuid and the number of the invoice, the SHA-256 of the zip and the type of the file in it. The answer is that it was queued (or why it was not); the queue says what became of it.</summary>
[Route("api/firm-invoice-service/v1/commercial/archives")]
public sealed class ArchivesController(IFirmInvoiceAppService firmInvoice) : BaseServiceController
{
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [PermissionAuthorize(EdxEndpointPermissions.FirmInvoice.Upload)]
    [DisableRequestSizeLimit]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> UploadAsync([FromForm] InvoiceUploadForm form, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(form);

        await using Stream content = form.File!.OpenReadStream();
        QueueItemDto queued = await firmInvoice.QueueFileAsync(form.ToDto(), content, InvoiceChannelType.ServiceUploadFile, cancellationToken);
        return CreatedAtAction(nameof(QueueController.GetAsync), "Queue", new { id = queued.Id }, queued);
    }
}
