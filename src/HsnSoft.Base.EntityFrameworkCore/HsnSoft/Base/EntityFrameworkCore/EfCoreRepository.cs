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
public class EfCoreRepository<TContext, TEntity, TKey>(TContext db) : IEfCoreRepository<TEntity, TKey>
    where TContext : DbContext
    where TEntity : class, IEntity<TKey>
{
    protected TContext Db { get; } = db;

    protected DbSet<TEntity> Set => Db.Set<TEntity>();

    public Task<TEntity?> FindAsync(TKey id, CancellationToken cancellationToken = default)
        => Set.FirstOrDefaultAsync(IdEquals(id), cancellationToken);

    public Task<TEntity?> FindAsync(TKey id, Func<IQueryable<TEntity>, IQueryable<TEntity>> include, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(include);

        return include(Set).FirstOrDefaultAsync(IdEquals(id), cancellationToken);
    }

    public Task<TDto?> FindAsync<TDto>(TKey id, Expression<Func<TEntity, TDto>>? selector = null, CancellationToken cancellationToken = default)
        where TDto : class
        => Set.AsNoTracking().Where(IdEquals(id)).Select(selector ?? EntityProjection.For<TEntity, TDto>()).FirstOrDefaultAsync(cancellationToken);

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

    public Task<IReadOnlyList<TEntity>> GetListAsync(ListQueryOptions<TEntity> options, CancellationToken cancellationToken = default)
        => ListAsync(options, null, cancellationToken);

    public Task<IReadOnlyList<TEntity>> GetListAsync(ListQueryOptions<TEntity> options, Func<IQueryable<TEntity>, IQueryable<TEntity>> include, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(include);

        return ListAsync(options, include, cancellationToken);
    }

    public async Task<IReadOnlyList<TDto>> GetListAsync<TDto>(ListQueryOptions<TEntity> options, Expression<Func<TEntity, TDto>>? selector = null, CancellationToken cancellationToken = default)
        where TDto : class
    {
        ArgumentNullException.ThrowIfNull(options);

        IQueryable<TEntity> query = Sorted(Narrowed(options, null), options);
        if (options.MaxResultCount is { } limit)
        {
            query = query.Take(limit);
        }

        return await query.Select(selector ?? EntityProjection.For<TEntity, TDto>()).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<PagedQueryResult<TEntity>> GetPagedListAsync(PagedQueryOptions<TEntity> options, CancellationToken cancellationToken = default)
        => PagedAsync(options, null, cancellationToken);

    public Task<PagedQueryResult<TEntity>> GetPagedListAsync(PagedQueryOptions<TEntity> options, Func<IQueryable<TEntity>, IQueryable<TEntity>> include, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(include);

        return PagedAsync(options, include, cancellationToken);
    }

    public async Task<PagedQueryResult<TDto>> GetPagedListAsync<TDto>(PagedQueryOptions<TEntity> options, Expression<Func<TEntity, TDto>>? selector = null, CancellationToken cancellationToken = default)
        where TDto : class
    {
        ArgumentNullException.ThrowIfNull(options);

        (IQueryable<TEntity> page, long total) = await PageOf(options, null, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<TDto> items = await page.Select(selector ?? EntityProjection.For<TEntity, TDto>()).ToListAsync(cancellationToken).ConfigureAwait(false);
        return new PagedQueryResult<TDto>(items, total);
    }

    private async Task<IReadOnlyList<TEntity>> ListAsync(ListQueryOptions<TEntity> options, Func<IQueryable<TEntity>, IQueryable<TEntity>>? include, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);

        IQueryable<TEntity> query = Sorted(Narrowed(options, include), options);
        if (options.MaxResultCount is { } limit)
        {
            query = query.Take(limit);
        }

        return await query.ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<PagedQueryResult<TEntity>> PagedAsync(PagedQueryOptions<TEntity> options, Func<IQueryable<TEntity>, IQueryable<TEntity>>? include, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);

        (IQueryable<TEntity> page, long total) = await PageOf(options, include, cancellationToken).ConfigureAwait(false);
        return new PagedQueryResult<TEntity>(await page.ToListAsync(cancellationToken).ConfigureAwait(false), total);
    }

    // the query of one page, and how many there are in all (counted without the include: a join does not change how many entities match)
    private async Task<(IQueryable<TEntity> Page, long Total)> PageOf(PagedQueryOptions<TEntity> options, Func<IQueryable<TEntity>, IQueryable<TEntity>>? include, CancellationToken cancellationToken)
    {
        int pageSize = Math.Max(options.MaxResultCount ?? PagedQueryOptions<TEntity>.DefaultPageSize, 1);
        long total = await (options.Filter is null ? Set : Set.Where(options.Filter)).LongCountAsync(cancellationToken).ConfigureAwait(false);
        return (Sorted(Narrowed(options, include), options).Skip((Math.Max(options.PageNumber, 1) - 1) * pageSize).Take(pageSize), total);
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

    private IQueryable<TEntity> Narrowed(ListQueryOptions<TEntity> options, Func<IQueryable<TEntity>, IQueryable<TEntity>>? include)
    {
        IQueryable<TEntity> query = Set.AsNoTracking();
        if (include is not null)
        {
            query = include(query);
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
    /// <summary>Registers <see cref="EfCoreRepository{TContext, TEntity, TKey}"/> as the <see cref="IRepository{TEntity, TKey}"/> and the <see cref="IEfCoreRepository{TEntity, TKey}"/> of an entity. A service that has a repository of its own registers that one instead.</summary>
    public static IServiceCollection AddEfCoreRepository<TContext, TEntity, TKey>(this IServiceCollection services)
        where TContext : DbContext
        where TEntity : class, IEntity<TKey>
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<EfCoreRepository<TContext, TEntity, TKey>>();
        services.AddScoped<IEfCoreRepository<TEntity, TKey>>(provider => provider.GetRequiredService<EfCoreRepository<TContext, TEntity, TKey>>());
        return services.AddScoped<IRepository<TEntity, TKey>>(provider => provider.GetRequiredService<EfCoreRepository<TContext, TEntity, TKey>>());
    }
}
