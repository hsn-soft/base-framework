using Hhs.TextNormalizerService.Application.Consts;
using Hhs.TextNormalizerService.Application.Contracts.JobDomain;
using Hhs.TextNormalizerService.Application.Contracts.JobDomain.Dtos;
using HsnSoft.Base.Logging;
using HsnSoft.Base.Logging.Abstracts;
using Microsoft.Extensions.DependencyInjection;

namespace Hhs.TextNormalizerService.Application.Services;

public sealed class JobAppService(
    IServiceProvider provider,
    NormalizerOperationRetryWorkerService normalizerOperationRetryWorkerService,
    OutlineProviderPollingWorkerService outlineProviderPollingWorkerService
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

        await normalizerOperationRetryWorkerService.RetryDueRequestsAsync(cancellationToken);
    }

    public async Task PollDueOutlineRequestsTriggerAsync(PollDueOutlineRequestsTriggerDto input, string correlationId = null, CancellationToken cancellationToken = default)
    {
        _logger.FrameworkInfoLog(LogHelper.Generate(
            message: $"{input.JobName} successfully triggered",
            reference: new { Type = "Job", Key = input.JobName, input.JobPeriodDesc, input.NextTriggerTimeUtc },
            facility: Facilities.PollDueOutlineRequestsTriggered,
            correlationId: correlationId,
            exception: null
        ));

        await outlineProviderPollingWorkerService.PollDueOutlineRequestsAsync(cancellationToken);
    }

    public async Task CheckReadyAnalysisContentsToOutlineTriggerAsync(CheckReadyAnalysisContentsToOutlineTriggerDto input, string correlationId = null, CancellationToken cancellationToken = default)
    {
        _logger.FrameworkInfoLog(LogHelper.Generate(
            message: $"{input.JobName} successfully triggered",
            reference: new { Type = "Job", Key = input.JobName, input.JobPeriodDesc, input.NextTriggerTimeUtc },
            facility: Facilities.CheckReadyAnalysisContentsToOutlineTriggered,
            correlationId: correlationId,
            exception: null
        ));

        await normalizerOperationRetryWorkerService.CheckReadyAnalysisContentsToOutlineAsync(cancellationToken);
    }

    public async Task CheckReadyAnalysisContentsToResultTriggerAsync(CheckReadyAnalysisContentsToResultTriggerDto input, string correlationId = null, CancellationToken cancellationToken = default)
    {
        _logger.FrameworkInfoLog(LogHelper.Generate(
            message: $"{input.JobName} successfully triggered",
            reference: new { Type = "Job", Key = input.JobName, input.JobPeriodDesc, input.NextTriggerTimeUtc },
            facility: Facilities.CheckReadyAnalysisContentsToResultTriggered,
            correlationId: correlationId,
            exception: null
        ));

        await normalizerOperationRetryWorkerService.CheckReadyAnalysisContentsToResultAsync(cancellationToken);
    }
}