using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.Domain.Repositories;

namespace HsnSoft.Base.EntityFrameworkCore;

/// <summary>
/// <see cref="IRepository{TEntity, TKey}"/> and what only a relational store does. <b>Nothing is joined unless it is asked for:</b> the methods with an <c>include</c> load the entity with what that says
/// (<c>query =&gt; query.Include(order =&gt; order.Lines)</c>); the methods of a DTO read the columns the DTO has and join what the DTO reaches, and need no include.
/// </summary>
public interface IEfCoreRepository<TEntity, TKey> : IRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
{
    /// <summary>The entity with what <paramref name="include"/> loads with it, tracked (so <c>UpdateAsync</c> saves its changes).</summary>
    Task<TEntity?> FindAsync(TKey id, Func<IQueryable<TEntity>, IQueryable<TEntity>> include, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TEntity>> GetListAsync(ListQueryOptions<TEntity> options, Func<IQueryable<TEntity>, IQueryable<TEntity>> include, CancellationToken cancellationToken = default);

    Task<PagedQueryResult<TEntity>> GetPagedListAsync(PagedQueryOptions<TEntity> options, Func<IQueryable<TEntity>, IQueryable<TEntity>> include, CancellationToken cancellationToken = default);
}
