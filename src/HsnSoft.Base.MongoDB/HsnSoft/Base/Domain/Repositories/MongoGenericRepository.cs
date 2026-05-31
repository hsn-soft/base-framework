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
using HsnSoft.Base.MultiTenancy;
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

    public ITrackingMongoCollection<TEntity> GetCollection() => _context?.GetCollection<TEntity>().WithReadPreference(ReadPreference.Primary) as ITrackingMongoCollection<TEntity>;

    public IQueryable<TEntity> GetQueryable()
    {
        var query = GetCollection()
            .AsQueryable()
            .AsExpandable();

        return ApplyDataFilters(query);
    }

    public IClientSessionHandle StartSession(ClientSessionOptions options = null, CancellationToken cancellationToken = default) => StartSessionAsync(options, cancellationToken).GetAwaiter().GetResult();

    public async Task<IClientSessionHandle> StartSessionAsync(ClientSessionOptions options = null, CancellationToken cancellationToken = default) => await _context.StartSessionAsync(options, cancellationToken);

    private FilterDefinition<TEntity> BuildSoftDeleteFilter()
    {
        if (!(DataFilter?.IsEnabled<ISoftDelete>() ?? false))
        {
            return Builders<TEntity>.Filter.Empty;
        }

        if (!typeof(ISoftDelete).IsAssignableFrom(typeof(TEntity)))
        {
            return Builders<TEntity>.Filter.Empty;
        }

        return Builders<TEntity>.Filter.Eq(nameof(ISoftDelete.IsDeleted), false);
    }

    private FilterDefinition<TEntity> BuildTenantFilter()
    {
        if (!(DataFilter?.IsEnabled<IMultiTenant>() ?? false))
        {
            return Builders<TEntity>.Filter.Empty;
        }

        if (!typeof(IMultiTenant).IsAssignableFrom(typeof(TEntity)))
        {
            return Builders<TEntity>.Filter.Empty;
        }

        if (CurrentTenant?.IsSystemTenant ?? false)
        {
            return Builders<TEntity>.Filter.Empty;
        }

        var allowedTenantIds = CurrentTenant?.AllowedTenantIds ?? [];
        if (allowedTenantIds.Count == 0)
        {
            return Builders<TEntity>.Filter.Where(_ => false);
        }

        return Builders<TEntity>.Filter.In(nameof(IMultiTenant.TenantId), allowedTenantIds);
    }

    private FilterDefinition<TEntity> BuildGlobalFilter()
    {
        var filters = new List<FilterDefinition<TEntity>>();

        var tenantFilter = BuildTenantFilter();
        var softDeleteFilter = BuildSoftDeleteFilter();

        if (tenantFilter != Builders<TEntity>.Filter.Empty)
            filters.Add(tenantFilter);

        if (softDeleteFilter != Builders<TEntity>.Filter.Empty)
            filters.Add(softDeleteFilter);

        return filters.Count switch
        {
            0 => Builders<TEntity>.Filter.Empty,
            1 => filters[0],
            _ => Builders<TEntity>.Filter.And(filters)
        };
    }

    public async Task<TEntity> GetByIdAsync(IClientSessionHandle session, TKey id, CancellationToken cancellationToken = default)
    {
        var idFilter = Builders<TEntity>.Filter.Eq(doc => doc.Id, id);
        var globalFilter = Builders<TEntity>.Filter.And
        (
            idFilter,
            BuildGlobalFilter()
        );
        var results = await GetCollection()
            .Find(session, globalFilter, new FindOptions { MaxAwaitTime = _findOptions.MaxAwaitTime, MaxTime = _findOptions.MaxTime })
            .Limit(2)
            .ToListAsync(cancellationToken);
        return results.Count switch
        {
            0 => throw new EntityNotFoundException(typeof(TEntity)),
            > 1 => throw new EntityDuplicateException(typeof(TEntity)),
            _ => results[0]
        };
    }

    public override async Task<TResult> GetByIdAsync<TResult>(
        TKey id,
        Expression<Func<TEntity, TResult>> selector,
        Func<IQueryable<TEntity>, IQueryable<TEntity>> includeEntity = null,
        CancellationToken cancellationToken = default)
    {
        var idFilter = Builders<TEntity>.Filter.Eq(doc => doc.Id, id);
        var globalFilter = Builders<TEntity>.Filter.And
        (
            idFilter,
            BuildGlobalFilter()
        );
        var results = await GetCollection()
            .Find(globalFilter, new FindOptions { MaxAwaitTime = _findOptions.MaxAwaitTime, MaxTime = _findOptions.MaxTime })
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
        Func<IQueryable<TEntity>, IQueryable<TEntity>> includeEntity = null,
        CancellationToken cancellationToken = default)
    {
        var idFilter = Builders<TEntity>.Filter.Eq(doc => doc.Id, id);
        var globalFilter = Builders<TEntity>.Filter.And
        (
            idFilter,
            BuildGlobalFilter()
        );
        var results = await GetCollection()
            .Find(globalFilter, new FindOptions { MaxAwaitTime = _findOptions.MaxAwaitTime, MaxTime = _findOptions.MaxTime })
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
        Func<IQueryable<TEntity>, IQueryable<TEntity>> includeEntity = null,
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
        Func<IQueryable<TEntity>, IQueryable<TEntity>> includeEntity = null,
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
        Func<IQueryable<TEntity>, IQueryable<TEntity>> includeEntity = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderByEntity = null,
        CancellationToken cancellationToken = default) => Task.FromResult(QueryGetFirstOrDefault(predicate, orderByEntity).Select(selector).FirstOrDefault());

    public override Task<TResult> GetFirstOrDefaultAsync<TResult>(
        Expression<Func<TEntity, bool>> predicate,
        IConfigurationProvider configuration,
        Func<IQueryable<TEntity>, IQueryable<TEntity>> includeEntity = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderByEntity = null,
        CancellationToken cancellationToken = default) => Task.FromResult(QueryGetFirstOrDefault(predicate, orderByEntity).ProjectTo<TResult>(configuration).FirstOrDefault());

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
        CancellationToken cancellationToken = default) => Task.FromResult(QueryGetList(options).Select(selector).ToList());

    public override Task<List<TResult>> GetListAsync<TResult>(
        ListQueryOptions<TEntity> options,
        IConfigurationProvider configuration,
        CancellationToken cancellationToken = default) => Task.FromResult(QueryGetList(options).ProjectTo<TResult>(configuration).ToList());

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
        if (filter != null)
        {
            return GetQueryable().LongCount(filter);
        }

        var globalFilter = BuildGlobalFilter();

        return await GetCollection()
            .CountDocumentsAsync(
                globalFilter,
                _countOptions,
                cancellationToken);
    }

    public override Task<bool> ExistsAsync(
        Expression<Func<TEntity, bool>> filter,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(GetQueryable().Any(filter));
    }

    public Task InsertAsync(IClientSessionHandle session, TEntity entity, CancellationToken cancellationToken = default) => InsertManyAsync(session, [entity], cancellationToken);

    public async Task InsertManyAsync(IClientSessionHandle session, IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        IEnumerable<TEntity> enumerable = entities.ToList();
        await GetCollection().InsertManyAsync(session, enumerable, cancellationToken: cancellationToken);
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
        var idFilter = Builders<TEntity>.Filter.Eq(doc => doc.Id, id);
        var globalFilter = Builders<TEntity>.Filter.And
        (
            idFilter,
            BuildGlobalFilter()
        );
        var entity = await tmpCollection.Find(globalFilter).FirstOrDefaultAsync(cancellationToken);
        if (entity == null) throw new EntityNotFoundException(typeof(TEntity), id);

        updateAction(entity);

        // GetDbContext().SetEntityEventState([entity], MongoEntityEventState.Modified);
        var replaceResult = await tmpCollection.ReplaceOneAsync(globalFilter, entity, cancellationToken: cancellationToken);
        return !replaceResult.IsAcknowledged ? throw new Exception($"Update error: {replaceResult}") : entity;
    }

    public Task UpdateAsync(IClientSessionHandle session, TEntity entity, CancellationToken cancellationToken = default) => UpdateManyAsync(session, [entity], cancellationToken);

    public async Task UpdateManyAsync(IClientSessionHandle session, IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        var tmpCollection = GetCollection();

        foreach (var entity in entities)
        {
            var filter = Builders<TEntity>.Filter.And(
                Builders<TEntity>.Filter.Eq(
                    x => x.Id,
                    entity.Id),
                BuildGlobalFilter());

            await tmpCollection.ReplaceOneAsync(
                session,
                filter,
                entity,
                new ReplaceOptions
                {
                    IsUpsert = false
                },
                cancellationToken);
        }
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
            var filter = Builders<TEntity>.Filter.And(
                Builders<TEntity>.Filter.Eq(
                    x => x.Id,
                    entity.Id),
                BuildGlobalFilter());

            await tmpCollection.ReplaceOneAsync(
                filter,
                entity,
                new ReplaceOptions
                {
                    IsUpsert = false
                },
                cancellationToken);

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

        var filter = Builders<TEntity>.Filter.And(
            BuildGlobalFilter(),
            Builders<TEntity>.Filter.Where(predicate));

        var result =   await collection.UpdateManyAsync(
            filter,
            update,
            cancellationToken: cancellationToken
        );



        return result.ModifiedCount;
    }

    public async Task<long> UpdateByExpressionAsync(IClientSessionHandle session, Expression<Func<TEntity, bool>> predicate, Func<UpdateDefinitionBuilder<TEntity>, UpdateDefinition<TEntity>> set, CancellationToken cancellationToken = default)
    {
        var collection = GetCollection();

        var builder = Builders<TEntity>.Update;
        var update = set(builder);

        var filter = Builders<TEntity>.Filter.And(
            BuildGlobalFilter(),
            Builders<TEntity>.Filter.Where(predicate));

        var result = await collection.UpdateManyAsync(
            session,
            filter,
            update,
            cancellationToken: cancellationToken
        );

        return result.ModifiedCount;
    }

    public override async Task<int> DeleteByIdListAsync(
        IEnumerable<TKey> ids,
        CancellationToken cancellationToken = default)
    {
      var filter = Builders<TEntity>.Filter.And(
            Builders<TEntity>.Filter.In(
                x => x.Id,
                ids),
            BuildGlobalFilter());

      var result =  await GetCollection().DeleteManyAsync(
            filter,
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