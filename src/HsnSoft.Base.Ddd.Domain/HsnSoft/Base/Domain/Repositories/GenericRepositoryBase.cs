using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Data;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.Domain.Models;
using HsnSoft.Base.MultiTenancy;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.Domain.Repositories;

public abstract class GenericRepositoryBase<TEntity, TKey>(IServiceProvider provider = null) : IGenericRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
{
    [CanBeNull] private IDataFilter DataFilter { get; } = provider?.GetService<IDataFilter>();

    [CanBeNull] private ICurrentTenant CurrentTenant { get; } = provider?.GetService<ICurrentTenant>();

    #region GetById / Single / First

    public Task<TEntity> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
        => GetByIdAsync(id, s => s, cancellationToken);

    public virtual Task<TResult> GetByIdAsync<TResult>(
        TKey id,
        Expression<Func<TEntity, TResult>> selector,
        CancellationToken cancellationToken = default) where TResult : class
        => GetSingleAsync(e => EqualityComparer<TKey>.Default.Equals(e.Id, id), selector, cancellationToken);

    public Task<TEntity> GetByIdOrDefaultAsync(TKey id, CancellationToken cancellationToken = default)
        => GetByIdOrDefaultAsync(id, s => s, cancellationToken);

    public virtual Task<TResult> GetByIdOrDefaultAsync<TResult>(
        TKey id,
        Expression<Func<TEntity, TResult>> selector,
        CancellationToken cancellationToken = default) where TResult : class
        => GetSingleOrDefaultAsync(e => EqualityComparer<TKey>.Default.Equals(e.Id, id), selector, cancellationToken);

    public Task<TEntity> GetSingleAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        => GetSingleAsync(predicate, s => s, cancellationToken);

    public async Task<TResult> GetSingleAsync<TResult>(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TResult>> selector,
        CancellationToken cancellationToken = default) where TResult : class
    {
        var entity = await GetSingleOrDefaultAsync(predicate, selector, cancellationToken);
        return entity ?? throw new EntityNotFoundException(typeof(TEntity));
    }

    public Task<TEntity> GetSingleOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
        => GetSingleOrDefaultAsync(predicate, s => s, cancellationToken);

    public abstract Task<TResult> GetSingleOrDefaultAsync<TResult>(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TResult>> selector,
        CancellationToken cancellationToken = default) where TResult : class;

    public Task<TEntity> GetFirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderByEntity = null,
        CancellationToken cancellationToken = default)
        => GetFirstOrDefaultAsync(predicate, s => s, orderByEntity, cancellationToken);

    public abstract Task<TResult> GetFirstOrDefaultAsync<TResult>(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TResult>> selector,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderByEntity = null,
        CancellationToken cancellationToken = default) where TResult : class;

    #endregion

    #region List / Paging / Count

    public Task<List<TEntity>> GetListAsync(
        ListQueryOptions<TEntity> options,
        CancellationToken cancellationToken = default)
        => GetListAsync(options, s => s, cancellationToken);

    public abstract Task<List<TResult>> GetListAsync<TResult>(
        ListQueryOptions<TEntity> options,
        Expression<Func<TEntity, TResult>> selector,
        CancellationToken cancellationToken = default) where TResult : class;

    public Task<PagedQueryResult<TEntity>> GetPageListAsync(
        PagedQueryOptions<TEntity> options,
        CancellationToken cancellationToken = default)
        => GetPageListAsync(options, s => s, cancellationToken);

    public abstract Task<PagedQueryResult<TResult>> GetPageListAsync<TResult>(
        PagedQueryOptions<TEntity> options,
        Expression<Func<TEntity, TResult>> selector,
        CancellationToken cancellationToken = default) where TResult : class;

    public abstract Task<long> GetCountAsync(
        Expression<Func<TEntity, bool>> filter = null,
        CancellationToken cancellationToken = default);

    public abstract Task<bool> ExistsAsync(
        Expression<Func<TEntity, bool>> filter,
        CancellationToken cancellationToken = default);

    #endregion

    #region Insert / Update / Delete

    public Task<int> InsertAsync(TEntity entity, CancellationToken cancellationToken = default)
        => InsertManyAsync([entity], cancellationToken);

    public abstract Task<int> InsertManyAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    public Task<int> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
        => UpdateManyAsync([entity], cancellationToken);

    public abstract Task<int> UpdateManyAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    public abstract Task<TEntity> UpdateByIdAsync(TKey id, Action<TEntity> updateAction, CancellationToken cancellationToken = default);

    public Task<int> DeleteByIdAsync(TKey id, CancellationToken cancellationToken = default)
        => DeleteByIdListAsync([id], cancellationToken);

    public abstract Task<int> DeleteByIdListAsync(IEnumerable<TKey> ids, CancellationToken cancellationToken = default);

    public Task<int> DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
        => DeleteManyAsync([entity], cancellationToken);

    public abstract Task<int> DeleteManyAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
    public abstract Task<int> DeleteManyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    #endregion

    #region Filter Functions

    protected TQueryable ApplyDataFilters<TQueryable>(TQueryable query) where TQueryable : IQueryable<TEntity>
    {
        return ApplyDataFilters<TQueryable, TEntity>(query);
    }

    protected virtual TQueryable ApplyDataFilters<TQueryable, TOtherEntity>(TQueryable query) where TQueryable : IQueryable<TOtherEntity>
    {
        if (typeof(ISoftDelete).IsAssignableFrom(typeof(TOtherEntity)))
        {
            query = (TQueryable)query.WhereIf(DataFilter?.IsEnabled<ISoftDelete>() ?? false, e => ((ISoftDelete)e).IsDeleted == false);
        }

        if (typeof(IMultiTenant).IsAssignableFrom(typeof(TOtherEntity)))
        {
            var tenantId = CurrentTenant?.Id;
            query = (TQueryable)query.WhereIf(DataFilter?.IsEnabled<IMultiTenant>() ?? false, e => ((IMultiTenant)e).TenantId == tenantId);
        }

        return query;
    }

    #endregion
}