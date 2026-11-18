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
    /// <summary>
    /// Adds the entity and saves it, and gives it back: <b>the same instance</b>, now what the store holds (the key the store made for it, the audit fields, the stamp, the tenant), so a caller that answers with it needs no second read. An entity
    /// whose key is empty (<c>Guid.Empty</c>) gets one from the store; one that has a key keeps it.
    /// </summary>
    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the changes of an entity that <c>FindAsync</c> returned (the entities of a list are not followed), and says what it did: <see cref="WriteOutcome.NotModified"/> when nothing in it differs from what was read (nothing is written, the stamp and the audit fields stay: not an error, the caller decides what
    /// it means) or <see cref="WriteOutcome.Modified"/>. An entity that <c>FindAsync</c> did not return is saved whole, so it is always modified. A record that is not there any more is a <see cref="RepositoryFailure.NotFound"/> (404), one whose stamp
    /// (<c>IHasConcurrencyStamp</c>) is not the stored one a <see cref="RepositoryFailure.ConcurrencyConflict"/> (409), as a <see cref="RepositoryException"/>, in every store.
    /// </summary>
    Task<WriteOutcome> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>Deletes the entity; an <see cref="ISoftDelete"/> entity is marked as deleted and stays. A record that is not there any more is a <see cref="RepositoryFailure.NotFound"/> (404) and one that is not current a <see cref="RepositoryFailure.ConcurrencyConflict"/> (409), as a <see cref="RepositoryException"/>, in every store.</summary>
    Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds all the entities in one go (one round trip, or as few as the store needs): <b>all are added or none</b>. A relational store does it in one transaction; a document store without a transaction adds them in order and, if one fails, removes the ones it
    /// had added (best effort: use a unit of work for a guarantee). The entities are the same instances, saved. See <see cref="AddAsync"/> for the keys.
    /// </summary>
    Task AddManyAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the changes of the entities, in one go, and says how many of them were written: an entity that <c>FindAsync</c> returned is saved by what changed in it (and left alone, <see cref="WriteResult.NotModified"/>, when nothing did), any other (one of a list too) whole.
    /// <b>One record that is not there or not current fails the write</b> with a <see cref="RepositoryException"/> that lists every such record (<see cref="RepositoryException.Ids"/>). A relational store writes nothing then; a document store without a transaction has written the
    /// others (each document is atomic): run it in a unit of work to write all or none.
    /// </summary>
    Task<WriteResult> UpdateManyAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    /// <summary>Deletes the entities, in one go (soft delete as <see cref="DeleteAsync"/>). One record that is not there or not current fails the delete with a <see cref="RepositoryException"/> that lists every such record, with the same guarantee as <see cref="UpdateManyAsync"/>.</summary>
    Task DeleteManyAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes every entity the filter matches without loading them, and says how many there were. An <see cref="ISoftDelete"/> entity is marked as deleted, with its time and user, as <see cref="DeleteAsync"/> does; the audit fields of
    /// the change are written too. What the caller may not see (the deleted, the other tenants') is not matched.
    /// </summary>
    Task<long> DeleteWhereAsync(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default);
}
