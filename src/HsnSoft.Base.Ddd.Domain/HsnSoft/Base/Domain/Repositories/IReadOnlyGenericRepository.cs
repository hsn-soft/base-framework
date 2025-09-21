using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.Domain.Models;
using JetBrains.Annotations;

namespace HsnSoft.Base.Domain.Repositories;

public interface IReadOnlyGenericRepository<TEntity, in TKey> : IRepository where TEntity : class, IEntity<TKey>
{
    Task<TEntity> GetByIdAsync(
        TKey id,
        CancellationToken cancellationToken = default
    );

    Task<TResult> GetByIdAsync<TResult>(
        TKey id,
        Expression<Func<TEntity, TResult>> selector,
        CancellationToken cancellationToken = default
    ) where TResult : class;

    [ItemCanBeNull]
    Task<TEntity> GetByIdOrDefaultAsync(
        TKey id,
        CancellationToken cancellationToken = default
    );

    [ItemCanBeNull]
    Task<TResult> GetByIdOrDefaultAsync<TResult>(
        TKey id,
        Expression<Func<TEntity, TResult>> selector,
        CancellationToken cancellationToken = default
    ) where TResult : class;

    Task<TEntity> GetSingleAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default
    );

    Task<TResult> GetSingleAsync<TResult>(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TResult>> selector,
        CancellationToken cancellationToken = default
    ) where TResult : class;

    [ItemCanBeNull]
    Task<TEntity> GetSingleOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default
    );

    [ItemCanBeNull]
    Task<TResult> GetSingleOrDefaultAsync<TResult>(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TResult>> selector,
        CancellationToken cancellationToken = default
    ) where TResult : class;

    [ItemCanBeNull]
    Task<TEntity> GetFirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        [CanBeNull] Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderByEntity = null,
        CancellationToken cancellationToken = default
    );

    [ItemCanBeNull]
    Task<TResult> GetFirstOrDefaultAsync<TResult>(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TResult>> selector,
        [CanBeNull] Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderByEntity = null,
        CancellationToken cancellationToken = default
    ) where TResult : class;

    Task<List<TEntity>> GetListAsync(
        ListQueryOptions<TEntity> options,
        CancellationToken cancellationToken = default
    );

    Task<List<TResult>> GetListAsync<TResult>(
        ListQueryOptions<TEntity> options,
        Expression<Func<TEntity, TResult>> selector,
        CancellationToken cancellationToken = default
    ) where TResult : class;


    Task<PaginationResult<TEntity>> GetPageListAsync(
        PaginationQueryOptions<TEntity> options,
        CancellationToken cancellationToken = default
    );

    Task<PaginationResult<TResult>> GetPageListAsync<TResult>(
        PaginationQueryOptions<TEntity> options,
        Expression<Func<TEntity, TResult>> selector,
        CancellationToken cancellationToken = default
    ) where TResult : class;

    Task<long> GetCountAsync(
        [CanBeNull] Expression<Func<TEntity, bool>> filter = null,
        CancellationToken cancellationToken = default
    );

    Task<bool> ExistsAsync(
        Expression<Func<TEntity, bool>> filter,
        CancellationToken cancellationToken = default
    );
}