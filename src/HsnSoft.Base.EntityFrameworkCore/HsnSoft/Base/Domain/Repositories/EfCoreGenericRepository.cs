using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using EFCore.BulkExtensions;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.Domain.Models;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace HsnSoft.Base.Domain.Repositories;

public class EfCoreGenericRepository<TEntity, TKey>(IServiceProvider provider, DbContext context) :
    GenericRepositoryBase<TEntity, TKey>(provider),
    IEfCoreGenericRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
{
    private readonly DbContext _context = context ?? throw new ArgumentNullException(nameof(context));

    public DbSet<TEntity> GetDbSet() => _context?.Set<TEntity>();
    public IQueryable<TEntity> GetQueryable() => GetDbSet().AsQueryable();

    public override async Task<TResult> GetSingleOrDefaultAsync<TResult>(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TResult>> selector,
        CancellationToken cancellationToken = default)
    {
        var results = await QueryGetSingleOrDefault(predicate).Select(selector).ToListAsync(cancellationToken);
        return results.Count switch
        {
            0 => null,
            > 1 => throw new EntityDuplicateException(typeof(TEntity)),
            _ => results[0]
        };
    }

    public override async Task<TResult> GetSingleOrDefaultAsync<TResult>(
        Expression<Func<TEntity, bool>> predicate,
        IConfigurationProvider configuration,
        CancellationToken cancellationToken = default)
    {
        var results = await QueryGetSingleOrDefault(predicate).ProjectTo<TResult>(configuration).ToListAsync(cancellationToken);
        return results.Count switch
        {
            0 => null,
            > 1 => throw new EntityDuplicateException(typeof(TEntity)),
            _ => results[0]
        };
    }

    private IQueryable<TEntity> QueryGetSingleOrDefault(Expression<Func<TEntity, bool>> predicate) => GetDbSet().Where(predicate).Take(2);

    public override async Task<TResult> GetFirstOrDefaultAsync<TResult>(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TResult>> selector,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderByEntity = null,
        CancellationToken cancellationToken = default)
        => await QueryGetFirstOrDefault(predicate, orderByEntity).Select(selector).FirstOrDefaultAsync(cancellationToken);

    public override async Task<TResult> GetFirstOrDefaultAsync<TResult>(
        Expression<Func<TEntity, bool>> predicate,
        IConfigurationProvider configuration,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderByEntity = null,
        CancellationToken cancellationToken = default)
        => await QueryGetFirstOrDefault(predicate, orderByEntity).ProjectTo<TResult>(configuration).FirstOrDefaultAsync(cancellationToken);

    private IQueryable<TEntity> QueryGetFirstOrDefault(
        Expression<Func<TEntity, bool>> predicate,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderByEntity = null
    )
    {
        IQueryable<TEntity> query = GetDbSet().Where(predicate);
        if (orderByEntity != null) query = orderByEntity(query);
        return query;
    }

    public override async Task<List<TResult>> GetListAsync<TResult>(
        ListQueryOptions<TEntity> options,
        Expression<Func<TEntity, TResult>> selector,
        CancellationToken cancellationToken = default)
        => await QueryGetList(options).Select(selector).ToListAsync(cancellationToken);

    public override async Task<List<TResult>> GetListAsync<TResult>(
        ListQueryOptions<TEntity> options,
        IConfigurationProvider configuration,
        CancellationToken cancellationToken = default)
        => await QueryGetList(options).ProjectTo<TResult>(configuration).ToListAsync(cancellationToken);

    private IQueryable<TEntity> QueryGetList(ListQueryOptions<TEntity> options)
    {
        IQueryable<TEntity> query = GetDbSet();

        query = query.AsNoTracking();
        if (options.Filter != null) query = query.Where(options.Filter);

        if (!string.IsNullOrWhiteSpace(options.OrderByDynamic))
            query = query.OrderBy(options.OrderByDynamic);
        else if (options.OrderByEntity != null)
            query = options.OrderByEntity(query);

        if (options.MaxResultCount.HasValue) query = query.Take(options.MaxResultCount.Value);

        return query;
    }

    public override async Task<PagedQueryResult<TResult>> GetPageListAsync<TResult>(
        PagedQueryOptions<TEntity> options,
        Expression<Func<TEntity, TResult>> selector,
        CancellationToken cancellationToken = default)
    {
        var result = await QueryGetPageListAsync(options, cancellationToken);
        var items = await result.query.Select(selector).ToListAsync(cancellationToken);

        return new PagedQueryResult<TResult> { Items = items, TotalCount = result.totalCount };
    }

    public override async Task<PagedQueryResult<TResult>> GetPageListAsync<TResult>(
        PagedQueryOptions<TEntity> options,
        IConfigurationProvider configuration,
        CancellationToken cancellationToken = default)
    {
        var result = await QueryGetPageListAsync(options, cancellationToken);
        var items = await result.query.ProjectTo<TResult>(configuration).ToListAsync(cancellationToken);

        return new PagedQueryResult<TResult> { Items = items, TotalCount = result.totalCount };
    }

    private async Task<(IQueryable<TEntity> query, long totalCount)> QueryGetPageListAsync(PagedQueryOptions<TEntity> options, CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> query = GetDbSet();

        query = query.AsNoTracking();
        if (options.Filter != null) query = query.Where(options.Filter);

        long totalCount = await query.LongCountAsync(cancellationToken);

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
        IQueryable<TEntity> query = GetDbSet();
        query = query.AsNoTracking();
        if (filter != null) query = query.Where(filter);

        return await query.LongCountAsync(cancellationToken);
    }

    public override async Task<bool> ExistsAsync(
        Expression<Func<TEntity, bool>> filter,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> query = GetDbSet();
        query = query.AsNoTracking().Where(filter);

        return await query.AnyAsync(cancellationToken);
    }

    public override async Task<int> InsertManyAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        await GetDbSet().AddRangeAsync(entities, cancellationToken);
        return await SaveChangesAsync(cancellationToken);
    }

    public override async Task<TEntity> UpdateByIdAsync(
        TKey id,
        Action<TEntity> updateAction,
        CancellationToken cancellationToken = default)
    {
        var entity = await GetDbSet().FindAsync([id], cancellationToken);
        if (entity == null) throw new EntityNotFoundException(typeof(TEntity));

        updateAction(entity);
        _context.Entry(entity).State = EntityState.Modified;
        await SaveChangesAsync(cancellationToken);

        return entity;
    }

    public async Task<int> UpdateByExpressionAsync(
        Expression<Func<TEntity, bool>> predicate,
        Action<UpdateSettersBuilder<TEntity>> setPropertyCalls,
        CancellationToken cancellationToken = default)
    {
        return await GetDbSet()
            .Where(predicate)
            .ExecuteUpdateAsync(setPropertyCalls, cancellationToken);
    }

    public override async Task<int> UpdateManyAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        var tmpDbSet = GetDbSet();
        foreach (var entity in entities)
        {
            var local = tmpDbSet.Local.FirstOrDefault(e => EqualityComparer<TKey>.Default.Equals(e.Id, entity.Id))
                        ?? await tmpDbSet.FindAsync([entity.Id], cancellationToken)
                        ?? throw new EntityNotFoundException(typeof(TEntity), entity.Id);
            _context.Entry(local).State = EntityState.Detached;

            tmpDbSet.Attach(entity);
            _context.Entry(entity).State = EntityState.Modified;
        }

        return await SaveChangesAsync(cancellationToken);
    }

    public override async Task<int> DeleteByIdListAsync(
        IEnumerable<TKey> ids,
        CancellationToken cancellationToken = default)
    {
        var tmpDbSet = GetDbSet();
        foreach (var id in ids)
        {
            var entity = tmpDbSet.Local.FirstOrDefault(e => EqualityComparer<TKey>.Default.Equals(e.Id, id))
                         ?? await tmpDbSet.FindAsync([id], cancellationToken)
                         ?? throw new EntityNotFoundException(typeof(TEntity), id);

            if (_context.Entry(entity).State == EntityState.Detached) tmpDbSet.Attach(entity);
            tmpDbSet.Remove(entity);
        }

        return await SaveChangesAsync(cancellationToken);
    }

    public override async Task<int> DeleteManyAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        var entities = await GetDbSet().Where(predicate).ToListAsync(cancellationToken);
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

    private Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);

    #region Raw SQL

    public virtual async Task<int> ExecuteSqlAsync(
        string sql, object[] parameters = null,
        CancellationToken cancellationToken = default)
        => await _context.Database.ExecuteSqlRawAsync(sql, parameters ?? [], cancellationToken);

    public virtual IQueryable<TEntity> FromSql(string sql, params object[] parameters)
        => _context.Set<TEntity>().FromSqlRaw(sql, parameters);

    #endregion

    public async Task BulkInsertAsync(IEnumerable<TEntity> entities, Action<BulkConfig> configAction = null, CancellationToken cancellationToken = default)
    {
        var defaultConfig = new BulkConfig();
        ApplyDefaults(defaultConfig, configAction);

        if (defaultConfig.UseTempDB) // use transaction for tempDb
        {
            await using var trx = await _context.Database.BeginTransactionAsync(cancellationToken);
            await _context.BulkInsertAsync(entities, cfg => ApplyDefaults(cfg, configAction), cancellationToken: cancellationToken);
            await trx.CommitAsync(cancellationToken);
            return;
        }

        await _context.BulkInsertAsync(entities, cfg => ApplyDefaults(cfg, configAction), cancellationToken: cancellationToken);
    }

    public async Task BulkUpdateAsync(IEnumerable<TEntity> entities, Action<BulkConfig> configAction = null, CancellationToken cancellationToken = default)
    {
        var defaultConfig = new BulkConfig();
        ApplyDefaults(defaultConfig, configAction);

        if (defaultConfig.UseTempDB) // use transaction for tempDb
        {
            await using var trx = await _context.Database.BeginTransactionAsync(cancellationToken);
            await _context.BulkUpdateAsync(entities, cfg => ApplyDefaults(cfg, configAction), cancellationToken: cancellationToken);
            await trx.CommitAsync(cancellationToken);
            return;
        }

        await _context.BulkUpdateAsync(entities, cfg => ApplyDefaults(cfg, configAction), cancellationToken: cancellationToken);
    }

    public async Task BulkDeleteAsync(IEnumerable<TEntity> entities, Action<BulkConfig> configAction = null, CancellationToken cancellationToken = default)
    {
        var defaultConfig = new BulkConfig();
        ApplyDefaults(defaultConfig, configAction);

        if (defaultConfig.UseTempDB) // use transaction for tempDb
        {
            await using var trx = await _context.Database.BeginTransactionAsync(cancellationToken);
            await _context.BulkDeleteAsync(entities, defaultConfig, cancellationToken: cancellationToken);
            await trx.CommitAsync(cancellationToken);
            return;
        }

        await _context.BulkDeleteAsync(entities, defaultConfig, cancellationToken: cancellationToken);
    }

    public async Task BulkMergeAsync(IEnumerable<TEntity> entities, Action<BulkConfig> configAction = null, CancellationToken cancellationToken = default)
    {
        var defaultConfig = new BulkConfig();
        ApplyDefaults(defaultConfig, configAction);

        if (defaultConfig.UseTempDB) // use transaction for tempDb
        {
            await using var trx = await _context.Database.BeginTransactionAsync(cancellationToken);
            await _context.BulkInsertOrUpdateAsync(entities, cfg => ApplyDefaults(cfg, configAction), cancellationToken: cancellationToken);
            await trx.CommitAsync(cancellationToken);
            return;
        }

        await _context.BulkInsertOrUpdateAsync(entities, cfg => ApplyDefaults(cfg, configAction), cancellationToken: cancellationToken);
    }

    public async Task BulkSyncAsync(IEnumerable<TEntity> entities, Action<BulkConfig> configAction = null, CancellationToken cancellationToken = default)
    {
        var defaultConfig = new BulkConfig();
        ApplyDefaults(defaultConfig, configAction);

        if (defaultConfig.UseTempDB) // use transaction for tempDb
        {
            await using var trx = await _context.Database.BeginTransactionAsync(cancellationToken);
            await _context.BulkInsertOrUpdateOrDeleteAsync(entities, cfg => ApplyDefaults(cfg, configAction), cancellationToken: cancellationToken);
            await trx.CommitAsync(cancellationToken);
            return;
        }

        await _context.BulkInsertOrUpdateOrDeleteAsync(entities, cfg => ApplyDefaults(cfg, configAction), cancellationToken: cancellationToken);
    }

    public async Task BulkReadAsync(IEnumerable<TEntity> entities, Action<BulkConfig> configAction = null, CancellationToken cancellationToken = default)
    {
        var defaultConfig = new BulkConfig();
        ApplyDefaults(defaultConfig, configAction);

        if (defaultConfig.UseTempDB) // use transaction for tempDb
        {
            await using var trx = await _context.Database.BeginTransactionAsync(cancellationToken);
            await _context.BulkReadAsync(entities, cfg => ApplyDefaults(cfg, configAction), cancellationToken: cancellationToken);
            await trx.CommitAsync(cancellationToken);
            return;
        }

        await _context.BulkReadAsync(entities, cfg => ApplyDefaults(cfg, configAction), cancellationToken: cancellationToken);
    }

    public async Task BulkTruncateAsync(Action<BulkConfig> configAction = null, CancellationToken cancellationToken = default)
    {
        var defaultConfig = new BulkConfig();
        ApplyDefaults(defaultConfig, configAction);

        if (defaultConfig.UseTempDB) // use transaction for tempDb
        {
            await using var trx = await _context.Database.BeginTransactionAsync(cancellationToken);
            await _context.TruncateAsync<TEntity>(cfg => ApplyDefaults(cfg, configAction), cancellationToken: cancellationToken);
            await trx.CommitAsync(cancellationToken);
            return;
        }

        await _context.TruncateAsync<TEntity>(cfg => ApplyDefaults(cfg, configAction), cancellationToken: cancellationToken);
    }

    private static void ApplyDefaults(BulkConfig bulkConfig, [CanBeNull] Action<BulkConfig> userConfig)
    {
        bulkConfig.BatchSize = 5000;
        bulkConfig.UseTempDB = true;
        bulkConfig.PreserveInsertOrder = false;
        bulkConfig.SetOutputIdentity = true;
        userConfig?.Invoke(bulkConfig);
    }
}