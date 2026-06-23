using Hhs.ContentService.Application.Services;
using Hhs.ContentService.Controllers.Base;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hhs.ContentService.Controllers;

[Route("api/content-service/v1/commercial/tests")]
public sealed class TestController(
    IServiceProvider provider,
    ContentOperationAppService appService
) : BaseServiceController(provider)
{
    [AllowAnonymous]
    [HttpPost("customer-contents")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<CreateContentResponse> GetOrCreateCustomerContentAsync([FromBody] CreateCustomerContentRequest request, CancellationToken cancellationToken = default)
        => await appService.CreateCustomerContentAsync(request, cancellationToken);

    [AllowAnonymous]
    [HttpPost("analysis-contents")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<CreateContentResponse> GetOrCreateAnalysisContentAsync([FromBody] CreateAnalysisContentRequest request, CancellationToken cancellationToken = default)
        => await appService.CreateAnalysisContentAsync(request, cancellationToken);
}