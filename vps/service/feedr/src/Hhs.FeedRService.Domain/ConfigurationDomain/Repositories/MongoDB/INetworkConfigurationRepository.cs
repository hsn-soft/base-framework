using Hhs.FeedRService.Domain.ConfigurationDomain.Entities.MongoDB;
using HsnSoft.Base.Domain.Repositories;

namespace Hhs.FeedRService.Domain.ConfigurationDomain.Repositories.MongoDB;

public interface INetworkConfigurationRepository : IReadOnlyGenericRepository<NetworkConfiguration, Guid>
{
    Task<List<NetworkConfiguration>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    Task<NetworkConfiguration> GetByNetworkCodeAsync(string networkCode, CancellationToken cancellationToken = default);

    Task<NetworkConfiguration> InsertAsync(NetworkConfiguration entity, CancellationToken cancellationToken = default);

    Task<NetworkConfiguration> UpdateAsync(NetworkConfiguration entity, CancellationToken cancellationToken = default);
}
