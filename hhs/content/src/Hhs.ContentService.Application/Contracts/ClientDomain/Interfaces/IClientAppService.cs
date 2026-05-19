using Hhs.ContentService.Application.Contracts.ClientDomain.Dtos;
using Hhs.ContentService.Application.Contracts.ClientDomain.Dtos.Filters;
using Hhs.ContentService.Application.Contracts.ClientDomain.Dtos.Submits;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.EventBus;

namespace Hhs.ContentService.Application.Contracts.ClientDomain.Interfaces;

public interface IClientAppService : IEventApplicationService
{
    Task<ClientDto> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedDataResultDto<ClientDto>> GetPagedListAsync(GetClientsPaged pagedInput, CancellationToken cancellationToken = default);
    Task<List<ClientDto>> GetFilterListAsync(GetClientsFilter filterInput, CancellationToken cancellationToken = default);
    Task<List<ClientSearchDto>> GetSearchListAsync(GetClientsSearch searchInput, CancellationToken cancellationToken = default);

    Task<ClientDto> CreateAsync(ClientCreateDto input);

    Task UpdateAsync(ClientUpdateDto input);

    Task DeleteAsync(Guid id);
}