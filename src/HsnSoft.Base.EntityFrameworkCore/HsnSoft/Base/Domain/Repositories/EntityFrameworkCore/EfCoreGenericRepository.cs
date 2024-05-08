using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HsnSoft.Base.Domain.Repositories.EntityFrameworkCore;

public class EfCoreGenericRepository<TDbContext, TEntity, TKey> : GenericRepositoryBase<TEntity, TKey>, IEfCoreGenericRepository<TEntity, TKey>
    where TDbContext : BaseEfCoreDbContext<TDbContext>
    where TEntity : class, IEntity<TKey>
{
    private readonly TDbContext _dbContext;

    public List<Expression<Func<TEntity, object>>> DefaultPropertySelector = null;

    public EfCoreGenericRepository(IServiceProvider provider, TDbContext dbContext) : base(provider)
    {
        _dbContext = dbContext;
    }

    public DbSet<TEntity> GetDbSet() => _dbContext?.Set<TEntity>();

    public IQueryable<TEntity> WithDetails()
    {
        if (DefaultPropertySelector == null)
        {
            return GetQueryable();
        }

        return WithDetails(DefaultPropertySelector.ToArray());
    }

    public IQueryable<TEntity> WithDetails(params Expression<Func<TEntity, object>>[] propertySelectors)
    {
        return IncludeDetails(GetQueryable(), propertySelectors);
    }

    public IQueryable<TEntity> GetQueryable()
    {
        return GetDbSet().AsQueryable();
    }

    public override async Task<TEntity> FindAsync(TKey id, bool includeDetails = true, CancellationToken cancellationToken = default)
    {
        IEnumerable<TKey> ids = new[] { id };
        return await FindAsync(x => ids.Contains(x.Id), cancellationToken: GetCancellationToken(cancellationToken));
    }

    public override async Task<TEntity> FindAsync(Expression<Func<TEntity, bool>> predicate, bool includeDetails = true, CancellationToken cancellationToken = default)
    {
        var query = includeDetails
            ? WithDetails().Where(predicate)
            : GetDbSet().Where(predicate);

        var results = await query.ToListAsync(GetCancellationToken(cancellationToken));

        if (results is not { Count: > 0 }) return null;
        if (results is { Count: > 1 })
        {
            throw new EntityDuplicateException(typeof(TEntity));
        }

        return results.SingleOrDefault();
    }

    public override async Task<List<TEntity>> GetListAsync(Expression<Func<TEntity, bool>> predicate, bool includeDetails = false, CancellationToken cancellationToken = default)
    {
        return includeDetails
            ? await WithDetails().Where(predicate).ToListAsync(GetCancellationToken(cancellationToken))
            : await GetDbSet().Where(predicate).ToListAsync(GetCancellationToken(cancellationToken));
    }

    public override async Task<List<TEntity>> GetListAsync(bool includeDetails = false, CancellationToken cancellationToken = default)
    {
        return includeDetails
            ? await WithDetails().ToListAsync(GetCancellationToken(cancellationToken))
            : await GetDbSet().ToListAsync(GetCancellationToken(cancellationToken));
    }

    public override async Task<long> GetCountAsync(CancellationToken cancellationToken = default)
    {
        return await GetDbSet().LongCountAsync(GetCancellationToken(cancellationToken));
    }

    public override async Task<List<TEntity>> GetPagedListAsync(
        int skipCount,
        int maxResultCount,
        string sorting,
        bool includeDetails = false,
        CancellationToken cancellationToken = default)
    {
        var queryable = includeDetails
            ? WithDetails()
            : GetDbSet();

        return await queryable
            .OrderByIf<TEntity, IQueryable<TEntity>>(!sorting.IsNullOrWhiteSpace(), sorting)
            .PageBy(skipCount, maxResultCount)
            .ToListAsync(GetCancellationToken(cancellationToken));
    }

    public override async Task<long> GetCountAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await GetDbSet().Where(predicate).LongCountAsync(GetCancellationToken(cancellationToken));
    }

    public override async Task<List<TEntity>> GetPagedListAsync(
        Expression<Func<TEntity, bool>> predicate,
        int skipCount,
        int maxResultCount,
        string sorting,
        bool includeDetails = false,
        CancellationToken cancellationToken = default)
    {
        var queryable = includeDetails
            ? WithDetails()
            : GetDbSet();

        return await queryable.Where(predicate)
            .OrderByIf<TEntity, IQueryable<TEntity>>(!sorting.IsNullOrWhiteSpace(), sorting)
            .PageBy(skipCount, maxResultCount)
            .ToListAsync(GetCancellationToken(cancellationToken));
    }

    public override async Task<TEntity> InsertAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        CheckAndSetId(entity);

        var savedEntity = (await GetDbSet().AddAsync(entity, GetCancellationToken(cancellationToken))).Entity;

        await _dbContext?.SaveChangesAsync(GetCancellationToken(cancellationToken))!;

        return savedEntity;
    }

    public override async Task InsertManyAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        var entityArray = entities.ToArray();
        cancellationToken = GetCancellationToken(cancellationToken);

        foreach (var entity in entityArray)
        {
            CheckAndSetId(entity);
        }

        await GetDbSet().AddRangeAsync(entityArray, cancellationToken);

        await _dbContext?.SaveChangesAsync(cancellationToken)!;
    }

    public override async Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        _dbContext?.Attach(entity);

        var updatedEntity = _dbContext?.Update(entity).Entity;

        await _dbContext?.SaveChangesAsync(GetCancellationToken(cancellationToken))!;

        return updatedEntity;
    }

    public override async Task UpdateManyAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        cancellationToken = GetCancellationToken(cancellationToken);

        GetDbSet().UpdateRange(entities);

        await _dbContext?.SaveChangesAsync(cancellationToken)!;
    }

    public override async Task<bool> DeleteAsync(TKey id, CancellationToken cancellationToken = default)
    {
        var entity = await FindAsync(id, cancellationToken: cancellationToken);
        if (entity == null)
        {
            return false;
        }

        return await DeleteAsync(entity, cancellationToken);
    }

    public override async Task<bool> DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        GetDbSet().Remove(entity);

        var resultCount = await _dbContext?.SaveChangesAsync(GetCancellationToken(cancellationToken))!;

        return resultCount > 0;
    }

    public override async Task<bool> DeleteAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var entities = await GetDbSet()
            .Where(predicate)
            .ToListAsync(GetCancellationToken(cancellationToken));

        await DeleteManyAsync(entities, cancellationToken);

        var resultCount = await _dbContext?.SaveChangesAsync(GetCancellationToken(cancellationToken))!;

        return resultCount > 0;
    }

    public override async Task DeleteManyAsync(IEnumerable<TKey> ids, CancellationToken cancellationToken = default)
    {
        cancellationToken = GetCancellationToken(cancellationToken);

        await GetDbSet().Where(x => ids.Contains(x.Id)).ExecuteDeleteAsync(GetCancellationToken(cancellationToken));
    }

    public override async Task DeleteManyAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        cancellationToken = GetCancellationToken(cancellationToken);

        _dbContext?.RemoveRange(entities);

        await _dbContext?.SaveChangesAsync(cancellationToken)!;
    }

    protected override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext?.SaveChangesAsync(cancellationToken)!;
    }

    private static IQueryable<TEntity> IncludeDetails(IQueryable<TEntity> query, Expression<Func<TEntity, object>>[] propertySelectors)
    {
        if (!propertySelectors.IsNullOrEmpty())
        {
            foreach (var propertySelector in propertySelectors)
            {
                query = query.Include(propertySelector);
            }
        }

        return query;
    }

    protected virtual void CheckAndSetId(TEntity entity)
    {
        if (entity is IEntity<Guid> entityWithGuidId)
        {
            TrySetGuidId(entityWithGuidId);
        }
    }

    protected virtual void TrySetGuidId(IEntity<Guid> entity)
    {
        if (entity.Id != default)
        {
            return;
        }

        EntityHelper.TrySetId(
            entity,
            Guid.NewGuid,
            true
        );
    }
}