using Hhs.FeedRService.Domain.ReportingDomain.Entities;
using Hhs.FeedRService.Domain.ReportingDomain.Enums;
using Hhs.FeedRService.Domain.ReportingDomain.Repositories;
using Hhs.FeedRService.MongoDb.Context;
using HsnSoft.Base.Domain.Repositories;
using MongoDB.Driver;
using System.Reflection;

namespace Hhs.FeedRService.MongoDb.Repositories;

public sealed class MongoRawGoogleAdManagerResponseRepository
    : MongoGenericRepository<RawGoogleAdManagerResponse, Guid>, IRawGoogleAdManagerResponseRepository
{
    private readonly FeedRServiceDbContext _dbContext;
    private IMongoCollection<RawGoogleAdManagerResponse> _mongoCollection;

    public MongoRawGoogleAdManagerResponseRepository(IServiceProvider provider, FeedRServiceDbContext dbContext)
        : base(provider, dbContext)
    {
        _dbContext = dbContext;
    }

    private IMongoCollection<RawGoogleAdManagerResponse> GetRawCollection()
    {
        if (_mongoCollection != null) return _mongoCollection;

        try
        {
            // Try to extract underlying MongoDB collection from the ITrackingMongoCollection
            var trackingCollection = _dbContext.RawGoogleAdManagerResponses;
            
            // Get the underlying IMongoCollection via reflection or direct cast
            var property = trackingCollection.GetType().GetProperty("InnerCollection", BindingFlags.NonPublic | BindingFlags.Instance);
            if (property?.GetValue(trackingCollection) is IMongoCollection<RawGoogleAdManagerResponse> innerCollection)
            {
                _mongoCollection = innerCollection;
                return _mongoCollection;
            }
            
            // Fallback: create a new collection reference directly
            // Extract database name from the tracking collection
            var field = trackingCollection.GetType().GetField("_collection", BindingFlags.NonPublic | BindingFlags.Instance);
            if (field?.GetValue(trackingCollection) is IMongoCollection<RawGoogleAdManagerResponse> collection)
            {
                _mongoCollection = collection;
                return _mongoCollection;
            }
        }
        catch
        {
            // If reflection fails, use the tracking collection directly
        }

        // Fallback: use the DbContext collection
        _mongoCollection = _dbContext.RawGoogleAdManagerResponses;
        return _mongoCollection;
    }

    public new async Task<RawGoogleAdManagerResponse> InsertAsync(RawGoogleAdManagerResponse entity, CancellationToken cancellationToken = default)
    {
        await GetRawCollection().InsertOneAsync(entity, cancellationToken: cancellationToken);
        return entity;
    }

    public async Task<List<RawGoogleAdManagerResponse>> GetUnprocessedAsync(int maxCount = 100, CancellationToken cancellationToken = default)
    {
        var collection = GetRawCollection();
        var filter = Builders<RawGoogleAdManagerResponse>.Filter.In(
            x => x.ProcessedStatus,
            new[] { DerivationStatus.Pending, DerivationStatus.Failed });
        
        var list = await collection
            .Find(filter)
            .Limit(maxCount)
            .ToListAsync(cancellationToken);
        
        return list;
    }

    public async Task<RawGoogleAdManagerResponse> GetByRequestIdAsync(string requestId, CancellationToken cancellationToken = default)
    {
        var collection = GetRawCollection();
        var filter = Builders<RawGoogleAdManagerResponse>.Filter.Eq(x => x.RequestId, requestId);
        return await collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task UpdateProcessedStatusAsync(Guid id, DerivationStatus status, string error = null, CancellationToken cancellationToken = default)
    {
        var collection = GetRawCollection();
        var filter = Builders<RawGoogleAdManagerResponse>.Filter.Eq(x => x.Id, id);
        
        var update = status switch
        {
            DerivationStatus.Processing => Builders<RawGoogleAdManagerResponse>.Update
                .Set(x => x.ProcessedStatus, status)
                .Set(x => x.ProcessedAt, DateTime.UtcNow),
            
            DerivationStatus.Completed => Builders<RawGoogleAdManagerResponse>.Update
                .Set(x => x.ProcessedStatus, status)
                .Set(x => x.ProcessedAt, DateTime.UtcNow),
            
            DerivationStatus.Failed => Builders<RawGoogleAdManagerResponse>.Update
                .Set(x => x.ProcessedStatus, status)
                .Set(x => x.ProcessedAt, DateTime.UtcNow)
                .Set(x => x.ProcessingError, error),
            
            _ => Builders<RawGoogleAdManagerResponse>.Update
                .Set(x => x.ProcessedStatus, status)
        };
        
        await collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
    }
}
