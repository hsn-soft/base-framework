using Hhs.TextNormalizerService.Application.Contracts.JobDomain.Dtos;
using JetBrains.Annotations;

namespace Hhs.TextNormalizerService.Application.Contracts.JobDomain;

public interface IJobAppService
{
    Task RetryDueRequestsTriggerAsync(RetryDueRequestsTriggerDto input, [CanBeNull] string correlationId = null, CancellationToken cancellationToken = default);
    Task PollDueOutlineRequestsTriggerAsync(PollDueOutlineRequestsTriggerDto input, [CanBeNull] string correlationId = null, CancellationToken cancellationToken = default);
    Task CheckReadyAnalysisContentsToOutlineTriggerAsync(CheckReadyAnalysisContentsToOutlineTriggerDto input, [CanBeNull] string correlationId = null, CancellationToken cancellationToken = default);
    Task CheckReadyAnalysisContentsToResultTriggerAsync(CheckReadyAnalysisContentsToResultTriggerDto input, [CanBeNull] string correlationId = null, CancellationToken cancellationToken = default);
}
