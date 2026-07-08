using Hhs.VideoGeneratorService.Application.Consts;
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
            reference: new { Type = "Job", Key = input.JobName, input.JobPeriodDesc, input.NextTriggerTimeUtc },
            facility: Facilities.RetryDueRequestsTriggered,
            correlationId: correlationId,
            exception: null
        ));

        await videoOperationRetryWorkerService.RetryDueRequestsAsync(cancellationToken);
    }

    public async Task PollDueVideoRequestsTriggerAsync(PollDueVideoRequestsTriggerDto input, string correlationId = null, CancellationToken cancellationToken = default)
    {
        _logger.FrameworkInfoLog(LogHelper.Generate(
            message: $"{input.JobName} successfully triggered",
            reference: new { Type = "Job", Key = input.JobName, input.JobPeriodDesc, input.NextTriggerTimeUtc },
            facility: Facilities.PollDueVideoRequestsTriggered,
            correlationId: correlationId,
            exception: null
        ));

        await videoProviderPollingWorkerService.PollDueVideoRequestsAsync(cancellationToken);
    }

    public async Task PollDueAudioRequestsTriggerAsync(PollDueAudioRequestsTriggerDto input, string correlationId = null, CancellationToken cancellationToken = default)
    {
        _logger.FrameworkInfoLog(LogHelper.Generate(
            message: $"{input.JobName} successfully triggered",
            reference: new { Type = "Job", Key = input.JobName, input.JobPeriodDesc, input.NextTriggerTimeUtc },
            facility: Facilities.PollDueAudioRequestsTriggered,
            correlationId: correlationId,
            exception: null
        ));

        await audioProviderPollingWorkerService.PollDueAudioRequestsAsync(cancellationToken);
    }

    public async Task AdvanceReadyVideoRequestsTriggerAsync(AdvanceReadyVideoRequestsTriggerDto input, string correlationId = null, CancellationToken cancellationToken = default)
    {
        _logger.FrameworkInfoLog(LogHelper.Generate(
            message: $"{input.JobName} successfully triggered",
            reference: new { Type = "Job", Key = input.JobName, input.JobPeriodDesc, input.NextTriggerTimeUtc },
            facility: Facilities.AdvanceReadyVideoRequestsTriggered,
            correlationId: correlationId,
            exception: null
        ));

        await videoOperationRetryWorkerService.AdvanceReadyVideoRequestsToProviderStartAsync(cancellationToken);
    }
}
