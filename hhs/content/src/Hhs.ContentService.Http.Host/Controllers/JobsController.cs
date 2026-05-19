using Hhs.ContentService.Application.Contracts.JobDomain;
using Hhs.ContentService.Application.Contracts.JobDomain.Dtos;
using Hhs.Shared.Hosting.Attributes;
using HsnSoft.Base.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;

namespace Hhs.ContentService.Controllers;

[ClientApiKeyAuth(KeyLabel = "SchedulerApiKey")]
[Produces("application/json")]
[Area("content-service")]
[ApiController]
[Route("api/[area]/v1/commercial/jobs")]
public sealed class JobsController(
    IHttpContextAccessor httpContextAccessor,
    IJobAppService jobAppService
) : ApiControllerBase
{
    [HttpPost("analysis-video-generation-query")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task AnalysisVideoGenerationQueryAsync([FromBody] AnalysisVideoGenerationQueryTriggerDto input)
        => await jobAppService.AnalysisVideoGenerationQueryTriggerAsync(input, GetJobCorrelationId());

    [HttpPost("dashboard-response-statistic-query")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task DashboardResponseStatisticQueryAsync([FromBody] DashboardResponseStatisticQueryTriggerDto input)
        => await jobAppService.DashboardResponseStatisticQueryTriggerAsync(input, GetJobCorrelationId());

    [HttpPost("trend-video-generation-query")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task TrendVideoGenerationQueryAsync([FromBody] TrendVideoGenerationQueryTriggerDto input)
        => await jobAppService.TrendVideoGenerationQueryTriggerAsync(input, GetJobCorrelationId());

    [HttpPost("test-query")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    // [EndpointConcurrencyLimit("content:jobs:test-query", defaultLimit: 3, ttlSeconds: 60)]
    [EventConcurrencyLimit("content:jobs:test-query", defaultLimit: 3, ttlSeconds: 60)]
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