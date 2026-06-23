using Hhs.TextNormalizerService.Application.Contracts.DasbhoardDomain;
using Hhs.TextNormalizerService.Application.Contracts.DasbhoardDomain.Dtos;
using Hhs.TextNormalizerService.Controllers.Base;
using Microsoft.AspNetCore.Mvc;

namespace Hhs.TextNormalizerService.Controllers;

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
