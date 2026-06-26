using Hhs.ContentService.Application.Contracts.ContentDomain.Interfaces;
using Hhs.ContentService.Application.Services;
using Hhs.ContentService.Controllers.Base;
using HsnSoft.Base.Data;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Subscribe;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hhs.ContentService.Controllers;

[Route("api/content-service/v1/commercial/tests")]
public sealed class TestController(
    IServiceProvider provider,
    IAnalysisContentAppService analysisContentAppService,
    IDataFilter dataFilter
) : BaseServiceController(provider)
{
    [AllowAnonymous]
    [HttpPost("analysis-contents")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<CreateContentResponse> GetOrCreateAnalysisContentAsync([FromBody] CreateAnalysisContentRequest request, CancellationToken cancellationToken = default)
    {
        using (dataFilter.Disable<IMultiTenant>())
        {
            using (dataFilter.Disable<IScopeSubscription>())
            {
                return await analysisContentAppService.CreateAnalysisContentAsync(request, cancellationToken);
            }
        }
    }

    [AllowAnonymous]
    [HttpPost("analysis-contents-from-scope")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<CreateContentResponse> CreateAnalysisContentFromScopeAsync([FromBody] CreateAnalysisContentFromScopeRequest request, CancellationToken cancellationToken = default)
    {
        using (dataFilter.Disable<IMultiTenant>())
        {
            using (dataFilter.Disable<IScopeSubscription>())
            {
                return await analysisContentAppService.CreateAnalysisContentFromScopeAsync(request, cancellationToken);
            }
        }
    }
}