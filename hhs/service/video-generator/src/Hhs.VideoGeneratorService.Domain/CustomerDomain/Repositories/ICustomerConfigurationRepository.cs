using Hhs.VideoGeneratorService.Domain.CustomerDomain.Entities;
using HsnSoft.Base.Domain.Repositories;

namespace Hhs.VideoGeneratorService.Domain.CustomerDomain.Repositories;

public interface ICustomerConfigurationRepository : IReadOnlyGenericRepository<CustomerConfiguration, Guid>
{
    Task<CustomerConfiguration> FindByUniqueKeysAsync(Guid clientId, CancellationToken cancellationToken = default);
}