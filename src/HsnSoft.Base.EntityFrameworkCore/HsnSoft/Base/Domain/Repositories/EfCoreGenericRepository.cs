using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace HsnSoft.Base.Domain.Repositories;

public class EfCoreGenericRepository<TEntity, TKey>(DbContext context) :
    GenericRepositoryBase<TEntity, TKey>,
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
        var results = await GetDbSet()
            .Where(predicate)
            .Take(2)
            .Select(selector)
            .ToListAsync(cancellationToken);

        return results.Count switch
        {
            0 => null,
            > 1 => throw new EntityDuplicateException(typeof(TEntity)),
            _ => results[0]
        };
    }

    public override async Task<TResult> GetFirstOrDefaultAsync<TResult>(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TResult>> selector,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderByEntity = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> query = GetDbSet().Where(predicate);
        if (orderByEntity != null) query = orderByEntity(query);

        return await query.Select(selector).FirstOrDefaultAsync(cancellationToken);
    }

    public override async Task<List<TResult>> GetListAsync<TResult>(
        ListQueryOptions<TEntity> options,
        Expression<Func<TEntity, TResult>> selector,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> query = GetDbSet();

        query = query.AsNoTracking();
        if (options.Filter != null) query = query.Where(options.Filter);

        if (!string.IsNullOrWhiteSpace(options.OrderByDynamic))
            query = query.OrderBy(options.OrderByDynamic);
        else if (options.OrderByEntity != null)
            query = options.OrderByEntity(query);

        if (options.ListLength.HasValue) query = query.Take((int)options.ListLength.Value);

        return await query.Select(selector).ToListAsync(cancellationToken);
    }

    public override async Task<PagedQueryResult<TResult>> GetPageListAsync<TResult>(
        PagedQueryOptions<TEntity> options,
        Expression<Func<TEntity, TResult>> selector,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> query = GetDbSet();

        query = query.AsNoTracking();
        if (options.Filter != null) query = query.Where(options.Filter);

        int totalCount = await query.CountAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(options.OrderByDynamic))
            query = query.OrderBy(options.OrderByDynamic);
        else if (options.OrderByEntity != null)
            query = options.OrderByEntity(query);

        if (options.PageNumber > 1)
        {
            query = query.Skip(((int)options.PageNumber - 1) * (int)options.PageSize)
                .Take((int)options.PageSize);
        }
        else
        {
            query = query.Take((int)options.PageSize);
        }

        var items = await query.Select(selector).ToListAsync(cancellationToken);

        return new PagedQueryResult<TResult> { Items = items, TotalCount = (uint)totalCount, PageNumber = options.PageNumber, PageSize = options.PageSize };
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

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}