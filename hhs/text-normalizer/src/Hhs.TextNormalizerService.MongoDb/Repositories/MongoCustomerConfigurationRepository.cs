using Hhs.TextNormalizerService.Domain.CustomerDomain.Entities;
using Hhs.TextNormalizerService.Domain.CustomerDomain.Repositories;
using Hhs.TextNormalizerService.MongoDb.Context;
using HsnSoft.Base.Domain.Repositories;

namespace Hhs.TextNormalizerService.MongoDb.Repositories;

public sealed class MongoCustomerConfigurationRepository : MongoGenericRepository<CustomerConfiguration, Guid>, ICustomerConfigurationRepository
{
    public MongoCustomerConfigurationRepository(IServiceProvider provider, TextNormalizerServiceDbContext dbContext) :
        base(provider, dbContext)
    {
    }

    public async Task<CustomerConfiguration> FindByUniqueKeysAsync(Guid clientId, CancellationToken cancellationToken = default)
    {
        return await GetSingleOrDefaultAsync(x => x.ClientId == clientId && x.IsDeleted == false, cancellationToken: cancellationToken);
    }
}