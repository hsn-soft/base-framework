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