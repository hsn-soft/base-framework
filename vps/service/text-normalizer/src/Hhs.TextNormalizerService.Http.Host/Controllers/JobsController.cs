using Hhs.Shared.Hosting.Attributes;
using Hhs.TextNormalizerService.Application.Contracts.JobDomain;
using Hhs.TextNormalizerService.Application.Contracts.JobDomain.Dtos;
using HsnSoft.Base.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;

namespace Hhs.TextNormalizerService.Controllers;

[ClientApiKeyAuth(KeyLabel = "SchedulerApiKey")]
[Produces("application/json")]
[Area("text-normalizer-service")]
[ApiController]
[Route("api/[area]/v1/commercial/jobs")]
public sealed class JobsController(
    IHttpContextAccessor httpContextAccessor,
    IJobAppService jobAppService
) : ApiControllerBase
{
    [HttpPost("retry-due-requests")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [EndpointConcurrencyLimit("text-normalizer:jobs:retry-due-requests", defaultLimit: 1, ttlSeconds: 120)]
    public async Task RetryDueRequestsAsync([FromBody] RetryDueRequestsTriggerDto input, CancellationToken cancellationToken)
        => await jobAppService.RetryDueRequestsTriggerAsync(input, GetJobCorrelationId(), cancellationToken);

    [HttpPost("poll-due-outline-requests")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [EndpointConcurrencyLimit("text-normalizer:jobs:poll-due-outline-requests", defaultLimit: 1, ttlSeconds: 120)]
    public async Task PollDueOutlineRequestsAsync([FromBody] PollDueOutlineRequestsTriggerDto input, CancellationToken cancellationToken)
        => await jobAppService.PollDueOutlineRequestsTriggerAsync(input, GetJobCorrelationId(), cancellationToken);

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
