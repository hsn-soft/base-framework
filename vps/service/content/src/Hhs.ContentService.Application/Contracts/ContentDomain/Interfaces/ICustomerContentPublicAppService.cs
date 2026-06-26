using Hhs.ContentService.Application.Contracts.ContentDomain.Dtos;
using Hhs.ContentService.Application.Contracts.ContentDomain.Submits;
using HsnSoft.Base.EventBus;

namespace Hhs.ContentService.Application.Contracts.ContentDomain.Interfaces;

public interface ICustomerContentPublicAppService : IEventApplicationService
{
    Task<GetOrCreateCustomerContentResponseDto> GetOrCreateAsync(GetOrCreateCustomerContentRequestDto input, CancellationToken cancellationToken = default);
    Task CreateAdResultAsync(CreateContentAdResultDto input, CancellationToken cancellationToken = default);
}