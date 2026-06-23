using Hhs.FeedRService.Application.Contracts.JobDomain;
using Hhs.FeedRService.Application.Contracts.JobDomain.Dtos;
using Hhs.Shared.Hosting.Attributes;
using HsnSoft.Base.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;

namespace Hhs.FeedRService.AdManager.Controllers;

[ClientApiKeyAuth(KeyLabel = "SchedulerApiKey")]
[Produces("application/json")]
[Area("feedr-admanager-service")]
[ApiController]
[Route("api/[area]/v1/commercial/jobs")]

public sealed class JobsController(IServiceProvider provider, IHttpContextAccessor httpContextAccessor,IJobAppService jobAppService)  : ApiControllerBase
{
    [HttpPost("test-query-ad")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [EventConcurrencyLimit("feedr-admanager:jobs:test-query", defaultLimit: 3, ttlSeconds: 60)]
    public async Task TestAsync([FromBody] TestQueryTriggerDto input)
        => await jobAppService.TestQueryTriggerAsync(input, GetJobCorrelationId());


    [HttpPost("generate-summary-report")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [EventConcurrencyLimit("feedr-admanager:jobs:generate-summary-report", defaultLimit: 3, ttlSeconds: 60)]
    public async Task TriggerAsync([FromBody] GenerateSummaryReportTriggerDto input)
        => await jobAppService.GenerateSummaryReportTriggerAsync(input, GetJobCorrelationId());

    [HttpPost("derive-pending-reports")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [EventConcurrencyLimit("feedr-admanager:jobs:derive-pending-reports", defaultLimit: 1, ttlSeconds: 60)]
    public async Task DerivePendingReportsAsync([FromBody] DerivePendingReportsTriggerDto input)
        => await jobAppService.DerivePendingReportsAsync(input, GetJobCorrelationId());


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

    #endregion
}