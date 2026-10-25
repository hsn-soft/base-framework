using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Domain.Sorting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.EntityFrameworkCore;

/// <summary>
/// <see cref="IRepository{TEntity, TKey}"/> for a relational store: the queries of a service's entity, written once. The query filters of the context (soft delete, tenants: <see cref="BaseDbContext"/>) apply to every
/// method but <c>AnyAsync(..., ignoreFilters: true)</c>. A service that needs more derives from it and adds its own methods (<c>class EfCoreUserRepository(IdentityDbContext db) : EfCoreRepository&lt;IdentityDbContext, User, Guid&gt;(db), IUserRepository</c>).
/// </summary>
public class EfCoreRepository<TContext, TEntity, TKey>(TContext db) : IRepository<TEntity, TKey>
    where TContext : DbContext
    where TEntity : class, IEntity<TKey>
{
    protected TContext Db { get; } = db;

    protected DbSet<TEntity> Set => Db.Set<TEntity>();

    public Task<TEntity?> FindAsync(TKey id, CancellationToken cancellationToken = default)
        => Set.FirstOrDefaultAsync(IdEquals(id), cancellationToken);

    public Task<TEntity?> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return Set.FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, bool ignoreFilters = false, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return (ignoreFilters ? Set.IgnoreQueryFilters() : Set).AnyAsync(predicate, cancellationToken);
    }

    public Task<long> CountAsync(Expression<Func<TEntity, bool>>? filter = null, CancellationToken cancellationToken = default)
        => (filter is null ? Set : Set.Where(filter)).LongCountAsync(cancellationToken);

    public async Task<IReadOnlyList<TEntity>> GetListAsync(ListQueryOptions<TEntity> options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        IQueryable<TEntity> query = Sorted(Narrowed(options), options);
        if (options.MaxResultCount is { } limit)
        {
            query = query.Take(limit);
        }

        return await query.ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<PagedQueryResult<TEntity>> GetPagedListAsync(PagedQueryOptions<TEntity> options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        int pageSize = Math.Max(options.MaxResultCount ?? PagedQueryOptions<TEntity>.DefaultPageSize, 1);
        IQueryable<TEntity> narrowed = Narrowed(options);
        long total = await (options.Filter is null ? Set : Set.Where(options.Filter)).LongCountAsync(cancellationToken).ConfigureAwait(false);
        IReadOnlyList<TEntity> items = await Sorted(narrowed, options).Skip((Math.Max(options.PageNumber, 1) - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken).ConfigureAwait(false);
        return new PagedQueryResult<TEntity>(items, total);
    }

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        await Set.AddAsync(entity, cancellationToken).ConfigureAwait(false);
        await Db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        // an entity that FindAsync returned is tracked and only its changes are saved; one that is not (it came from a list) is saved whole
        if (Db.Entry(entity).State == EntityState.Detached)
        {
            Set.Update(entity);
        }

        await Db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        Set.Remove(entity);
        await Db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private IQueryable<TEntity> Narrowed(ListQueryOptions<TEntity> options)
    {
        IQueryable<TEntity> query = Set.AsNoTracking();
        if (options.Include is not null)
        {
            query = options.Include(query);
        }

        return options.Filter is null ? query : query.Where(options.Filter);
    }

    private static IQueryable<TEntity> Sorted(IQueryable<TEntity> query, ListQueryOptions<TEntity> options)
    {
        // a page needs an order to be the same page twice: without one the key orders it
        SortVisitor visitor = new(query);
        if (options.Sort is { Clauses.Count: > 0 })
        {
            options.Sort.Accept(visitor);
        }
        else
        {
            visitor.Visit(entity => entity.Id, descending: false);
        }

        return visitor.Query;
    }

    // the id is compared as a parameter of the query, not as a constant in it: one plan for every id
    private static Expression<Func<TEntity, bool>> IdEquals(TKey id)
    {
        ParameterExpression entity = Expression.Parameter(typeof(TEntity), "entity");
        Expression<Func<TKey>> value = () => id;
        return Expression.Lambda<Func<TEntity, bool>>(Expression.Equal(Expression.Property(entity, nameof(IEntity<>.Id)), value.Body), entity);
    }

    private sealed class SortVisitor(IQueryable<TEntity> query) : ISortVisitor<TEntity>
    {
        // an EF query is an IOrderedQueryable even before it is ordered, so whether it was is kept here
        private IOrderedQueryable<TEntity>? _ordered;

        public IQueryable<TEntity> Query => _ordered ?? query;

        public void Visit<TSortKey>(Expression<Func<TEntity, TSortKey>> key, bool descending)
            => _ordered = _ordered is null
                ? descending ? query.OrderByDescending(key) : query.OrderBy(key)
                : descending ? _ordered.ThenByDescending(key) : _ordered.ThenBy(key);
    }
}

public static class EfCoreRepositoryServiceCollectionExtensions
{
    /// <summary>Registers <see cref="EfCoreRepository{TContext, TEntity, TKey}"/> as the <see cref="IRepository{TEntity, TKey}"/> of an entity. A service that has a repository of its own registers that one instead.</summary>
    public static IServiceCollection AddEfCoreRepository<TContext, TEntity, TKey>(this IServiceCollection services)
        where TContext : DbContext
        where TEntity : class, IEntity<TKey>
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddScoped<IRepository<TEntity, TKey>, EfCoreRepository<TContext, TEntity, TKey>>();
    }
}
