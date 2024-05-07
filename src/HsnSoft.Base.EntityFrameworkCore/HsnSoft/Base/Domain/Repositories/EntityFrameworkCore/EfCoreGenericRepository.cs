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

public class EfCoreGenericRepository<TDbContext, TEntity, TKey> : GenericRepositoryBase<TEntity, TKey>, IEfCoreGenericRepository<TDbContext, TEntity, TKey>
    where TDbContext : BaseEfCoreDbContext<TDbContext>
    where TEntity : class, IEntity<TKey>
{
    private readonly TDbContext _dbContext;

    public List<Expression<Func<TEntity, object>>> DefaultPropertySelector = null;

    public EfCoreGenericRepository(IServiceProvider provider, TDbContext dbContext) : base(provider)
    {
        _dbContext = dbContext;
    }

    public TDbContext GetDbContext() => _dbContext;

    public DbSet<TEntity> GetDbSet() => GetDbContext().Set<TEntity>();

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
        return includeDetails
            ? await WithDetails().OrderBy(e => e.Id).FirstOrDefaultAsync(e => e.Id.Equals(id), GetCancellationToken(cancellationToken))
            : await GetDbSet().FindAsync(new object[] { id }, GetCancellationToken(cancellationToken));
    }

    public override async Task<TEntity> FindAsync(Expression<Func<TEntity, bool>> predicate, bool includeDetails = true, CancellationToken cancellationToken = default)
    {
        return includeDetails
            ? await WithDetails()
                .Where(predicate)
                .FirstOrDefaultAsync(GetCancellationToken(cancellationToken))
            : await GetDbSet()
                .Where(predicate)
                .FirstOrDefaultAsync(GetCancellationToken(cancellationToken));
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

    public override async Task<TEntity> InsertAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        CheckAndSetId(entity);

        var savedEntity = (await GetDbSet().AddAsync(entity, GetCancellationToken(cancellationToken))).Entity;

        await GetDbContext().SaveChangesAsync(GetCancellationToken(cancellationToken));

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

        await GetDbContext().SaveChangesAsync(cancellationToken);
    }

    public override async Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        var dbContext = GetDbContext();
        dbContext.Attach(entity);

        var updatedEntity = dbContext.Update(entity).Entity;

        await dbContext.SaveChangesAsync(GetCancellationToken(cancellationToken));

        return updatedEntity;
    }

    public override async Task UpdateManyAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        cancellationToken = GetCancellationToken(cancellationToken);

        GetDbSet().UpdateRange(entities);

        await GetDbContext().SaveChangesAsync(cancellationToken);
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

        var resultCount = await GetDbContext().SaveChangesAsync(GetCancellationToken(cancellationToken));

        return resultCount > 0;
    }

    public override async Task<bool> DeleteAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var entities = await GetDbSet()
            .Where(predicate)
            .ToListAsync(GetCancellationToken(cancellationToken));

        await DeleteManyAsync(entities, cancellationToken);

        var resultCount = await GetDbContext().SaveChangesAsync(GetCancellationToken(cancellationToken));

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

        GetDbContext().RemoveRange(entities);

        await GetDbContext().SaveChangesAsync(cancellationToken);
    }

    protected override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await GetDbContext().SaveChangesAsync(cancellationToken);
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