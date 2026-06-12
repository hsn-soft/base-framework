using Hhs.VideoGeneratorService.Domain.CustomerDomain.Entities;
using Hhs.VideoGeneratorService.Domain.CustomerDomain.Repositories;
using Hhs.VideoGeneratorService.MongoDb.Context;
using HsnSoft.Base.Domain.Repositories;

namespace Hhs.VideoGeneratorService.MongoDb.Repositories;

public sealed class MongoCustomerConfigurationRepository : MongoGenericRepository<CustomerConfiguration, Guid>, ICustomerConfigurationRepository
{
    public MongoCustomerConfigurationRepository(IServiceProvider provider, VideoGeneratorServiceDbContext dbContext) :
        base(provider, dbContext)
    {
    }

    public async Task<CustomerConfiguration> FindByUniqueKeysAsync(Guid clientId, CancellationToken cancellationToken = default)
    {
        return await GetSingleOrDefaultAsync(x => x.ClientId == clientId && x.IsDeleted == false, cancellationToken: cancellationToken); //TODO Check Find Method
    }
}