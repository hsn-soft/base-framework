using Hhs.VideoGeneratorService.Application.Contracts.DashboardDomain.Dtos;
using JetBrains.Annotations;

namespace Hhs.VideoGeneratorService.Application.Contracts.DashboardDomain;

public interface IDashboardAppService
{
    Task<GetAnalysisVideoDetailResultDto> GetAnalysisVideoDetailResultDtoAsync(GetAnalysisVideoDetailRequest input, [CanBeNull] CancellationToken cancellationToken = default);
}