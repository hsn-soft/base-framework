using Hhs.ContentService.Application.Contracts.JobDomain.Dtos;
using JetBrains.Annotations;

namespace Hhs.ContentService.Application.Contracts.JobDomain;

public interface IJobAppService
{
    Task AnalysisVideoGenerationQueryTriggerAsync(AnalysisVideoGenerationQueryTriggerDto input, [CanBeNull] string correlationId = null);
}