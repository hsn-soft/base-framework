using Edx.IntakeService.Application.Contracts.IntakeDomain.Dtos;
using Edx.IntakeService.Application.Contracts.IntakeDomain.Services;
using Edx.IntakeService.Domain.StoredFileDomain.Entities;
using Edx.IntakeService.Http.Host.Controllers.Base;
using Edx.Shared.Helper;
using HsnSoft.Base.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edx.IntakeService.Http.Host.Controllers;

/// <summary>The archives a customer sends over REST: a zip as the file of a multipart form (<c>file</c>).</summary>
[Route("api/intake-service/v1/commercial/archives")]
public sealed class ArchivesController(IIntakeAppService intake) : BaseServiceController
{
    /// <summary>A zip of XML files, each well formed.</summary>
    [PermissionAuthorize(EdxEndpointPermissions.Intake.Archives.UploadXml)]
    [HttpPost("xml")]
    [Consumes("multipart/form-data")]
    [DisableRequestSizeLimit]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> UploadXmlAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        await using Stream content = file.OpenReadStream();
        StoredFileDto stored = await intake.ReceiveXmlArchiveAsync(file.FileName, content, IntakeChannel.Rest, cancellationToken);
        return Created(stored);
    }

    /// <summary>A zip of text, CSV or Excel files.</summary>
    [PermissionAuthorize(EdxEndpointPermissions.Intake.Archives.UploadTabular)]
    [HttpPost("tabular")]
    [Consumes("multipart/form-data")]
    [DisableRequestSizeLimit]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> UploadTabularAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        await using Stream content = file.OpenReadStream();
        StoredFileDto stored = await intake.ReceiveTabularArchiveAsync(file.FileName, content, IntakeChannel.Rest, cancellationToken);
        return Created(stored);
    }

    private CreatedAtActionResult Created(StoredFileDto stored)
        => CreatedAtAction(nameof(FilesController.GetAsync), "Files", new { id = stored.Id }, stored);
}
