using Hhs.TextNormalizerService.Application.Contracts.DasbhoardDomain.Dtos;
using JetBrains.Annotations;

namespace Hhs.TextNormalizerService.Application.Contracts.DasbhoardDomain;

public interface IDashboardAppService
{
    Task<GetContentTextDetailResultDto> GetContentTextDetailResultDtoAsync(GetContentTextDetailRequestDto input, [CanBeNull] CancellationToken cancellationToken = default);
}