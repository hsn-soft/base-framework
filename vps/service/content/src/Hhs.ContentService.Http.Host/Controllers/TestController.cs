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
    ContentOperationAppService appService,
    IDataFilter dataFilter
) : BaseServiceController(provider)
{
    private readonly IDataFilter _dataFilter = dataFilter;

    [AllowAnonymous]
    [HttpPost("customer-contents")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<CreateContentResponse> GetOrCreateCustomerContentAsync([FromBody] CreateCustomerContentRequest request, CancellationToken cancellationToken = default)
    {
        using (_dataFilter.Disable<IMultiTenant>())
        {
            using (_dataFilter.Disable<IScopeSubscription>())
            {
                return await appService.CreateCustomerContentAsync(request, cancellationToken);
            }
        }
    }

    [AllowAnonymous]
    [HttpPost("analysis-contents")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<CreateContentResponse> GetOrCreateAnalysisContentAsync([FromBody] CreateAnalysisContentRequest request, CancellationToken cancellationToken = default)
    {
        using (_dataFilter.Disable<IMultiTenant>())
        {
            using (_dataFilter.Disable<IScopeSubscription>())
            {
                return await appService.CreateAnalysisContentAsync(request, cancellationToken);
            }
        }
    }
}