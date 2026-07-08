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
public sealed class JobsController(
    IHttpContextAccessor httpContextAccessor,
    IJobAppService jobAppService
) : ApiControllerBase
{
    [HttpPost("retry-due-requests")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [EndpointConcurrencyLimit("video-generator:jobs:retry-due-requests", defaultLimit: 1, ttlSeconds: 120)]
    public async Task RetryDueRequestsAsync([FromBody] RetryDueRequestsTriggerDto input, CancellationToken cancellationToken)
        => await jobAppService.RetryDueRequestsTriggerAsync(input, GetJobCorrelationId(), cancellationToken);

    [HttpPost("poll-due-video-requests")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [EndpointConcurrencyLimit("video-generator:jobs:poll-due-video-requests", defaultLimit: 1, ttlSeconds: 120)]
    public async Task PollDueVideoRequestsAsync([FromBody] PollDueVideoRequestsTriggerDto input, CancellationToken cancellationToken)
        => await jobAppService.PollDueVideoRequestsTriggerAsync(input, GetJobCorrelationId(), cancellationToken);

    [HttpPost("poll-due-audio-requests")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [EndpointConcurrencyLimit("video-generator:jobs:poll-due-audio-requests", defaultLimit: 1, ttlSeconds: 120)]
    public async Task PollDueAudioRequestsAsync([FromBody] PollDueAudioRequestsTriggerDto input, CancellationToken cancellationToken)
        => await jobAppService.PollDueAudioRequestsTriggerAsync(input, GetJobCorrelationId(), cancellationToken);

    [HttpPost("advance-ready-video-requests")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [EndpointConcurrencyLimit("video-generator:jobs:advance-ready-video-requests", defaultLimit: 1, ttlSeconds: 120)]
    public async Task AdvanceReadyVideoRequestsAsync([FromBody] AdvanceReadyVideoRequestsTriggerDto input, CancellationToken cancellationToken)
        => await jobAppService.AdvanceReadyVideoRequestsTriggerAsync(input, GetJobCorrelationId(), cancellationToken);

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
