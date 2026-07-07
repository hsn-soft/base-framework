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
            facility: "RETRY_DUE_REQUESTS",
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
            facility: "POLL_DUE_OUTLINE_REQUESTS",
            correlationId: correlationId,
            exception: null
        ));

        await outlineProviderPollingWorkerService.PollDueOutlineRequestsAsync(cancellationToken);
    }
}
