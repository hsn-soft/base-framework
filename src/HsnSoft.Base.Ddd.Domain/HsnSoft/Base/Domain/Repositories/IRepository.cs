using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Domain.Entities;

namespace HsnSoft.Base.Domain.Repositories;

/// <summary>
/// Where the entities of one kind are kept. The domain says what it needs; a persistence project says how (<c>EfCoreRepository</c>, <c>MongoRepository</c>). Soft-deleted entities and the entities of tenants the caller
/// may not see are not found by any method unless a method says otherwise. Every change is saved by the method that makes it.
/// </summary>
public interface IRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
{
    Task<TEntity?> FindAsync(TKey id, CancellationToken cancellationToken = default);

    Task<TEntity?> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>Whether an entity matches. <paramref name="ignoreFilters"/> looks at the deleted ones and the other tenants' too: for a value that is unique in the whole store (a user name).</summary>
    Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, bool ignoreFilters = false, CancellationToken cancellationToken = default);

    Task<long> CountAsync(Expression<Func<TEntity, bool>>? filter = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TEntity>> GetListAsync(ListQueryOptions<TEntity> options, CancellationToken cancellationToken = default);

    Task<PagedQueryResult<TEntity>> GetPagedListAsync(PagedQueryOptions<TEntity> options, CancellationToken cancellationToken = default);

    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>Saves the changes of an entity that <c>FindAsync</c> returned.</summary>
    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>Deletes the entity; an <see cref="ISoftDelete"/> entity is marked as deleted and stays.</summary>
    Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);
}
