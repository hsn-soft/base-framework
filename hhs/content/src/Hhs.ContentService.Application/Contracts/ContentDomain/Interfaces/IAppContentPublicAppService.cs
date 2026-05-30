using Hhs.ContentService.Application.Contracts.ContentDomain.Dtos;
using HsnSoft.Base.EventBus;

namespace Hhs.ContentService.Application.Contracts.ContentDomain.Interfaces;

public interface IAppContentPublicAppService : IEventApplicationService
{
    Task<GetOrCreateAppContentResponseDto> GetOrCreateAsync(GetOrCreateAppContentRequestDto input, CancellationToken cancellationToken = default);
    Task CreateAdResultAsync(CreateContentAdResultDto input, CancellationToken cancellationToken = default);
}