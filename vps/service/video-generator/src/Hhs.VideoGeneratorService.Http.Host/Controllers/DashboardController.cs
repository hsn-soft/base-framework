using Hhs.VideoGeneratorService.Application.Contracts.DashboardDomain;
using Hhs.VideoGeneratorService.Application.Contracts.DashboardDomain.Dtos;
using Hhs.VideoGeneratorService.Controllers.Base;
using HsnSoft.Base.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hhs.VideoGeneratorService.Http.Host.Controllers;

[Produces("application/json")]
[Area("video-generator-service")]
[ApiController]
[Route("api/[area]/v1/commercial/dashboard")]

public sealed class DashboardController(
    IServiceProvider provider,
    IDashboardAppService dashboardAppService) : BaseServiceController(provider)
{
    [HttpPost("analysis-video-detail")]
    [ProducesResponseType(typeof(GetAnalysisVideoDetailResultDto), StatusCodes.Status200OK)]
    public async Task<GetAnalysisVideoDetailResultDto> GetAnalysisVideoDetailResultDtoAsync(
        [FromBody] GetAnalysisVideoDetailRequest request,
        CancellationToken cancellationToken)
        => await dashboardAppService.GetAnalysisVideoDetailResultDtoAsync(request, cancellationToken);
}
