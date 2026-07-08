using Hhs.VideoGeneratorService.Application.Contracts.JobDomain.Dtos;
using JetBrains.Annotations;

namespace Hhs.VideoGeneratorService.Application.Contracts.JobDomain;

public interface IJobAppService
{
    Task RetryDueRequestsTriggerAsync(RetryDueRequestsTriggerDto input, [CanBeNull] string correlationId = null, CancellationToken cancellationToken = default);
    Task PollDueVideoRequestsTriggerAsync(PollDueVideoRequestsTriggerDto input, [CanBeNull] string correlationId = null, CancellationToken cancellationToken = default);
    Task PollDueAudioRequestsTriggerAsync(PollDueAudioRequestsTriggerDto input, [CanBeNull] string correlationId = null, CancellationToken cancellationToken = default);
    Task AdvanceReadyVideoRequestsTriggerAsync(AdvanceReadyVideoRequestsTriggerDto input, [CanBeNull] string correlationId = null, CancellationToken cancellationToken = default);
}
