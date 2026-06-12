using Hhs.VideoGeneratorService.Application.Contracts.JobDomain.Dtos;
using JetBrains.Annotations;

namespace Hhs.VideoGeneratorService.Application.Contracts.JobDomain;

public interface IJobAppService
{
    Task VideoRequestQueryTriggerAsync(VideoRequestQueryTriggerDto input, [CanBeNull] string correlationId = null);
}