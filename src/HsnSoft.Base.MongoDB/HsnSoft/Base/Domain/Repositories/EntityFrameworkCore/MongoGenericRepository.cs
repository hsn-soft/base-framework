using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.MongoDB;
using HsnSoft.Base.MongoDB.Context;
using MongoDB.Driver;

namespace HsnSoft.Base.Domain.Repositories.EntityFrameworkCore;

public class MongoGenericRepository<TDbContext, TEntity, TKey> : GenericRepositoryBase<TEntity, TKey>, IMongoGenericRepository<TEntity, TKey>
    where TDbContext : BaseMongoDbContext
    where TEntity : class, IEntity<TKey>
{
    private readonly TDbContext _dbContext;
    private readonly FindOptions<TEntity> _findOptions;
    private readonly CountOptions _countOptions;

    public MongoGenericRepository(IServiceProvider provider, TDbContext dbContext) : base(provider)
    {
        _dbContext = dbContext;
        _findOptions = new FindOptions<TEntity>
        {
            MaxAwaitTime = _dbContext.ClientWaitQueueTimeout,
            MaxTime = _dbContext.ClientWaitQueueTimeout
        };
        _countOptions = new CountOptions
        {
            MaxTime = _dbContext.ClientWaitQueueTimeout
        };
    }

    public IMongoCollection<TEntity> GetCollection(TEntity entity = null, MongoEntityEventState eventState = MongoEntityEventState.Unchanged)
        => _dbContext?.Collection(entity, eventState);

    public IMongoCollection<TEntity> GetCollections(IEnumerable<TEntity> entities = null, MongoEntityEventState eventState = MongoEntityEventState.Unchanged)
        => _dbContext?.Collections(entities, eventState);

    public IQueryable<TEntity> WithDetails()
    {
        return GetQueryable();
    }

    public IQueryable<TEntity> WithDetails(params Expression<Func<TEntity, object>>[] propertySelectors)
    {
        return GetQueryable();
    }

    public IQueryable<TEntity> GetQueryable()
    {
        return GetCollection().WithReadPreference(ReadPreference.Primary).AsQueryable();
    }

    public override async Task<TEntity> FindAsync(Expression<Func<TEntity, bool>> predicate, bool includeDetails = true, CancellationToken cancellationToken = default)
    {
        var asyncCursor = await GetCollection().WithReadPreference(ReadPreference.Primary)
            .FindAsync(predicate, _findOptions, cancellationToken);

        var results = await asyncCursor.ToListAsync(GetCancellationToken(cancellationToken));

        if (results is not { Count: > 0 }) return null;
        if (results is { Count: > 1 })
        {
            throw new EntityDuplicateException(typeof(TEntity));
        }

        return results.SingleOrDefault();
    }

    public override async Task<TEntity> FindAsync(TKey id, bool includeDetails = true, CancellationToken cancellationToken = default)
    {
        var filter = Builders<TEntity>.Filter.Eq(doc => doc.Id, id);
        var asyncCursor = await GetCollection().WithReadPreference(ReadPreference.Primary)
            .FindAsync(filter, _findOptions, cancellationToken: GetCancellationToken(cancellationToken));

        var results = await asyncCursor.ToListAsync(GetCancellationToken(cancellationToken));

        if (results is not { Count: > 0 }) return null;
        if (results is { Count: > 1 })
        {
            throw new EntityDuplicateException(typeof(TEntity));
        }

        return results.SingleOrDefault();
    }

    public override async Task<List<TEntity>> GetListAsync(Expression<Func<TEntity, bool>> predicate, bool includeDetails = false, CancellationToken cancellationToken = default)
    {
        var asyncCursor = await GetCollection().WithReadPreference(ReadPreference.Primary)
            .FindAsync(predicate, _findOptions, cancellationToken: GetCancellationToken(cancellationToken));

        return asyncCursor != null ? await asyncCursor.ToListAsync(GetCancellationToken(cancellationToken)) : null;
    }

    public override async Task<List<TEntity>> GetListAsync(bool includeDetails = false, CancellationToken cancellationToken = default)
    {
        var asyncCursor = await GetCollection().WithReadPreference(ReadPreference.Primary)
            .FindAsync(Builders<TEntity>.Filter.Empty, _findOptions, cancellationToken: GetCancellationToken(cancellationToken));

        return asyncCursor != null ? await asyncCursor.ToListAsync(GetCancellationToken(cancellationToken)) : null;
    }

    public override async Task<long> GetCountAsync(CancellationToken cancellationToken = default)
    {
        return await GetCollection().WithReadPreference(ReadPreference.Primary).EstimatedDocumentCountAsync(new EstimatedDocumentCountOptions
        {
            MaxTime = _countOptions.MaxTime
        }, GetCancellationToken(cancellationToken));
    }

    public override async Task<List<TEntity>> GetPagedListAsync(
        int skipCount,
        int maxResultCount,
        string sorting,
        bool includeDetails = false,
        CancellationToken cancellationToken = default)
    {
        var find = GetCollection().WithReadPreference(ReadPreference.Primary)
            .Find(Builders<TEntity>.Filter.Empty, new FindOptions
            {
                MaxAwaitTime = _findOptions.MaxAwaitTime,
                MaxTime = _findOptions.MaxTime
            });

        return find.ToEnumerable().AsQueryable() // GetQueryable()
            .OrderByIf<TEntity, IQueryable<TEntity>>(!sorting.IsNullOrWhiteSpace(), sorting)
            .PageBy(skipCount, maxResultCount)
            .ToList();
    }

    public override async Task<long> GetCountAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await GetCollection().WithReadPreference(ReadPreference.Primary).CountDocumentsAsync(predicate, _countOptions, GetCancellationToken(cancellationToken));
    }

    public override async Task<List<TEntity>> GetPagedListAsync(
        Expression<Func<TEntity, bool>> predicate,
        int skipCount,
        int maxResultCount,
        string sorting,
        bool includeDetails = false,
        CancellationToken cancellationToken = default)
    {
        var query = GetCollection().WithReadPreference(ReadPreference.Primary)
            .Find(predicate, new FindOptions
            {
                MaxAwaitTime = _findOptions.MaxAwaitTime,
                MaxTime = _findOptions.MaxTime
            });

        return query.ToEnumerable().AsQueryable() // GetQueryable()
            .OrderByIf<TEntity, IQueryable<TEntity>>(!sorting.IsNullOrWhiteSpace(), sorting)
            .PageBy(skipCount, maxResultCount)
            .ToList();
    }

    public override async Task<TEntity> InsertAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        CheckAndSetId(entity);

        await GetCollection(entity, MongoEntityEventState.Added).InsertOneAsync(entity, new InsertOneOptions
        {
            BypassDocumentValidation = false
        }, GetCancellationToken(cancellationToken));

        return entity;
    }

    public override async Task InsertManyAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        var enumerable = entities.ToList();
        foreach (var entity in enumerable)
        {
            CheckAndSetId(entity);
        }

        await GetCollections(enumerable, MongoEntityEventState.Added).InsertManyAsync(enumerable, new InsertManyOptions
        {
            BypassDocumentValidation = false
        }, cancellationToken: cancellationToken);
    }

    public override async Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        var filter = Builders<TEntity>.Filter.Eq(doc => doc.Id, entity.Id);
        var replaceResult = await GetCollection(entity, MongoEntityEventState.Modified).ReplaceOneAsync(filter, entity, cancellationToken: GetCancellationToken(cancellationToken));
        if (!replaceResult.IsAcknowledged) throw new Exception($"Update error: {replaceResult}");
        return entity;
    }

    public override async Task UpdateManyAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        var enumerable = entities.ToList();
        var models = PrepareModelsForReplaceMany(enumerable);
        await GetCollections(enumerable, MongoEntityEventState.Modified).BulkWriteAsync(models, new BulkWriteOptions { IsOrdered = false }, GetCancellationToken(cancellationToken));
    }

    private static IEnumerable<WriteModel<TEntity>> PrepareModelsForReplaceMany(IEnumerable<TEntity> entities)
    {
        var models = new List<WriteModel<TEntity>>();
        foreach (var entity in entities)
        {
            var filter = Builders<TEntity>.Filter.Eq(doc => doc.Id, entity.Id);
            models.Add(new ReplaceOneModel<TEntity>(filter, entity));
        }

        return models;
    }

    public override async Task<bool> DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        var filter = Builders<TEntity>.Filter.Eq(doc => doc.Id, entity.Id);
        if (typeof(ISoftDelete).IsAssignableFrom(typeof(TEntity)))
        {
            // Soft delete
            var replaceResult = await GetCollection(entity, MongoEntityEventState.Deleted)
                .ReplaceOneAsync(filter, entity, cancellationToken: GetCancellationToken(cancellationToken));

            return replaceResult.IsAcknowledged && (int)replaceResult.ModifiedCount > 0;
        }

        // Hard Delete
        var deleteResult = await GetCollection().DeleteOneAsync(filter, GetCancellationToken(cancellationToken));

        return deleteResult.IsAcknowledged && (int)deleteResult.DeletedCount > 0;
    }

    public override async Task<bool> DeleteAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var entities = await GetListAsync(predicate, cancellationToken: cancellationToken);
        if (entities is not { Count: > 0 }) return false;
        await DeleteManyAsync(entities, cancellationToken);
        return true;
    }

    public override async Task DeleteManyAsync(IEnumerable<TKey> ids, CancellationToken cancellationToken = default)
    {
        foreach (var id in ids)
        {
            await DeleteAsync(id, cancellationToken: cancellationToken);
        }

        var entities = await GetListAsync(x => ids.Contains(x.Id), cancellationToken: cancellationToken);
        if (entities is not { Count: > 0 }) return;
        await DeleteManyAsync(entities, cancellationToken);
    }

    public override async Task DeleteManyAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        if (typeof(ISoftDelete).IsAssignableFrom(typeof(TEntity)))
        {
            // Soft delete
            var enumerable = entities.ToList();
            var models = PrepareModelsForReplaceMany(enumerable);
            await GetCollections(enumerable, MongoEntityEventState.Deleted).BulkWriteAsync(models, new BulkWriteOptions { IsOrdered = false }, GetCancellationToken(cancellationToken));
            return;
        }

        // Hard Delete
        foreach (var entity in entities)
        {
            await DeleteAsync(entity, cancellationToken: cancellationToken);
        }
    }

    protected override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return 0;
    }

    private void CheckAndSetId(TEntity entity)
    {
        if (entity is IEntity<Guid> entityWithGuidId)
        {
            TrySetGuidId(entityWithGuidId);
        }
    }

    private static void TrySetGuidId(IEntity<Guid> entity)
    {
        if (entity.Id != default)
        {
            return;
        }

        EntityHelper.TrySetId(
            entity,
            Guid.NewGuid,
            true
        );
    }
}