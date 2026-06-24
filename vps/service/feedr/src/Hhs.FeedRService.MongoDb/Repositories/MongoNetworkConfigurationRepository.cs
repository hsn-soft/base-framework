using Hhs.FeedRService.MongoDb.Context;
using HsnSoft.Base.Domain.Repositories;
using MongoDB.Driver;
using System.Reflection;
using Hhs.FeedRService.Domain.ConfigurationDomain.Entities.MongoDB;
using Hhs.FeedRService.Domain.ConfigurationDomain.Repositories;

namespace Hhs.FeedRService.MongoDb.Repositories;

public sealed class MongoNetworkConfigurationRepository
    : MongoGenericRepository<NetworkConfiguration, Guid>, INetworkConfigurationRepository
{
    private readonly FeedRServiceDbContext _dbContext;
    private IMongoCollection<NetworkConfiguration> _mongoCollection;

    public MongoNetworkConfigurationRepository(IServiceProvider provider, FeedRServiceDbContext dbContext)
        : base(provider, dbContext)
    {
        _dbContext = dbContext;
    }

    private IMongoCollection<NetworkConfiguration> GetRawCollection()
    {
        if (_mongoCollection != null) return _mongoCollection;

        try
        {
            var trackingCollection = _dbContext.NetworkConfigurations;
            var property = trackingCollection.GetType().GetProperty("InnerCollection", BindingFlags.NonPublic | BindingFlags.Instance);
            if (property?.GetValue(trackingCollection) is IMongoCollection<NetworkConfiguration> innerCollection)
            {
                _mongoCollection = innerCollection;
                return _mongoCollection;
            }

            var field = trackingCollection.GetType().GetField("_collection", BindingFlags.NonPublic | BindingFlags.Instance);
            if (field?.GetValue(trackingCollection) is IMongoCollection<NetworkConfiguration> collection)
            {
                _mongoCollection = collection;
                return _mongoCollection;
            }
        }
        catch
        {
        }

        _mongoCollection = _dbContext.NetworkConfigurations;
        return _mongoCollection;
    }

    public async Task<List<NetworkConfiguration>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        var collection = GetRawCollection();
        var filter = Builders<NetworkConfiguration>.Filter.Eq(x => x.IsActive, true);
        return await collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<NetworkConfiguration> GetByNetworkCodeAsync(string networkCode, CancellationToken cancellationToken = default)
    {
        var collection = GetRawCollection();
        var filter = Builders<NetworkConfiguration>.Filter.Eq(x => x.NetworkCode, networkCode);
        return await collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public new async Task<NetworkConfiguration> InsertAsync(NetworkConfiguration entity, CancellationToken cancellationToken = default)
    {
        await GetRawCollection().InsertOneAsync(entity, cancellationToken: cancellationToken);
        return entity;
    }

    public new async Task<NetworkConfiguration> UpdateAsync(NetworkConfiguration entity, CancellationToken cancellationToken = default)
    {
        var collection = GetRawCollection();
        var filter = Builders<NetworkConfiguration>.Filter.Eq(x => x.Id, entity.Id);
        await collection.ReplaceOneAsync(filter, entity, cancellationToken: cancellationToken);
        return entity;
    }
}
