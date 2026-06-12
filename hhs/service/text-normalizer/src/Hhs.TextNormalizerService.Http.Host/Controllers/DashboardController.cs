using Hhs.TextNormalizerService.Application.Contracts.DashboardDomain;
using Hhs.TextNormalizerService.Application.Contracts.DashboardDomain.Dtos;
using Hhs.TextNormalizerService.Controllers.Base;
using HsnSoft.Base.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hhs.TextNormalizerService.Http.Host.Controllers;

[Produces("application/json")]
[Area("text-normalizer-service")]
[ApiController]
[Route("api/[area]/v1/commercial/dashboard")]

public sealed class DashboardController(
    IServiceProvider provider,
    IDashboardAppService dashboardAppService) : BaseServiceController(provider)
{
    [HttpPost("content-text-detail")]
    [ProducesResponseType(typeof(GetContentTextDetailResultDto), StatusCodes.Status200OK)]
    public async Task<GetContentTextDetailResultDto> GetContentTextDetailResultDtoAsync(
        [FromBody] GetContentTextDetailRequestDto request,
        CancellationToken cancellationToken)
        => await dashboardAppService.GetContentTextDetailResultDtoAsync(request, cancellationToken);
}
