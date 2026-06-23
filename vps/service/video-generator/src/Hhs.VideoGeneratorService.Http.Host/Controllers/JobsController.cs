using Hhs.Shared.Hosting.Attributes;
using Hhs.VideoGeneratorService.Application.Contracts.JobDomain;
using Hhs.VideoGeneratorService.Application.Contracts.JobDomain.Dtos;
using HsnSoft.Base.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;

namespace Hhs.VideoGeneratorService.Controllers;

[ClientApiKeyAuth(KeyLabel = "SchedulerApiKey")]
[Produces("application/json")]
[Area("video-generator-service")]
[ApiController]
[Route("api/[area]/v1/commercial/jobs")]
public sealed class JobsController(IHttpContextAccessor httpContextAccessor, IJobAppService jobAppService) : ApiControllerBase
{
    [HttpPost("video-request-query")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task VideoRequestQueryAsync([FromBody] VideoRequestQueryTriggerDto input)
    {
        await jobAppService.VideoRequestQueryTriggerAsync(input, GetJobCorrelationId());
    }

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