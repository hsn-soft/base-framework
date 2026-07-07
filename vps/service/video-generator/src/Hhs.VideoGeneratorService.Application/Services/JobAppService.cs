using Hhs.VideoGeneratorService.Application.Contracts.JobDomain;
using Hhs.VideoGeneratorService.Application.Contracts.JobDomain.Dtos;
using HsnSoft.Base.Logging;
using HsnSoft.Base.Logging.Abstracts;
using Microsoft.Extensions.DependencyInjection;

namespace Hhs.VideoGeneratorService.Application.Services;

public sealed class JobAppService(
    IServiceProvider provider,
    VideoOperationRetryWorkerService videoOperationRetryWorkerService,
    VideoProviderPollingWorkerService videoProviderPollingWorkerService,
    AudioProviderPollingWorkerService audioProviderPollingWorkerService
) : ApplicationServiceBase(provider), IJobAppService
{
    private readonly IFrameworkLogger _logger = provider.GetRequiredService<IFrameworkLogger>();

    public async Task RetryDueRequestsTriggerAsync(RetryDueRequestsTriggerDto input, string correlationId = null, CancellationToken cancellationToken = default)
    {
        _logger.FrameworkInfoLog(LogHelper.Generate(
            message: $"{input.JobName} successfully triggered",
            reference: new { RefContentId = input.JobName },
            facility: "RETRY_DUE_REQUESTS",
            correlationId: correlationId,
            exception: null
        ));

        await videoOperationRetryWorkerService.RetryDueRequestsAsync(cancellationToken);
    }

    public async Task PollDueVideoRequestsTriggerAsync(PollDueVideoRequestsTriggerDto input, string correlationId = null, CancellationToken cancellationToken = default)
    {
        _logger.FrameworkInfoLog(LogHelper.Generate(
            message: $"{input.JobName} successfully triggered",
            reference: new { RefContentId = input.JobName },
            facility: "POLL_DUE_VIDEO_REQUESTS",
            correlationId: correlationId,
            exception: null
        ));

        await videoProviderPollingWorkerService.PollDueVideoRequestsAsync(cancellationToken);
    }

    public async Task PollDueAudioRequestsTriggerAsync(PollDueAudioRequestsTriggerDto input, string correlationId = null, CancellationToken cancellationToken = default)
    {
        _logger.FrameworkInfoLog(LogHelper.Generate(
            message: $"{input.JobName} successfully triggered",
            reference: new { RefContentId = input.JobName },
            facility: "POLL_DUE_AUDIO_REQUESTS",
            correlationId: correlationId,
            exception: null
        ));

        await audioProviderPollingWorkerService.PollDueAudioRequestsAsync(cancellationToken);
    }
}
