using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Authorization;
using Hsx.Shared.Contracts.Permissions;
using Hsx.VideoGeneratorService.Application.Contracts.VideoRequestDomain.Dtos;
using Hsx.VideoGeneratorService.Application.Contracts.VideoRequestDomain.Dtos.Filters;
using Hsx.VideoGeneratorService.Application.Contracts.VideoRequestDomain.Dtos.Submits;
using Hsx.VideoGeneratorService.Application.Contracts.VideoRequestDomain.Services;
using Hsx.VideoGeneratorService.Controllers.Base;
using Microsoft.AspNetCore.Mvc;

namespace Hsx.VideoGeneratorService.Controllers;

/// <summary>The video requests: every action only calls the application service, which holds the rules.</summary>
[Route("api/video-generator-service/v1/commercial/video-requests")]
public sealed class VideoRequestsController(IVideoRequestAppService videoRequestAppService) : BaseServiceController
{
    [HttpPost("get/{id:guid}")]
    [PermissionAuthorize(VideoGeneratorPermissions.Endpoints.VideoRequests.Read)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<VideoRequestDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => videoRequestAppService.GetAsync(id, cancellationToken);

    [HttpPost("paged-list")]

    [PermissionAuthorize(VideoGeneratorPermissions.Endpoints.VideoRequests.Read)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<PagedDataResultDto<VideoRequestDto>> GetPagedListAsync([FromBody] GetVideoRequestsPaged pagedInput, CancellationToken cancellationToken = default)
        => videoRequestAppService.GetPagedListAsync(pagedInput, cancellationToken);

    [HttpPost("create")]

    [PermissionAuthorize(VideoGeneratorPermissions.Endpoints.VideoRequests.Create)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAsync([FromBody] VideoRequestCreateDto input, CancellationToken cancellationToken = default)
    {
        VideoRequestDto created = await videoRequestAppService.CreateAsync(input, cancellationToken);
        return CreatedAtAction(nameof(GetAsync), new { id = created.Id }, created);
    }

    [HttpPut]

    [PermissionAuthorize(VideoGeneratorPermissions.Endpoints.VideoRequests.Update)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateAsync([FromBody] VideoRequestUpdateDto input, CancellationToken cancellationToken = default)
    {
        await videoRequestAppService.UpdateAsync(input, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]

    [PermissionAuthorize(VideoGeneratorPermissions.Endpoints.VideoRequests.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await videoRequestAppService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
