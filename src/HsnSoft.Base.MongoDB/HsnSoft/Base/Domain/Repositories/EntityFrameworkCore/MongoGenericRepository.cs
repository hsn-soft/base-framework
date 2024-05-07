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
using MongoDB.Driver.Linq;

namespace HsnSoft.Base.Domain.Repositories.EntityFrameworkCore;

public class MongoGenericRepository<TDbContext, TEntity, TKey> : GenericRepositoryBase<TEntity, TKey>, IMongoGenericRepository<TDbContext, TEntity, TKey>
    where TDbContext : BaseMongoDbContext
    where TEntity : class, IEntity<TKey>
{
    private readonly TDbContext _dbContext;
    private readonly FindOptions<TEntity> _findOptions;

    public MongoGenericRepository(IServiceProvider provider, TDbContext dbContext) : base(provider)
    {
        _dbContext = dbContext;
        _findOptions = new FindOptions<TEntity>
        {
            MaxAwaitTime = _dbContext.ClientWaitQueueTimeout,
            MaxTime = _dbContext.ClientWaitQueueTimeout
        };
    }

    public TDbContext GetDbContext() => _dbContext;

    public IMongoCollection<TEntity> GetCollection() => _dbContext?.Collection<TEntity>();

    public IMongoQueryable<TEntity> WithDetails()
    {
        return GetQueryable();
    }

    public IMongoQueryable<TEntity> WithDetails(params Expression<Func<TEntity, object>>[] propertySelectors)
    {
        return GetQueryable();
    }

    public IMongoQueryable<TEntity> GetQueryable()
    {
        return GetCollection().AsQueryable();
    }

    public override async Task<TEntity> FindAsync(Expression<Func<TEntity, bool>> predicate, bool includeDetails = true, CancellationToken cancellationToken = default)
    {
        var asyncCursor = await GetCollection().FindAsync(predicate, _findOptions, cancellationToken);

        return asyncCursor != null ? await asyncCursor.FirstOrDefaultAsync(GetCancellationToken(cancellationToken)) : null;
    }

    public override async Task<TEntity> FindAsync(TKey id, bool includeDetails = true, CancellationToken cancellationToken = default)
    {
        var asyncCursor = await GetCollection().FindAsync(Builders<TEntity>.Filter.Eq("_id", id), _findOptions, cancellationToken: GetCancellationToken(cancellationToken));

        return asyncCursor != null ? await asyncCursor.FirstOrDefaultAsync(GetCancellationToken(cancellationToken)) : null;
    }

    public override async Task<List<TEntity>> GetListAsync(Expression<Func<TEntity, bool>> predicate, bool includeDetails = false, CancellationToken cancellationToken = default)
    {
        var asyncCursor = await GetCollection().FindAsync(predicate, _findOptions, cancellationToken: GetCancellationToken(cancellationToken));

        return asyncCursor != null ? await asyncCursor.ToListAsync(GetCancellationToken(cancellationToken)) : null;
    }

    public override async Task<List<TEntity>> GetListAsync(bool includeDetails = false, CancellationToken cancellationToken = default)
    {
        var asyncCursor = await GetCollection().FindAsync(Builders<TEntity>.Filter.Empty, _findOptions, cancellationToken: GetCancellationToken(cancellationToken));

        return asyncCursor != null ? await asyncCursor.ToListAsync(GetCancellationToken(cancellationToken)) : null;
    }

    public override async Task<long> GetCountAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override async Task<List<TEntity>> GetPagedListAsync(
        int skipCount,
        int maxResultCount,
        string sorting,
        bool includeDetails = false,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override async Task<TEntity> InsertAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        CheckAndSetId(entity);

        await _dbContext.AddEntityCommandAsync(async _ =>
        {
            _dbContext.ApplyEntityTrackingChanges(entity, MongoCommandState.Added);
            await GetCollection().InsertOneAsync(entity, new InsertOneOptions { BypassDocumentValidation = false }, GetCancellationToken(cancellationToken));
            return entity;
        });

        await SaveChangesAsync((CancellationToken)GetCancellationToken(cancellationToken));
        return entity;
    }

    public override async Task InsertManyAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override async Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override async Task UpdateManyAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override async Task<bool> DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        if (typeof(ISoftDelete).IsAssignableFrom(typeof(TEntity)))
        {
            // Soft delete
            await DeleteManyAsync(new List<TEntity> { entity }, GetCancellationToken(cancellationToken));
            return true;
        }

        // Hard Delete
        await _dbContext.AddEntityCommandAsync(async _ =>
        {
            var deleteResult = await GetCollection().DeleteOneAsync(Builders<TEntity>.Filter.Eq("_id", entity.Id), GetCancellationToken(cancellationToken));
            return deleteResult.IsAcknowledged ? (int)deleteResult.DeletedCount : 0;
        });

        return await SaveChangesAsync((CancellationToken)GetCancellationToken(cancellationToken)) > 0;
    }

    public override async Task<bool> DeleteAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override async Task DeleteManyAsync(IEnumerable<TKey> ids, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override async Task DeleteManyAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        if (typeof(ISoftDelete).IsAssignableFrom(typeof(TEntity)))
        {
            // Soft delete
            foreach (var entity in entities)
            {
                await _dbContext.AddEntityCommandAsync(async _ =>
                {
                    _dbContext.ApplyEntityTrackingChanges(entity, MongoCommandState.Deleted);
                    var replaceResult = await GetCollection().ReplaceOneAsync(Builders<TEntity>.Filter.Eq("_id", entity.Id),
                        entity, cancellationToken: GetCancellationToken(cancellationToken));
                    return replaceResult.IsAcknowledged ? (int)replaceResult.ModifiedCount : 0;
                });
            }

            await SaveChangesAsync((CancellationToken)GetCancellationToken(cancellationToken));
            return;
        }

        // Hard Delete
        var idList = entities.Select(x => x.Id).ToList();
        await _dbContext.AddEntityCommandAsync(async _ =>
        {
            var deleteResult = await GetCollection().DeleteManyAsync(x => idList.Contains(x.Id),
                cancellationToken: GetCancellationToken(cancellationToken));
            return deleteResult.IsAcknowledged ? (int)deleteResult.DeletedCount : 0;
        });

        await SaveChangesAsync((CancellationToken)GetCancellationToken(cancellationToken));
    }

    protected override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SaveSaveEntityCommandsIfExistChangesAsync();
    }

    protected void CheckAndSetId(TEntity entity)
    {
        if (entity is IEntity<Guid> entityWithGuidId)
        {
            TrySetGuidId(entityWithGuidId);
        }
    }

    protected virtual void TrySetGuidId(IEntity<Guid> entity)
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