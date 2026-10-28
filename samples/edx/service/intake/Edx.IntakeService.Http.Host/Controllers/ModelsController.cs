using Edx.IntakeService.Application.Contracts.IntakeDomain.Dtos;
using Edx.IntakeService.Application.Contracts.IntakeDomain.Dtos.Submits;
using Edx.IntakeService.Application.Contracts.IntakeDomain.Services;
using Edx.IntakeService.Http.Host.Controllers.Base;
using Edx.Shared.Contracts.Invoicing;
using Edx.Shared.Helper;
using HsnSoft.Base.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edx.IntakeService.Http.Host.Controllers;

/// <summary>The models of the shared API a customer sends over REST: the class is the body (it holds the uuid and the number of the invoice), and the service keeps it as a JSON or an XML file and queues it.</summary>
[Route("api/intake-service/v1/commercial/models")]
public sealed class ModelsController(IIntakeAppService intake) : BaseServiceController
{
    /// <param name="input">The model.</param>
    /// <param name="format">The file it is kept as: <c>json</c> (the default) or <c>xml</c>.</param>
    /// <param name="cancellationToken">Cancelled when the client goes away.</param>
    [HttpPost("invoice")]
    [PermissionAuthorize(EdxEndpointPermissions.Intake.SubmitModel)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> SubmitInvoiceAsync([FromBody] InvoiceSubmissionDto input, [FromQuery] ModelFormat format = ModelFormat.Json, CancellationToken cancellationToken = default)
    {
        QueueItemDto queued = await intake.QueueModelAsync(input, format, InvoiceChannelType.ServiceDto, cancellationToken);
        return CreatedAtAction(nameof(QueueController.GetAsync), "Queue", new { id = queued.Id }, queued);
    }
}
