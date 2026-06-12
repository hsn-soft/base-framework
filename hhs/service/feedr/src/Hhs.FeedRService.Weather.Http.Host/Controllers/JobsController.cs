using Hhs.FeedRService.Application.Contracts.JobDomain;
using Hhs.FeedRService.Application.Contracts.JobDomain.Dtos;
using Hhs.Shared.Hosting.Attributes;
using HsnSoft.Base.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;

namespace Hhs.FeedRService.Weather.Controllers;

[ClientApiKeyAuth(KeyLabel = "SchedulerApiKey")]
[Produces("application/json")]
[Area("feedr-weather-service")]
[ApiController]
[Route("api/[area]/v1/commercial/jobs")]
public sealed class JobsController(
    IHttpContextAccessor httpContextAccessor,
    IJobAppService jobAppService
) : ApiControllerBase
{
    [HttpPost("test-query")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    // [EndpointConcurrencyLimit("feedr-weather:jobs:test-query", defaultLimit: 3, ttlSeconds: 60)]
    [EventConcurrencyLimit("feedr-weather:jobs:test-query", defaultLimit: 3, ttlSeconds: 60)]
    public async Task TestAsync([FromBody] TestQueryTriggerDto input)
        => await jobAppService.TestQueryTriggerAsync(input, GetJobCorrelationId());

    #region Private Functions

    [NonAction]
    private string GetJobCorrelationId()
    {
        string correlationId = null;
        if (httpContextAccessor.HttpContext != null && httpContextAccessor.HttpContext.Request.Headers.TryGetValue("SchedulerJobId", out var schedulerJobId))
        {
            correlationId = schedulerJobId.ToString();
        }

        return string.IsNullOrWhiteSpace(correlationId) ? null : correlationId;
    }

    # endregion
}