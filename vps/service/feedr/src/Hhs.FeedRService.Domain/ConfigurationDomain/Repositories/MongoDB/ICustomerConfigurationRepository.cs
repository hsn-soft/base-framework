using Hhs.FeedRService.Domain.ConfigurationDomain.Entities.MongoDB;
using HsnSoft.Base.Domain.Repositories;

namespace Hhs.FeedRService.Domain.ConfigurationDomain.Repositories.MongoDB;

public interface ICustomerConfigurationRepository : IReadOnlyGenericRepository<CustomerConfiguration, Guid>
{
    Task<CustomerConfiguration> FindByUniqueKeysAsync(Guid clientId, CancellationToken cancellationToken = default);
    Task<List<CustomerConfiguration>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<CustomerConfiguration> InsertAsync(CustomerConfiguration entity, CancellationToken cancellationToken = default);
    Task<CustomerConfiguration> UpdateAsync(CustomerConfiguration entity, CancellationToken cancellationToken = default);
}