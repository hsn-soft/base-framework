using System;
using System.Linq;
using System.Linq.Expressions;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HsnSoft.Base.Domain.Repositories;

public interface IEfCoreGenericRepository<TDbContext, TEntity, in TKey> : IGenericRepository<TEntity, TKey>
    where TDbContext : BaseEfCoreDbContext<TDbContext>
    where TEntity : class, IEntity<TKey>
{
    TDbContext GetDbContext();
    DbSet<TEntity> GetDbSet();

    IQueryable<TEntity> WithDetails();

    IQueryable<TEntity> WithDetails(params Expression<Func<TEntity, object>>[] propertySelectors);

    IQueryable<TEntity> GetQueryable(bool isThreadSafe = false);
}