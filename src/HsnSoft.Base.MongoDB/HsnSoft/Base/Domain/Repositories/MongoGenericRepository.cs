using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.Domain.Models;
using HsnSoft.Base.MongoDB;
using HsnSoft.Base.MongoDB.Context;
using LinqKit.Core;
using MongoDB.Driver;

namespace HsnSoft.Base.Domain.Repositories;

public class MongoGenericRepository<TEntity, TKey> :
    GenericRepositoryBase<TEntity, TKey>,
    IMongoGenericRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
{
    private readonly BaseMongoDbContext _context;
    private readonly FindOptions<TEntity> _findOptions;
    private readonly CountOptions _countOptions;

    public MongoGenericRepository(IServiceProvider provider, BaseMongoDbContext context) : base(provider)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _findOptions = new FindOptions<TEntity> { MaxAwaitTime = _context.ClientWaitQueueTimeout, MaxTime = _context.ClientWaitQueueTimeout };
        _countOptions = new CountOptions { MaxTime = _context.ClientWaitQueueTimeout };
    }

    public ITrackingMongoCollection<TEntity> GetCollection()
        => _context?.GetCollection<TEntity>().WithReadPreference(ReadPreference.Primary) as ITrackingMongoCollection<TEntity>;

    public IQueryable<TEntity> GetQueryable() => GetCollection().AsQueryable().AsExpandable();

    public override async Task<TResult> GetByIdAsync<TResult>(
        TKey id,
        Expression<Func<TEntity, TResult>> selector,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<TEntity>.Filter.Eq(doc => doc.Id, id);
        var results = await GetCollection()
            .Find(filter, new FindOptions { MaxAwaitTime = _findOptions.MaxAwaitTime, MaxTime = _findOptions.MaxTime })
            .Limit(2)
            .Project(selector)
            .ToListAsync(cancellationToken);
        return results.Count switch
        {
            0 => throw new EntityNotFoundException(typeof(TEntity)),
            > 1 => throw new EntityDuplicateException(typeof(TEntity)),
            _ => results[0]
        };
    }

    public override async Task<TResult> GetByIdOrDefaultAsync<TResult>(
        TKey id,
        Expression<Func<TEntity, TResult>> selector,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<TEntity>.Filter.Eq(doc => doc.Id, id);
        var results = await GetCollection()
            .Find(filter, new FindOptions { MaxAwaitTime = _findOptions.MaxAwaitTime, MaxTime = _findOptions.MaxTime })
            .Limit(2)
            .Project(selector)
            .ToListAsync(cancellationToken);
        return results.Count switch
        {
            0 => null,
            > 1 => throw new EntityDuplicateException(typeof(TEntity)),
            _ => results[0]
        };
    }

    public override Task<TResult> GetSingleOrDefaultAsync<TResult>(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TResult>> selector,
        CancellationToken cancellationToken = default)
    {
        var results = QueryGetSingleOrDefault(predicate).Select(selector).ToList();
        return Task.FromResult(results.Count switch
        {
            0 => null,
            > 1 => throw new EntityDuplicateException(typeof(TEntity)),
            _ => results[0]
        });
    }

    public override Task<TResult> GetSingleOrDefaultAsync<TResult>(
        Expression<Func<TEntity, bool>> predicate,
        IConfigurationProvider configuration,
        CancellationToken cancellationToken = default)
    {
        var results = QueryGetSingleOrDefault(predicate).ProjectTo<TResult>(configuration).ToList();
        return Task.FromResult(results.Count switch
        {
            0 => null,
            > 1 => throw new EntityDuplicateException(typeof(TEntity)),
            _ => results[0]
        });
    }

    private IQueryable<TEntity> QueryGetSingleOrDefault(Expression<Func<TEntity, bool>> predicate) => GetQueryable().Where(predicate).Take(2);

    public override Task<TResult> GetFirstOrDefaultAsync<TResult>(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TResult>> selector,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderByEntity = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(QueryGetFirstOrDefault(predicate, orderByEntity).Select(selector).FirstOrDefault());

    public override Task<TResult> GetFirstOrDefaultAsync<TResult>(
        Expression<Func<TEntity, bool>> predicate,
        IConfigurationProvider configuration,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderByEntity = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(QueryGetFirstOrDefault(predicate, orderByEntity).ProjectTo<TResult>(configuration).FirstOrDefault());

    private IQueryable<TEntity> QueryGetFirstOrDefault(
        Expression<Func<TEntity, bool>> predicate,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderByEntity = null
    )
    {
        var query = GetQueryable().Where(predicate);
        if (orderByEntity != null) query = orderByEntity(query);
        return query;
    }

    public override Task<List<TResult>> GetListAsync<TResult>(
        ListQueryOptions<TEntity> options,
        Expression<Func<TEntity, TResult>> selector,
        CancellationToken cancellationToken = default)
        => Task.FromResult(QueryGetList(options).Select(selector).ToList());

    public override Task<List<TResult>> GetListAsync<TResult>(
        ListQueryOptions<TEntity> options,
        IConfigurationProvider configuration,
        CancellationToken cancellationToken = default)
        => Task.FromResult(QueryGetList(options).ProjectTo<TResult>(configuration).ToList());

    private IQueryable<TEntity> QueryGetList(ListQueryOptions<TEntity> options)
    {
        var query = GetQueryable();

        if (options.Filter != null) query = query.Where(options.Filter);

        if (!string.IsNullOrWhiteSpace(options.OrderByDynamic))
            query = query.OrderBy(options.OrderByDynamic);
        else if (options.OrderByEntity != null)
            query = options.OrderByEntity(query);

        if (options.MaxResultCount.HasValue) query = query.Take(options.MaxResultCount.Value);

        return query;
    }

    public override Task<PagedQueryResult<TResult>> GetPageListAsync<TResult>(
        PagedQueryOptions<TEntity> options,
        Expression<Func<TEntity, TResult>> selector,
        CancellationToken cancellationToken = default)
    {
        var result = QueryGetPageList(options);
        var items = result.query.Select(selector).ToList();

        return Task.FromResult(new PagedQueryResult<TResult> { Items = items, TotalCount = result.totalCount });
    }

    public override Task<PagedQueryResult<TResult>> GetPageListAsync<TResult>(
        PagedQueryOptions<TEntity> options,
        IConfigurationProvider configuration,
        CancellationToken cancellationToken = default)
    {
        var result = QueryGetPageList(options);
        var items = result.query.ProjectTo<TResult>(configuration).ToList();

        return Task.FromResult(new PagedQueryResult<TResult> { Items = items, TotalCount = result.totalCount });
    }

    private (IQueryable<TEntity> query, long totalCount) QueryGetPageList(PagedQueryOptions<TEntity> options)
    {
        var query = GetQueryable();

        if (options.Filter != null) query = query.Where(options.Filter);

        int totalCount = query.Count();

        if (!string.IsNullOrWhiteSpace(options.OrderByDynamic))
            query = query.OrderBy(options.OrderByDynamic);
        else if (options.OrderByEntity != null)
            query = options.OrderByEntity(query);

        if (options.PageNumber > 1)
        {
            query = query.Skip((options.PageNumber - 1) * options.MaxResultCount)
                .Take(options.MaxResultCount);
        }
        else
        {
            query = query.Take(options.MaxResultCount);
        }

        return (query, totalCount);
    }

    public override async Task<long> GetCountAsync(
        Expression<Func<TEntity, bool>> filter = null,
        CancellationToken cancellationToken = default)
    {
        return filter == null
            ? await GetCollection().CountDocumentsAsync(_ => true, _countOptions, cancellationToken: cancellationToken)
            : await Task.FromResult(GetQueryable().Count(filter));
    }

    public override Task<bool> ExistsAsync(
        Expression<Func<TEntity, bool>> filter,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(GetQueryable().Any(filter));
    }

    public override async Task<int> InsertManyAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        // GetDbContext().SetEntityEventState(entities, MongoEntityEventState.Added);
        IEnumerable<TEntity> enumerable = entities.ToList();
        await GetCollection().InsertManyAsync(enumerable, cancellationToken: cancellationToken);
        return enumerable.Count();
    }

    public override async Task<TEntity> UpdateByIdAsync(
        TKey id,
        Action<TEntity> updateAction,
        CancellationToken cancellationToken = default)
    {
        var tmpCollection = GetCollection();
        var filter = Builders<TEntity>.Filter.Eq(doc => doc.Id, id);
        var entity = await tmpCollection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        if (entity == null) throw new EntityNotFoundException(typeof(TEntity), id);

        updateAction(entity);

        // GetDbContext().SetEntityEventState([entity], MongoEntityEventState.Modified);
        var replaceResult = await tmpCollection.ReplaceOneAsync(filter, entity, cancellationToken: cancellationToken);
        return !replaceResult.IsAcknowledged ? throw new Exception($"Update error: {replaceResult}") : entity;
    }

    public override async Task<int> UpdateManyAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        int updated = 0;
        var tmpCollection = GetCollection();

        // GetDbContext().SetEntityEventState(entities, MongoEntityEventState.Modified);
        foreach (var entity in entities)
        {
            var result = await tmpCollection.ReplaceOneAsync(
                x => x.Id.Equals(entity.Id),
                entity,
                new ReplaceOptions { IsUpsert = false },
                cancellationToken);

            if (result.ModifiedCount == 0)
                throw new EntityNotFoundException(typeof(TEntity), entity.Id);

            updated++;
        }

        return updated;
    }

    public async Task<long> UpdateByExpressionAsync(
        Expression<Func<TEntity, bool>> predicate,
        Func<UpdateDefinitionBuilder<TEntity>, UpdateDefinition<TEntity>> set,
        CancellationToken cancellationToken = default)
    {
        var collection = GetCollection();

        var builder = Builders<TEntity>.Update;
        var update = set(builder);

        var result = await collection.UpdateManyAsync(
            predicate,
            update,
            cancellationToken: cancellationToken
        );

        return result.ModifiedCount;
    }

    public override async Task<int> DeleteByIdListAsync(
        IEnumerable<TKey> ids,
        CancellationToken cancellationToken = default)
    {
        var result = await GetCollection().DeleteManyAsync(
            x => ids.Contains(x.Id),
            cancellationToken);

        if (result.DeletedCount == 0)
            throw new EntityNotFoundException(typeof(TEntity));

        return (int)result.DeletedCount;
    }

    public override async Task<int> DeleteManyAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        var entities = await GetListAsync(new ListQueryOptions<TEntity> { Filter = predicate }, cancellationToken);
        if (entities.Count == 0) throw new EntityNotFoundException(typeof(TEntity));
        return await DeleteManyAsync(entities, cancellationToken);
    }

    public override async Task<int> DeleteManyAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        var ids = entities.Select(x => x.Id).ToList();
        return await DeleteByIdListAsync(ids, cancellationToken);
    }
}