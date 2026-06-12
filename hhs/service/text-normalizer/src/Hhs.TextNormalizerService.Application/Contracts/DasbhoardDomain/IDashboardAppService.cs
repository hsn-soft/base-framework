using Hhs.TextNormalizerService.Application.Contracts.DashboardDomain.Dtos;
using JetBrains.Annotations;

namespace Hhs.TextNormalizerService.Application.Contracts.DashboardDomain;

public interface IDashboardAppService
{
    Task<GetContentTextDetailResultDto> GetContentTextDetailResultDtoAsync(GetContentTextDetailRequestDto input, [CanBeNull] CancellationToken cancellationToken = default);
}