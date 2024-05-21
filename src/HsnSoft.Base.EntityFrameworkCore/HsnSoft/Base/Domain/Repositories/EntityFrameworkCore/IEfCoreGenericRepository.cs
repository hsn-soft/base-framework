using System;
using System.Linq;
using System.Linq.Expressions;
using HsnSoft.Base.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HsnSoft.Base.Domain.Repositories.EntityFrameworkCore;

public interface IEfCoreGenericRepository<TEntity, in TKey> : IGenericRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
{
    DbSet<TEntity> GetDbSet();

    IQueryable<TEntity> WithDetails();

    IQueryable<TEntity> WithDetails(params Expression<Func<TEntity, object>>[] propertySelectors);

    IQueryable<TEntity> GetQueryable(bool isThreadSafe = false);
}