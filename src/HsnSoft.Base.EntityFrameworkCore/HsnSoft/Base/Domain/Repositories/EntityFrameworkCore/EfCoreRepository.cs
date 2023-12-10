using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.DependencyInjection;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.EntityFrameworkCore;
using HsnSoft.Base.Guids;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.Domain.Repositories.EntityFrameworkCore;

public class EfCoreRepository<TDbContext, TEntity> : RepositoryBase<TEntity>, IEfCoreRepository<TEntity>
    where TDbContext : BaseDbContext<TDbContext>
    where TEntity : class, IEntity
{
    private readonly TDbContext _dbContext;

    public List<Expression<Func<TEntity, object>>> DefaultPropertySelector = null;

    public virtual IGuidGenerator GuidGenerator => LazyServiceProvider.LazyGetService<IGuidGenerator>(SimpleGuidGenerator.Instance);

    public IEfCoreBulkOperationProvider BulkOperationProvider => LazyServiceProvider.LazyGetService<IEfCoreBulkOperationProvider>();

    public EfCoreRepository(IServiceProvider serviceProvider, TDbContext dbContext)
    {
        if (serviceProvider == null)
        {
            throw new ArgumentNullException(nameof(serviceProvider), "ServiceProvider can not be null");
        }

        LazyServiceProvider = serviceProvider.GetRequiredService<IBaseLazyServiceProvider>();

        _dbContext = dbContext;
        _dbContext.LazyServiceProvider = LazyServiceProvider;
    }

    DbContext IEfCoreRepository<TEntity>.GetDbContext()
    {
        return GetDbContext() as DbContext;
    }

    DbSet<TEntity> IEfCoreRepository<TEntity>.GetDbSet()
    {
        return GetDbSet();
    }

    protected virtual TDbContext GetDbContext()
    {
        return _dbContext;
    }

    protected DbSet<TEntity> GetDbSet()
    {
        return GetDbContext().Set<TEntity>();
    }

    public override async Task<IQueryable<TEntity>> WithDetailsAsync()
    {
        if (DefaultPropertySelector == null)
        {
            return await base.WithDetailsAsync();
        }

        return await WithDetailsAsync(DefaultPropertySelector.ToArray());
    }

    public override async Task<IQueryable<TEntity>> WithDetailsAsync(params Expression<Func<TEntity, object>>[] propertySelectors)
    {
        return IncludeDetails(await GetQueryableAsync(), propertySelectors);
    }

    public override async Task<IQueryable<TEntity>> GetQueryableAsync()
    {
        return GetDbSet().AsQueryable();
    }

    public override async Task<TEntity> FindAsync(Expression<Func<TEntity, bool>> predicate, bool includeDetails = true, CancellationToken cancellationToken = default)
    {
        return includeDetails
            ? await (await WithDetailsAsync())
                .Where(predicate)
                .SingleOrDefaultAsync(GetCancellationToken(cancellationToken))
            : await GetDbSet()
                .Where(predicate)
                .SingleOrDefaultAsync(GetCancellationToken(cancellationToken));
    }

    public override async Task<List<TEntity>> GetListAsync(Expression<Func<TEntity, bool>> predicate, bool includeDetails = false, CancellationToken cancellationToken = default)
    {
        return includeDetails
            ? await (await WithDetailsAsync()).Where(predicate).ToListAsync(GetCancellationToken(cancellationToken))
            : await GetDbSet().Where(predicate).ToListAsync(GetCancellationToken(cancellationToken));
    }

    public override async Task<List<TEntity>> GetListAsync(bool includeDetails = false, CancellationToken cancellationToken = default)
    {
        return includeDetails
            ? await (await WithDetailsAsync()).ToListAsync(GetCancellationToken(cancellationToken))
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
            ? await WithDetailsAsync()
            : GetDbSet();

        return await queryable
            .OrderByIf<TEntity, IQueryable<TEntity>>(!sorting.IsNullOrWhiteSpace(), sorting)
            .PageBy(skipCount, maxResultCount)
            .ToListAsync(GetCancellationToken(cancellationToken));
    }

    public override async Task<TEntity> InsertAsync(TEntity entity, bool autoSave = false, CancellationToken cancellationToken = default)
    {
        CheckAndSetId(entity);

        var savedEntity = (await GetDbSet().AddAsync(entity, GetCancellationToken(cancellationToken))).Entity;

        if (autoSave)
        {
            await GetDbContext().SaveChangesAsync(GetCancellationToken(cancellationToken));
        }

        return savedEntity;
    }

    public override async Task InsertManyAsync(IEnumerable<TEntity> entities, bool autoSave = false, CancellationToken cancellationToken = default)
    {
        var entityArray = entities.ToArray();
        cancellationToken = GetCancellationToken(cancellationToken);

        foreach (var entity in entityArray)
        {
            CheckAndSetId(entity);
        }

        if (BulkOperationProvider != null)
        {
            await BulkOperationProvider.InsertManyAsync<TDbContext, TEntity>(
                this,
                entityArray,
                autoSave,
                GetCancellationToken(cancellationToken)
            );
            return;
        }

        await GetDbSet().AddRangeAsync(entityArray, cancellationToken);

        if (autoSave)
        {
            await GetDbContext().SaveChangesAsync(cancellationToken);
        }
    }

    public override async Task<TEntity> UpdateAsync(TEntity entity, bool autoSave = false, CancellationToken cancellationToken = default)
    {
        var dbContext = GetDbContext();
        dbContext.Attach(entity);

        var updatedEntity = dbContext.Update(entity).Entity;

        if (autoSave)
        {
            await dbContext.SaveChangesAsync(GetCancellationToken(cancellationToken));
        }

        return updatedEntity;
    }

    public override async Task UpdateManyAsync(IEnumerable<TEntity> entities, bool autoSave = false, CancellationToken cancellationToken = default)
    {
        cancellationToken = GetCancellationToken(cancellationToken);

        if (BulkOperationProvider != null)
        {
            await BulkOperationProvider.UpdateManyAsync<TDbContext, TEntity>(
                this,
                entities,
                autoSave,
                GetCancellationToken(cancellationToken)
            );

            return;
        }

        GetDbSet().UpdateRange(entities);

        if (autoSave)
        {
            await GetDbContext().SaveChangesAsync(cancellationToken);
        }
    }

    public override async Task DeleteAsync(TEntity entity, bool autoSave = false, CancellationToken cancellationToken = default)
    {
        GetDbSet().Remove(entity);

        if (autoSave)
        {
            await GetDbContext().SaveChangesAsync(GetCancellationToken(cancellationToken));
        }
    }

    public override async Task DeleteManyAsync(IEnumerable<TEntity> entities, bool autoSave = false, CancellationToken cancellationToken = default)
    {
        cancellationToken = GetCancellationToken(cancellationToken);

        if (BulkOperationProvider != null)
        {
            await BulkOperationProvider.DeleteManyAsync<TDbContext, TEntity>(
                this,
                entities,
                autoSave,
                cancellationToken
            );

            return;
        }

        GetDbContext().RemoveRange(entities);

        if (autoSave)
        {
            await GetDbContext().SaveChangesAsync(cancellationToken);
        }
    }

    public override async Task DeleteAsync(Expression<Func<TEntity, bool>> predicate, bool autoSave = false, CancellationToken cancellationToken = default)
    {
        var entities = await GetDbSet()
            .Where(predicate)
            .ToListAsync(GetCancellationToken(cancellationToken));

        await DeleteManyAsync(entities, autoSave, cancellationToken);

        if (autoSave)
        {
            await GetDbContext().SaveChangesAsync(GetCancellationToken(cancellationToken));
        }
    }

    public override async Task DeleteDirectAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        await GetDbSet().Where(predicate).ExecuteDeleteAsync(GetCancellationToken(cancellationToken));
    }

    protected override async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await GetDbContext().SaveChangesAsync(cancellationToken);
    }

    public virtual async Task EnsureCollectionLoadedAsync<TProperty>(
        TEntity entity,
        Expression<Func<TEntity, IEnumerable<TProperty>>> propertyExpression,
        CancellationToken cancellationToken = default)
        where TProperty : class
    {
        await GetDbContext()
            .Entry(entity)
            .Collection(propertyExpression)
            .LoadAsync(GetCancellationToken(cancellationToken));
    }

    public virtual async Task EnsurePropertyLoadedAsync<TProperty>(
        TEntity entity,
        Expression<Func<TEntity, TProperty>> propertyExpression,
        CancellationToken cancellationToken = default)
        where TProperty : class
    {
        await GetDbContext()
            .Entry(entity)
            .Reference(propertyExpression)
            .LoadAsync(GetCancellationToken(cancellationToken));
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
            () => GuidGenerator.Create(),
            true
        );
    }
}

public class EfCoreRepository<TDbContext, TEntity, TKey> : EfCoreRepository<TDbContext, TEntity>,
    IEfCoreRepository<TEntity, TKey>,
    ISupportsExplicitLoading<TEntity, TKey>
    where TDbContext : BaseDbContext<TDbContext>
    where TEntity : class, IEntity<TKey>
{
    public EfCoreRepository(IServiceProvider serviceProvider, TDbContext dbContext)
        : base(serviceProvider, dbContext)
    {
    }

    public virtual async Task<TEntity> GetAsync(TKey id, bool includeDetails = true, CancellationToken cancellationToken = default)
    {
        var entity = await FindAsync(id, includeDetails, GetCancellationToken(cancellationToken));

        if (entity == null)
        {
            throw new EntityNotFoundException(typeof(TEntity), id);
        }

        return entity;
    }

    public virtual async Task<TEntity> FindAsync(TKey id, bool includeDetails = true, CancellationToken cancellationToken = default)
    {
        return includeDetails
            ? await (await WithDetailsAsync()).OrderBy(e => e.Id).FirstOrDefaultAsync(e => e.Id.Equals(id), GetCancellationToken(cancellationToken))
            : await GetDbSet().FindAsync(new object[] { id }, GetCancellationToken(cancellationToken));
    }

    public virtual async Task DeleteAsync(TKey id, bool autoSave = false, CancellationToken cancellationToken = default)
    {
        var entity = await FindAsync(id, cancellationToken: cancellationToken);
        if (entity == null)
        {
            return;
        }

        await DeleteAsync(entity, autoSave, cancellationToken);
    }

    public virtual async Task DeleteManyAsync(IEnumerable<TKey> ids, bool autoSave = false, CancellationToken cancellationToken = default)
    {
        cancellationToken = GetCancellationToken(cancellationToken);

        var entities = await GetDbSet().Where(x => ids.Contains(x.Id)).ToListAsync(cancellationToken);

        await DeleteManyAsync(entities, autoSave, cancellationToken);
    }
}