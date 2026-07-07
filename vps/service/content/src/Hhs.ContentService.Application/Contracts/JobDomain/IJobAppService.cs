using Hhs.ContentService.Application.Contracts.JobDomain.Dtos;
using JetBrains.Annotations;

namespace Hhs.ContentService.Application.Contracts.JobDomain;

public interface IJobAppService
{
    Task AnalysisVideoGenerationQueryTriggerAsync(AnalysisVideoGenerationQueryTriggerDto input, [CanBeNull] string correlationId = null);
    Task TrendVideoGenerationQueryTriggerAsync(TrendVideoGenerationQueryTriggerDto input, [CanBeNull] string correlationId = null);

    Task TestQueryTriggerAsync(TestQueryTriggerDto input, [CanBeNull] string correlationId = null);

    Task RetryDueRequestsTriggerAsync(RetryDueRequestsTriggerDto input, [CanBeNull] string correlationId = null, CancellationToken cancellationToken = default);
}