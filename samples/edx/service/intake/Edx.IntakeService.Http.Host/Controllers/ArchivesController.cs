using Edx.IntakeService.Application.Contracts.IntakeDomain.Dtos;
using Edx.IntakeService.Application.Contracts.IntakeDomain.Services;
using Edx.IntakeService.Http.Host.Controllers.Base;
using Edx.IntakeService.Http.Host.Controllers.Forms;
using Edx.Shared.Contracts.Invoicing;
using Edx.Shared.Helper;
using HsnSoft.Base.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edx.IntakeService.Http.Host.Controllers;

/// <summary>The zips a customer sends over REST: a form with the zip (<c>file</c>), the uuid and the number of the invoice, the SHA-256 of the zip and the type of the file in it. The answer is that it was queued (or why it was not); the queue says what became of it.</summary>
[Route("api/intake-service/v1/commercial/archives")]
public sealed class ArchivesController(IIntakeAppService intake) : BaseServiceController
{
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [PermissionAuthorize(EdxEndpointPermissions.Intake.Upload)]
    [DisableRequestSizeLimit]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> UploadAsync([FromForm] InvoiceUploadForm form, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(form);

        await using Stream content = form.File!.OpenReadStream();
        QueueItemDto queued = await intake.QueueFileAsync(form.ToDto(), content, InvoiceChannelType.ServiceUploadFile, cancellationToken);
        return CreatedAtAction(nameof(QueueController.GetAsync), "Queue", new { id = queued.Id }, queued);
    }
}
