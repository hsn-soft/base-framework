using Hhs.AdministrationService.Application.Contracts.JobDomain;
using Hhs.AdministrationService.Application.Contracts.JobDomain.Dtos;
using Hhs.Shared.Hosting.Attributes;
using HsnSoft.Base.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;

namespace Hhs.AdministrationService.Controllers;

[ClientApiKeyAuth(KeyLabel = "SchedulerApiKey")]
[Produces("application/json")]
[Area("administration-service")]
[ApiController]
[Route("api/[area]/v1/commercial/jobs")]
public sealed class JobsController(IHttpContextAccessor httpContextAccessor, IJobAppService jobAppService) : ApiControllerBase
{
    [HttpPost("synch-all-permission-to-cache-db")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task SynchAllPermissionToCacheDbAsync([FromBody] SynchAllPermissionToCacheDbTriggerDto input)
    {
        await jobAppService.SynchAllPermissionToCacheDbTriggerAsync(input, GetJobCorrelationId());
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