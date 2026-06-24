using Hhs.FeedRService.Domain.ConfigurationDomain.Entities.MongoDB;
using Hhs.FeedRService.Domain.ConfigurationDomain.Repositories;
using Hhs.FeedRService.MongoDb.Context;
using HsnSoft.Base.Domain.Repositories;
using MongoDB.Driver;

namespace Hhs.FeedRService.MongoDb.Repositories;

public sealed class MongoCustomerConfigurationRepository : MongoGenericRepository<CustomerConfiguration, Guid>, ICustomerConfigurationRepository
{
    private readonly FeedRServiceDbContext _dbContext;

    public MongoCustomerConfigurationRepository(IServiceProvider provider, FeedRServiceDbContext dbContext) :
        base(provider, dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CustomerConfiguration> FindByUniqueKeysAsync(Guid clientId, CancellationToken cancellationToken = default)
    {
        return await GetSingleOrDefaultAsync(x => x.ClientId == clientId && x.IsDeleted == false, cancellationToken: cancellationToken); //TODO Check Find Method
    }

    public async Task<List<CustomerConfiguration>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.CustomerConfigurations
            .Find(FilterDefinition<CustomerConfiguration>.Empty)
            .ToListAsync(cancellationToken);
    }

    public new async Task<CustomerConfiguration> InsertAsync(CustomerConfiguration entity, CancellationToken cancellationToken = default)
    {
        await _dbContext.CustomerConfigurations.InsertOneAsync(entity, cancellationToken: cancellationToken);
        return entity;
    }

    public new async Task<CustomerConfiguration> UpdateAsync(CustomerConfiguration entity, CancellationToken cancellationToken = default)
    {
        var filter = Builders<CustomerConfiguration>.Filter.Eq(x => x.Id, entity.Id);
        await _dbContext.CustomerConfigurations.ReplaceOneAsync(filter, entity, cancellationToken: cancellationToken);
        return entity;
    }
}