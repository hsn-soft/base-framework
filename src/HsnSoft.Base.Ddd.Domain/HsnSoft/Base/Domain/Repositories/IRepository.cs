using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Domain.Entities;

namespace HsnSoft.Base.Domain.Repositories;

/// <summary>
/// What every store can do to read the entities of one kind. Soft-deleted entities and the entities of tenants the caller may not see are not found by any method unless a method says otherwise. The methods that end in a DTO
/// (<c>FindAsync&lt;TDto&gt;</c>, ...) read only what the DTO has: the select is made from the DTO (<see cref="EntityProjection.For{TEntity, TDto}"/>) or given as <c>selector</c>.
/// </summary>
public interface IReadOnlyRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
{
    Task<TEntity?> FindAsync(TKey id, CancellationToken cancellationToken = default);

    Task<TEntity?> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    Task<TDto?> FindAsync<TDto>(TKey id, Expression<Func<TEntity, TDto>>? selector = null, CancellationToken cancellationToken = default)
        where TDto : class;

    /// <summary>Whether an entity matches. <paramref name="ignoreFilters"/> looks at the deleted ones and the other tenants' too: for a value that is unique in the whole store (a user name).</summary>
    Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, bool ignoreFilters = false, CancellationToken cancellationToken = default);

    Task<long> CountAsync(Expression<Func<TEntity, bool>>? filter = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TEntity>> GetListAsync(ListQueryOptions<TEntity> options, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TDto>> GetListAsync<TDto>(ListQueryOptions<TEntity> options, Expression<Func<TEntity, TDto>>? selector = null, CancellationToken cancellationToken = default)
        where TDto : class;

    Task<PagedQueryResult<TEntity>> GetPagedListAsync(PagedQueryOptions<TEntity> options, CancellationToken cancellationToken = default);

    Task<PagedQueryResult<TDto>> GetPagedListAsync<TDto>(PagedQueryOptions<TEntity> options, Expression<Func<TEntity, TDto>>? selector = null, CancellationToken cancellationToken = default)
        where TDto : class;
}

/// <summary>
/// Where the entities of one kind are kept: reading (<see cref="IReadOnlyRepository{TEntity, TKey}"/>) and writing. The domain says what it needs; a persistence project says how (<c>EfCoreRepository</c>, <c>MongoRepository</c>). Every
/// change is saved by the method that makes it. What only one kind of store can do is on the interface of that store (<c>IEfCoreRepository</c>, <c>IMongoRepository</c>), which extends this one.
/// </summary>
public interface IRepository<TEntity, TKey> : IReadOnlyRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
{
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>Saves the changes of an entity that <c>FindAsync</c> returned.</summary>
    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>Deletes the entity; an <see cref="ISoftDelete"/> entity is marked as deleted and stays.</summary>
    Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);
}
