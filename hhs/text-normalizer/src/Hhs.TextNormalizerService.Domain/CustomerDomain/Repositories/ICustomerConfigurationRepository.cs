using Hhs.TextNormalizerService.Domain.CustomerDomain.Entities;
using HsnSoft.Base.Domain.Repositories;

namespace Hhs.TextNormalizerService.Domain.CustomerDomain.Repositories;

public interface ICustomerConfigurationRepository : IReadOnlyGenericRepository<CustomerConfiguration, Guid>
{
    Task<CustomerConfiguration> FindByUniqueKeysAsync(Guid clientId, CancellationToken cancellationToken = default);
}