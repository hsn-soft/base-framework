using Hhs.FeedRService.Application.Contracts.CustomerDomain.Dtos;

namespace Hhs.FeedRService.Application.Contracts.CustomerDomain;

public interface INetworkConfigurationAppService
{
    Task<NetworkConfigurationDto> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<NetworkConfigurationDto> GetByNetworkCodeAsync(string networkCode, CancellationToken cancellationToken = default);
    Task<List<NetworkConfigurationDto>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<NetworkConfigurationDto> CreateAsync(CreateNetworkConfigurationDto input, CancellationToken cancellationToken = default);
    Task<NetworkConfigurationDto> UpdateAsync(Guid id, UpdateNetworkConfigurationDto input, CancellationToken cancellationToken = default);
}
