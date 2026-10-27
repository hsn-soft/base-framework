using Edx.IntakeService.Application.Contracts.IntakeDomain.Dtos;
using Edx.IntakeService.Application.Contracts.IntakeDomain.Dtos.Filters;
using Edx.IntakeService.Application.Contracts.IntakeDomain.Services;
using Edx.IntakeService.Http.Host.Controllers.Base;
using Edx.Shared.Helper;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edx.IntakeService.Http.Host.Controllers;

/// <summary>What was received: the records of the files, and the files themselves. A customer sees only its own tenant's.</summary>
[Route("api/intake-service/v1/commercial/files")]
public sealed class FilesController(IIntakeAppService intake) : BaseServiceController
{
    [PermissionAuthorize(EdxEndpointPermissions.Intake.Files.Get)]
    [HttpPost("get/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<StoredFileDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => intake.GetAsync(id, cancellationToken);

    [PermissionAuthorize(EdxEndpointPermissions.Intake.Files.PagedList)]
    [HttpPost("paged-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<PagedDataResultDto<StoredFileDto>> GetPagedListAsync([FromBody] GetStoredFilesPaged pagedInput, CancellationToken cancellationToken = default)
        => intake.GetPagedListAsync(pagedInput, cancellationToken);

    /// <summary>The file as it was received.</summary>
    [PermissionAuthorize(EdxEndpointPermissions.Intake.Files.Download)]
    [HttpGet("{id:guid}/download")]
    [Produces("application/octet-stream")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DownloadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        StoredFileContent file = await intake.OpenAsync(id, cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }
}
