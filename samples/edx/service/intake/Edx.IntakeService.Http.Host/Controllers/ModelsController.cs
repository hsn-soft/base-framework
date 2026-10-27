using Edx.IntakeService.Application.Contracts.IntakeDomain.Dtos;
using Edx.IntakeService.Application.Contracts.IntakeDomain.Dtos.Submits;
using Edx.IntakeService.Application.Contracts.IntakeDomain.Services;
using Edx.IntakeService.Domain.StoredFileDomain.Entities;
using Edx.IntakeService.Http.Host.Controllers.Base;
using Edx.Shared.Helper;
using HsnSoft.Base.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edx.IntakeService.Http.Host.Controllers;

/// <summary>The models of the shared API a customer sends over REST: the class is the body, and the service keeps it as a JSON or an XML file.</summary>
[Route("api/intake-service/v1/commercial/models")]
public sealed class ModelsController(IIntakeAppService intake) : BaseServiceController
{
    /// <param name="input">The model.</param>
    /// <param name="format">The file it is kept as: <c>json</c> (the default) or <c>xml</c>.</param>
    /// <param name="cancellationToken">Cancelled when the client goes away.</param>
    [PermissionAuthorize(EdxEndpointPermissions.Intake.Models.SubmitInvoice)]
    [HttpPost("invoice")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> SubmitInvoiceAsync([FromBody] InvoiceSubmissionDto input, [FromQuery] ModelFormat format = ModelFormat.Json, CancellationToken cancellationToken = default)
    {
        StoredFileDto stored = await intake.ReceiveInvoiceModelAsync(input, format, IntakeChannel.Rest, cancellationToken);
        return CreatedAtAction(nameof(FilesController.GetAsync), "Files", new { id = stored.Id }, stored);
    }
}
